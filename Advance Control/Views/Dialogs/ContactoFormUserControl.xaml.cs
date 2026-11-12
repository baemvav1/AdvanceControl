using Advance_Control.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Advance_Control.Views.Dialogs
{
    /// <summary>Formulario de contacto para crear o editar (los botones los pone quien lo abre).</summary>
    public sealed partial class ContactoFormUserControl : UserControl
    {
        private ContactoDto? _original;

        public ContactoFormUserControl(ContactoDto? contacto = null)
        {
            InitializeComponent();
            if (contacto is not null)
                Cargar(contacto);
            Loaded += (_, _) => NombreTextBox.Focus(FocusState.Programmatic);
        }

        public bool EsEdicion => _original is not null;

        private void Cargar(ContactoDto c)
        {
            _original = c;
            TratamientoTextBox.Text = c.Tratamiento ?? string.Empty;
            NombreTextBox.Text = c.Nombre ?? string.Empty;
            ApellidoTextBox.Text = c.Apellido ?? string.Empty;
            CargoTextBox.Text = c.Cargo ?? string.Empty;
            DepartamentoTextBox.Text = c.Departamento ?? string.Empty;
            CorreoTextBox.Text = c.Correo ?? string.Empty;
            TelefonoTextBox.Text = c.Telefono ?? string.Empty;
            NotasTextBox.Text = c.Notas ?? string.Empty;
        }

        /// <summary>Valida y muestra el error en el formulario. El nombre es obligatorio.</summary>
        public bool Validar()
        {
            if (string.IsNullOrWhiteSpace(NombreTextBox.Text))
            {
                MostrarError("El nombre es obligatorio.");
                return false;
            }
            ErrorText.Visibility = Visibility.Collapsed;
            return true;
        }

        public void MostrarError(string mensaje)
        {
            ErrorText.Text = mensaje;
            ErrorText.Visibility = Visibility.Visible;
        }

        private static string? Valor(TextBox t) => string.IsNullOrWhiteSpace(t.Text) ? null : t.Text.Trim();

        /// <summary>Datos para la API: "create" si es nuevo, "update" (conservando lo demás) si es edición.</summary>
        public ContactoEditDto ADto() => new()
        {
            Operacion = EsEdicion ? "update" : "create",
            ContactoId = _original?.ContactoId ?? 0,
            CredencialId = _original?.CredencialId,
            IdProveedor = _original?.IdProveedor,
            IdCliente = _original?.IdCliente,
            CodigoInterno = _original?.CodigoInterno,
            Activo = _original?.Activo ?? true,
            Tratamiento = Valor(TratamientoTextBox),
            Nombre = Valor(NombreTextBox),
            Apellido = Valor(ApellidoTextBox),
            Cargo = Valor(CargoTextBox),
            Departamento = Valor(DepartamentoTextBox),
            Correo = Valor(CorreoTextBox),
            Telefono = Valor(TelefonoTextBox),
            Notas = Valor(NotasTextBox)
        };

        /// <summary>El contacto tal como quedó (para mostrarlo sin recargar).</summary>
        public ContactoDto AContacto(long contactoId)
        {
            var d = ADto();
            return new ContactoDto
            {
                ContactoId = contactoId,
                CredencialId = d.CredencialId,
                IdProveedor = d.IdProveedor,
                IdCliente = d.IdCliente,
                CodigoInterno = d.CodigoInterno,
                Activo = d.Activo,
                Tratamiento = d.Tratamiento,
                Nombre = d.Nombre,
                Apellido = d.Apellido,
                Cargo = d.Cargo,
                Departamento = d.Departamento,
                Correo = d.Correo,
                Telefono = d.Telefono,
                Notas = d.Notas
            };
        }
    }
}
