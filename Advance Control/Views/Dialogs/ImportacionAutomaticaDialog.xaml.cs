using System;
using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;

namespace Advance_Control.Views.Dialogs;

/// <summary>
/// Pide el mes y las credenciales del portal FEL en Línea para la importación automática.
/// Usuario y cuenta se recuerdan en LocalSettings; la contraseña nunca se guarda.
/// </summary>
public sealed partial class ImportacionAutomaticaDialog : ContentDialog
{
    private const string ClaveUsuario = "ImportacionAutomatica.Usuario";
    private const string ClaveCuenta = "ImportacionAutomatica.Cuenta";

    public ImportacionAutomaticaRequestDto? Resultado { get; private set; }

    public ImportacionAutomaticaDialog(XamlRoot xamlRoot)
    {
        InitializeComponent();
        XamlRoot = xamlRoot;

        var hoy = DateTimeOffset.Now;
        MesDatePicker.MinYear = new DateTimeOffset(2009, 1, 1, 0, 0, 0, hoy.Offset);
        MesDatePicker.MaxYear = hoy;
        MesDatePicker.Date = new DateTimeOffset(hoy.Year, hoy.Month, 1, 0, 0, 0, hoy.Offset);

        UsuarioTextBox.Text = LeerAjuste(ClaveUsuario);
        CuentaTextBox.Text = LeerAjuste(ClaveCuenta);

        PrimaryButtonClick += OnPrimaryButtonClick;
        Opened += (_, _) =>
        {
            if (string.IsNullOrEmpty(UsuarioTextBox.Text)) UsuarioTextBox.Focus(FocusState.Programmatic);
            else PasswordBox.Focus(FocusState.Programmatic);
        };
    }

    private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        EstadoInfoBar.IsOpen = false;

        var usuario = UsuarioTextBox.Text?.Trim() ?? string.Empty;
        var cuenta = CuentaTextBox.Text?.Trim() ?? string.Empty;
        var mes = MesDatePicker.Date;

        string? error = null;
        if (usuario.Length == 0 || cuenta.Length == 0 || PasswordBox.Password.Length == 0)
            error = "Usuario, cuenta y contraseña son obligatorios.";
        else if (new DateTime(mes.Year, mes.Month, 1) > DateTime.Today)
            error = "No se puede importar un mes futuro.";

        if (error != null)
        {
            EstadoInfoBar.Message = error;
            EstadoInfoBar.IsOpen = true;
            args.Cancel = true;
            return;
        }

        GuardarAjuste(ClaveUsuario, usuario);
        GuardarAjuste(ClaveCuenta, cuenta);

        Resultado = new ImportacionAutomaticaRequestDto
        {
            Anio = mes.Year,
            Mes = mes.Month,
            Usuario = usuario,
            Cuenta = cuenta,
            Password = PasswordBox.Password
        };
    }

    private static string LeerAjuste(string clave)
    {
        try
        {
            return ApplicationData.Current.LocalSettings.Values[clave] as string ?? string.Empty;
        }
        catch
        {
            // LocalSettings no disponible: se capturan a mano.
            return string.Empty;
        }
    }

    private static void GuardarAjuste(string clave, string valor)
    {
        try
        {
            ApplicationData.Current.LocalSettings.Values[clave] = valor;
        }
        catch
        {
            // LocalSettings no disponible: no se recuerda, sin más.
        }
    }
}
