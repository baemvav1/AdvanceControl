using System.Collections.Generic;

namespace Advance_Control.Models
{
    public class AdvertenciasImportacionPdfDto
    {
        public List<string> SinClasificar { get; set; } = new();
        public List<string> ErroresSaldo { get; set; } = new();
        public bool SaldoCuadra { get; set; }
        public bool DepositosCuadran { get; set; }
        public bool RetirosCuadran { get; set; }

        public bool TieneProblemas => !SaldoCuadra || !DepositosCuadran || !RetirosCuadran || ErroresSaldo.Count > 0;
    }
}
