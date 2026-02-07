using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;
using Advance_Control.Services.Contactos;
using Advance_Control.Services.Logging;

namespace Advance_Control.ViewModels
{
    public class ContactosViewModel : ViewModelBase
    {
        private readonly IContactoService _contactoService;
        private readonly ILoggingService _logger;
        private ObservableCollection<ContactoDto> _contactos;
        private bool _isLoading;
        private string? _errorMessage;
        private string? _nombreFilter;
        private string? _apellidoFilter;
        private string? _correoFilter;
        private string? _telefonoFilter;
        private string? _departamentoFilter;
        private string? _cargoFilter;

        public ContactosViewModel(IContactoService contactoService, ILoggingService logger)
        {
            _contactoService = contactoService ?? throw new ArgumentNullException(nameof(contactoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _contactos = new ObservableCollection<ContactoDto>();
        }

        public ObservableCollection<ContactoDto> Contactos
        {
            get => _contactos;
            set => SetProperty(ref _contactos, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Mensaje de error para mostrar al usuario
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Indica si hay un mensaje de error activo
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public string? NombreFilter
        {
            get => _nombreFilter;
            set => SetProperty(ref _nombreFilter, value);
        }

        public string? ApellidoFilter
        {
            get => _apellidoFilter;
            set => SetProperty(ref _apellidoFilter, value);
        }

        public string? CorreoFilter
        {
            get => _correoFilter;
            set => SetProperty(ref _correoFilter, value);
        }

        public string? TelefonoFilter
        {
            get => _telefonoFilter;
            set => SetProperty(ref _telefonoFilter, value);
        }

        public string? DepartamentoFilter
        {
            get => _departamentoFilter;
            set => SetProperty(ref _departamentoFilter, value);
        }

        public string? CargoFilter
        {
            get => _cargoFilter;
            set => SetProperty(ref _cargoFilter, value);
        }

        /// <summary>
        /// Carga los contactos desde el servicio con los filtros aplicados
        /// </summary>
        public async Task LoadContactosAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
                return;

            try
            {
                IsLoading = true;
                ErrorMessage = null; // Limpiar errores anteriores
                await _logger.LogInformationAsync("Cargando contactos...", "ContactosViewModel", "LoadContactosAsync");

                var query = new ContactoQueryDto
                {
                    Nombre = NombreFilter,
                    Apellido = ApellidoFilter,
                    Correo = CorreoFilter,
                    Telefono = TelefonoFilter,
                    Departamento = DepartamentoFilter,
                    Cargo = CargoFilter
                };

                var contactos = await _contactoService.GetContactosAsync(query, cancellationToken);

                if (contactos == null)
                {
                    ErrorMessage = "Error: El servicio no devolvió datos válidos.";
                    await _logger.LogWarningAsync("GetContactosAsync devolvió null", "ContactosViewModel", "LoadContactosAsync");
                    return;
                }

                Contactos.Clear();
                foreach (var contacto in contactos)
                {
                    Contactos.Add(contacto);
                }

                await _logger.LogInformationAsync($"Se cargaron {contactos.Count} contactos exitosamente", "ContactosViewModel", "LoadContactosAsync");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "La operación fue cancelada.";
                await _logger.LogInformationAsync("Operación de carga de contactos cancelada por el usuario", "ContactosViewModel", "LoadContactosAsync");
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Error de conexión: No se pudo conectar con el servidor. Verifique su conexión a internet.";
                await _logger.LogErrorAsync("Error de conexión al cargar contactos", ex, "ContactosViewModel", "LoadContactosAsync");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error inesperado al cargar contactos: {ex.Message}";
                await _logger.LogErrorAsync("Error inesperado al cargar contactos", ex, "ContactosViewModel", "LoadContactosAsync");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Limpia los filtros y recarga todos los contactos
        /// </summary>
        public async Task ClearFiltersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                NombreFilter = null;
                ApellidoFilter = null;
                CorreoFilter = null;
                TelefonoFilter = null;
                DepartamentoFilter = null;
                CargoFilter = null;
                ErrorMessage = null;
                await LoadContactosAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al limpiar filtros y recargar contactos.";
                await _logger.LogErrorAsync("Error al limpiar filtros", ex, "ContactosViewModel", "ClearFiltersAsync");
            }
        }

        /// <summary>
        /// Crea un nuevo contacto
        /// </summary>
        public async Task<bool> CreateContactoAsync(
            string nombre,
            string? apellido = null,
            string? correo = null,
            string? telefono = null,
            string? departamento = null,
            string? cargo = null,
            string? codigoInterno = null,
            string? notas = null,
            int? idCliente = null,
            int? idProveedor = null,
            bool activo = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Creando contacto: {nombre} {apellido}", "ContactosViewModel", "CreateContactoAsync");

                var contactoDto = new ContactoEditDto
                {
                    Operacion = "create",
                    Nombre = nombre,
                    Apellido = apellido,
                    Correo = correo,
                    Telefono = telefono,
                    Departamento = departamento,
                    Cargo = cargo,
                    CodigoInterno = codigoInterno,
                    Notas = notas,
                    IdCliente = idCliente,
                    IdProveedor = idProveedor,
                    Activo = activo
                };

                var response = await _contactoService.CreateContactoAsync(contactoDto, cancellationToken);

                if (response.Success)
                {
                    await _logger.LogInformationAsync($"Contacto creado exitosamente: {nombre} {apellido}", "ContactosViewModel", "CreateContactoAsync");
                    
                    // Recargar la lista de contactos
                    await LoadContactosAsync(cancellationToken);
                    return true;
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo crear el contacto: {response.Message}", "ContactosViewModel", "CreateContactoAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al crear contacto", ex, "ContactosViewModel", "CreateContactoAsync");
                return false;
            }
        }

        /// <summary>
        /// Actualiza un contacto existente
        /// </summary>
        public async Task<bool> UpdateContactoAsync(
            long contactoId,
            string? nombre = null,
            string? apellido = null,
            string? correo = null,
            string? telefono = null,
            string? departamento = null,
            string? cargo = null,
            string? codigoInterno = null,
            string? notas = null,
            int? idCliente = null,
            int? idProveedor = null,
            bool? activo = null,
            bool? estatus = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Actualizando contacto ID: {contactoId}", "ContactosViewModel", "UpdateContactoAsync");

                var contactoDto = new ContactoEditDto
                {
                    Operacion = "update",
                    ContactoId = contactoId,
                    Nombre = nombre,
                    Apellido = apellido,
                    Correo = correo,
                    Telefono = telefono,
                    Departamento = departamento,
                    Cargo = cargo,
                    CodigoInterno = codigoInterno,
                    Notas = notas,
                    IdCliente = idCliente,
                    IdProveedor = idProveedor,
                    Activo = activo,
                };

                var response = await _contactoService.UpdateContactoAsync(contactoDto, cancellationToken);

                if (response.Success)
                {
                    await _logger.LogInformationAsync($"Contacto {contactoId} actualizado exitosamente", "ContactosViewModel", "UpdateContactoAsync");
                    
                    // Recargar la lista de contactos
                    await LoadContactosAsync(cancellationToken);
                    return true;
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo actualizar el contacto: {response.Message}", "ContactosViewModel", "UpdateContactoAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al actualizar contacto", ex, "ContactosViewModel", "UpdateContactoAsync");
                return false;
            }
        }

        /// <summary>
        /// Elimina un contacto
        /// </summary>
        public async Task<bool> DeleteContactoAsync(long contactoId, CancellationToken cancellationToken = default)
        {
            try
            {
                await _logger.LogInformationAsync($"Eliminando contacto ID: {contactoId}", "ContactosViewModel", "DeleteContactoAsync");

                var response = await _contactoService.DeleteContactoAsync(contactoId, cancellationToken);

                if (response.Success)
                {
                    await _logger.LogInformationAsync($"Contacto {contactoId} eliminado exitosamente", "ContactosViewModel", "DeleteContactoAsync");
                    
                    // Recargar la lista de contactos
                    await LoadContactosAsync(cancellationToken);
                    return true;
                }
                else
                {
                    await _logger.LogWarningAsync($"No se pudo eliminar el contacto: {response.Message}", "ContactosViewModel", "DeleteContactoAsync");
                    return false;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error al eliminar contacto", ex, "ContactosViewModel", "DeleteContactoAsync");
                return false;
            }
        }
    }
}
