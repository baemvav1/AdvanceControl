using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.PermisosUi
{
    public interface IPermisoUiRuntimeService
    {
        bool IsInitialized { get; }
        int NivelUsuario { get; }
        IReadOnlyDictionary<string, PermisoModuloDto> Modulos { get; }
        Task InitializeAsync(int nivelUsuario, bool forceSync = false, CancellationToken cancellationToken = default);
        void Reset();
        bool CanAccessModule(string moduleKey);
        bool CanAccessAction(string actionKey);
        bool TryGetModulo(string moduleKey, out PermisoModuloDto? modulo);
        string BuildModuleKey(Type moduleType);
        string BuildActionKey(string moduleKey, string controlType, string controlKey);
    }
}
