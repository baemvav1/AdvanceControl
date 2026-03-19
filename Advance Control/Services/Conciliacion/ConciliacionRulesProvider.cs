using Advance_Control.Rules;

namespace Advance_Control.Services.Conciliacion
{
    public sealed class ConciliacionRulesProvider : IConciliacionRulesProvider
    {
        private static readonly ConciliacionRules DefaultRules = new();

        public ConciliacionRules GetCurrentRules() => DefaultRules;
    }
}
