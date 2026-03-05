using System.Threading.Tasks;
using Advance_Control.Models;

namespace Advance_Control.Services.CheckOperacion
{
    public interface ICheckOperacionService
    {
        /// <summary>Obtiene (o crea) el check de pasos de una operación.</summary>
        Task<CheckOperacionDto?> GetAsync(int idOperacion);

        /// <summary>Actualiza un campo específico del check.</summary>
        Task<bool> UpdateCampoAsync(int idOperacion, string campo, bool valor = true);
    }
}
