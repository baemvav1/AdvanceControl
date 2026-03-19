using Advance_Control.Rules;

namespace Advance_Control.Services.Conciliacion
{
    public interface IConciliacionRulesProvider
    {
        ConciliacionRules GetCurrentRules();
    }
}
