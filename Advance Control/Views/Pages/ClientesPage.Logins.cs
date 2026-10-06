using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Activity;
using Advance_Control.Services.Email;
using Advance_Control.Services.Notificacion;
using Advance_Control.Services.Portal;
using Advance_Control.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Pages
{
    /// <summary>
    /// Pivot "Logins": accesos de la empresa al Portal de Clientes. Cada login
    /// se liga a un contacto del cliente (que recibe sus datos por correo); la
    /// contraseña la genera la API y solo se conoce al crear o restablecer.
    /// </summary>
    public sealed partial class ClientesPage
    {
        private const string PortalClientesUrl = "https://advance-elevadores.mx/portalclientes/";

        private IClienteLoginService ClienteLoginService => AppServices.Get<IClienteLoginService>();

        private async Task LoadLoginsForClienteAsync(CustomerDto cliente)
        {
            if (cliente.IsLoadingLogins)
                return;

            try
            {
                cliente.IsLoadingLogins = true;
                var logins = await ClienteLoginService.ObtenerAsync(cliente.IdCliente);

                cliente.Logins.Clear();
                foreach (var login in logins)
                    cliente.Logins.Add(login);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al cargar logins del portal del cliente", ex, "ClientesPage", nameof(LoadLoginsForClienteAsync));
            }
            finally
            {
                cliente.LoginsLoaded = true;
                cliente.NotifyNoLoginsMessageChanged();
                cliente.IsLoadingLogins = false;
            }
        }

        private async Task RecargarLoginsAsync(CustomerDto cliente)
        {
            cliente.LoginsLoaded = false;
            await LoadLoginsForClienteAsync(cliente);
        }

        private CustomerDto? ClienteDeLogin(ClienteLoginDto login) =>
            ViewModel.Customers.FirstOrDefault(c => c.Logins.Contains(login));

        private async void NuevoLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not CustomerDto cliente)
                return;

            if (!cliente.ContactosLoaded)
                await LoadContactosForClienteAsync(cliente);

            var idsConLogin = cliente.Logins.Select(l => l.ContactoId).ToHashSet();
            var candidatos = cliente.Contactos
                .Where(c => !string.IsNullOrWhiteSpace(c.Correo) && !idsConLogin.Contains(c.ContactoId))
                .OrderBy(c => c.NombreCompleto)
                .ToList();

            if (candidatos.Count == 0)
            {
                await _notificacionService.MostrarAsync("Sin contactos disponibles",
                    "No hay contactos con correo sin login. Agrega primero el contacto (con su correo) en la pestaña \"Contactos\".");
                return;
            }

            var contactoCombo = new ComboBox
            {
                Header = "Contacto",
                PlaceholderText = "Selecciona el contacto que recibirá el acceso",
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            foreach (var contacto in candidatos)
                contactoCombo.Items.Add(new ComboBoxItem { Content = $"{contacto.NombreCompleto} · {contacto.Correo}", Tag = contacto });

            var usuarioTextBox = new TextBox { Header = "Usuario", PlaceholderText = "Con el que entrará al portal" };
            contactoCombo.SelectionChanged += (_, _) =>
            {
                if (contactoCombo.SelectedItem is ComboBoxItem { Tag: ContactoDto c })
                    usuarioTextBox.Text = c.Correo?.Trim() ?? string.Empty;
            };
            contactoCombo.SelectedIndex = 0;

            var enviarCheckBox = new CheckBox { Content = "Enviar los datos de acceso por correo al contacto", IsChecked = true };

            var dialog = new ContentDialog
            {
                Title = $"Nuevo login · {cliente.NombreComercial}",
                Content = new StackPanel
                {
                    Spacing = 12,
                    MinWidth = 420,
                    Children =
                    {
                        contactoCombo,
                        usuarioTextBox,
                        new TextBlock
                        {
                            Text = "La contraseña se genera automáticamente.",
                            FontSize = 12,
                            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                        },
                        enviarCheckBox,
                    }
                },
                PrimaryButtonText = "Crear",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return;

            if (contactoCombo.SelectedItem is not ComboBoxItem { Tag: ContactoDto contactoElegido })
                return;

            var usuario = usuarioTextBox.Text.Trim();
            if (usuario.Length < 3)
            {
                await _notificacionService.MostrarAsync("Validación", "El usuario debe tener al menos 3 caracteres.");
                return;
            }

            ClienteLoginPasswordDto creado;
            try
            {
                creado = await ClienteLoginService.CrearAsync(cliente.IdCliente, contactoElegido.ContactoId, usuario);
            }
            catch (InvalidOperationException ex)
            {
                await _notificacionService.MostrarAsync("No se pudo crear el login", ex.Message);
                return;
            }

            _activityService.Registrar("Clientes", "Login del portal creado");
            await RecargarLoginsAsync(cliente);

            if (enviarCheckBox.IsChecked == true)
                await EnviarDatosAccesoAsync(cliente, creado);
            else
                await MostrarDatosAccesoAsync("Login creado", creado, "Guarda o comparte estos datos: la contraseña no se puede volver a consultar.");
        }

        private async void EnviarDatosLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ClienteLoginDto login } || ClienteDeLogin(login) is not CustomerDto cliente)
                return;

            if (string.IsNullOrWhiteSpace(login.ContactoCorreo))
            {
                await _notificacionService.MostrarAsync("Sin correo", "El contacto de este login no tiene correo registrado.");
                return;
            }

            var confirmado = await ConfirmarAsync(
                "Enviar datos de acceso",
                $"Se generará una contraseña nueva para \"{login.Usuario}\" y se enviará a {login.ContactoCorreo}. La contraseña anterior dejará de funcionar.",
                "Enviar");
            if (!confirmado)
                return;

            ClienteLoginPasswordDto restablecido;
            try
            {
                restablecido = await ClienteLoginService.RestablecerAsync(cliente.IdCliente, login.Id);
            }
            catch (InvalidOperationException ex)
            {
                await _notificacionService.MostrarAsync("No se pudo generar la contraseña", ex.Message);
                return;
            }

            await EnviarDatosAccesoAsync(cliente, restablecido);
        }

        private async void RestablecerLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ClienteLoginDto login } || ClienteDeLogin(login) is not CustomerDto cliente)
                return;

            var confirmado = await ConfirmarAsync(
                "Restablecer contraseña",
                $"Se generará una contraseña nueva para \"{login.Usuario}\" y se cerrarán sus sesiones abiertas. No se enviará correo: la verás en pantalla una sola vez.",
                "Restablecer");
            if (!confirmado)
                return;

            try
            {
                var restablecido = await ClienteLoginService.RestablecerAsync(cliente.IdCliente, login.Id);
                _activityService.Registrar("Clientes", "Contraseña de login del portal restablecida");
                await RecargarLoginsAsync(cliente);
                await MostrarDatosAccesoAsync("Contraseña restablecida", restablecido, "La contraseña no se puede volver a consultar.");
            }
            catch (InvalidOperationException ex)
            {
                await _notificacionService.MostrarAsync("No se pudo restablecer", ex.Message);
            }
        }

        private async void EliminarLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: ClienteLoginDto login } || ClienteDeLogin(login) is not CustomerDto cliente)
                return;

            var confirmado = await ConfirmarAsync(
                "Eliminar login",
                $"¿Eliminar el acceso \"{login.Usuario}\" ({login.ContactoNombre})? Dejará de poder entrar al portal de inmediato.",
                "Eliminar");
            if (!confirmado)
                return;

            try
            {
                await ClienteLoginService.EliminarAsync(cliente.IdCliente, login.Id);
                _activityService.Registrar("Clientes", "Login del portal eliminado");
                cliente.Logins.Remove(login);
                cliente.NotifyNoLoginsMessageChanged();
            }
            catch (InvalidOperationException ex)
            {
                await _notificacionService.MostrarAsync("No se pudo eliminar", ex.Message);
            }
        }

        /// <summary>
        /// Envía usuario y contraseña al correo del contacto con la cuenta de
        /// correo del usuario actual. Si el envío falla, muestra los datos en
        /// pantalla para que la contraseña recién generada no se pierda.
        /// </summary>
        private async Task EnviarDatosAccesoAsync(CustomerDto cliente, ClienteLoginPasswordDto datos)
        {
            var login = datos.Login;
            try
            {
                await AppServices.Get<IEmailService>().SendEmailAsync(new EmailMessage
                {
                    Para = { login.ContactoCorreo! },
                    Asunto = "Tus datos de acceso al Portal de Clientes · Advance Elevadores",
                    CuerpoHtml = ConstruirCorreoHtml(cliente, datos),
                    CuerpoTexto = ConstruirCorreoTexto(cliente, datos),
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error al enviar datos de acceso del portal", ex, "ClientesPage", nameof(EnviarDatosAccesoAsync));
                await MostrarDatosAccesoAsync("No se pudo enviar el correo", datos,
                    $"{ex.Message}\n\nLa contraseña ya se cambió: compártela manualmente con el contacto.");
                return;
            }

            try
            {
                await ClienteLoginService.MarcarDatosEnviadosAsync(cliente.IdCliente, login.Id);
            }
            catch (InvalidOperationException ex)
            {
                await _loggingService.LogWarningAsync($"El correo se envió pero no se registró el envío: {ex.Message}", "ClientesPage", nameof(EnviarDatosAccesoAsync));
            }

            _activityService.Registrar("Clientes", "Datos de acceso del portal enviados");
            await RecargarLoginsAsync(cliente);
            await _notificacionService.MostrarAsync("Datos enviados", $"Se enviaron los datos de acceso a {login.ContactoCorreo}.");
        }

        private async Task MostrarDatosAccesoAsync(string titulo, ClienteLoginPasswordDto datos, string nota)
        {
            var texto = $"Portal: {PortalClientesUrl}\nUsuario: {datos.Login.Usuario}\nContraseña: {datos.Password}";

            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock { Text = nota, TextWrapping = TextWrapping.Wrap },
                        new TextBox
                        {
                            Text = texto,
                            IsReadOnly = true,
                            AcceptsReturn = true,
                            TextWrapping = TextWrapping.Wrap,
                            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                        },
                    }
                },
                PrimaryButtonText = "Copiar",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var paquete = new global::Windows.ApplicationModel.DataTransfer.DataPackage();
                paquete.SetText(texto);
                global::Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(paquete);
            }
        }

        private async Task<bool> ConfirmarAsync(string titulo, string mensaje, string accion)
        {
            var dialog = new ContentDialog
            {
                Title = titulo,
                Content = new TextBlock { Text = mensaje, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = accion,
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private static string ConstruirCorreoTexto(CustomerDto cliente, ClienteLoginPasswordDto datos) =>
            $"Hola {datos.Login.ContactoNombre}:\n\n" +
            $"Estos son tus datos para entrar al Portal de Clientes de Advance Elevadores ({cliente.RazonSocial}), " +
            "donde puedes consultar el avance de tus servicios y aprobar los mantenimientos preventivos.\n\n" +
            $"Portal: {PortalClientesUrl}\n" +
            $"Usuario: {datos.Login.Usuario}\n" +
            $"Contraseña: {datos.Password}\n\n" +
            "No compartas estos datos. Si no solicitaste este acceso, ignora este correo.\n\n" +
            "Advance Elevadores";

        private static string ConstruirCorreoHtml(CustomerDto cliente, ClienteLoginPasswordDto datos)
        {
            static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

            return $@"<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;max-width:560px"">
<p>Hola {H(datos.Login.ContactoNombre)}:</p>
<p>Estos son tus datos para entrar al <b>Portal de Clientes de Advance Elevadores</b> ({H(cliente.RazonSocial)}),
donde puedes consultar el avance de tus servicios y aprobar los mantenimientos preventivos.</p>
<table style=""border-collapse:collapse;margin:16px 0"">
<tr><td style=""padding:4px 12px 4px 0;color:#555"">Portal</td><td style=""padding:4px 0""><a href=""{PortalClientesUrl}"">{PortalClientesUrl}</a></td></tr>
<tr><td style=""padding:4px 12px 4px 0;color:#555"">Usuario</td><td style=""padding:4px 0;font-family:Consolas,monospace""><b>{H(datos.Login.Usuario)}</b></td></tr>
<tr><td style=""padding:4px 12px 4px 0;color:#555"">Contraseña</td><td style=""padding:4px 0;font-family:Consolas,monospace""><b>{H(datos.Password)}</b></td></tr>
</table>
<p><a href=""{PortalClientesUrl}"" style=""display:inline-block;background:#1F3A5F;color:#fff;padding:10px 18px;border-radius:4px;text-decoration:none"">Entrar al portal</a></p>
<p style=""color:#666;font-size:12px"">No compartas estos datos. Si no solicitaste este acceso, ignora este correo.</p>
<p>Advance Elevadores</p>
</div>";
        }
    }
}
