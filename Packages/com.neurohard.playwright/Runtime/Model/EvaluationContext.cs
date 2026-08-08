using System;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    /// <summary>Todo lo que una condición necesita para evaluarse.</summary>
    public sealed class EvaluationContext
    {
        public IVariableStorage Variables { get; }
        public IQueryResolver Queries { get; }

        public EvaluationContext(IVariableStorage variables, IQueryResolver queries = null)
        {
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Queries = queries ?? new NoQueryResolver();
        }

        public static implicit operator EvaluationContext(InMemoryVariableStorage vars)
            => new EvaluationContext(vars);
    }
}