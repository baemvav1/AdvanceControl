using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class EstadoCuentaDetalleDto
    {
        public EstadoCuentaResumenDto? EstadoCuenta { get; set; }
        public List<EstadoCuentaGrupoDetalleDto> Grupos { get; set; } = new();
    }
}
