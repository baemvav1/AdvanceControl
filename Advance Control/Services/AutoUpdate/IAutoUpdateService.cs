using System.Threading;
using System.Threading.Tasks;

namespace Advance_Control.Services.AutoUpdate
{
    public interface IAutoUpdateService
    {
        Task CheckAndPromptAsync(CancellationToken ct = default);
    }
}
