using System;
using System.Collections.Generic;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    public enum ComparisonOp { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual, Exists, NotExists }

    /// <summary>Condición evaluable contra el almacén de variables.</summary>
    public abstract class Condition
    {
        private Condition() { }

        public abstract bool Evaluate(IVariableStorage vars);

        public sealed class Always : Condition
        {
            public static readonly Always Instance = new Always();
            private Always() { }
            public override bool Evaluate(IVariableStorage vars) => true;
        }

        public sealed class Compare : Condition
        {
            public string Variable { get; }
            public ComparisonOp Op { get; }
            public object Value { get; }

            public Compare(string variable, ComparisonOp op, object value)
            {
                if (string.IsNullOrEmpty(variable))
                    throw new ArgumentException("Condición sin nombre de variable.", nameof(variable));
                Variable = variable; Op = op; Value = value;
            }

            public override bool Evaluate(IVariableStorage vars)
            {
                if (Op == ComparisonOp.Exists) return vars.Has(Variable);
                if (Op == ComparisonOp.NotExists) return !vars.Has(Variable);

                if (!vars.TryGet<object>(Variable, out var actual)) return false;
                return VariableMath.Compare(actual, Value, Op);
            }
        }

        public sealed class All : Condition
        {
            public IReadOnlyList<Condition> Items { get; }
            public All(IReadOnlyList<Condition> items) => Items = items ?? Array.Empty<Condition>();
            public override bool Evaluate(IVariableStorage vars)
            {
                foreach (var c in Items) if (!c.Evaluate(vars)) return false;
                return true;
            }
        }

        public sealed class Any : Condition
        {
            public IReadOnlyList<Condition> Items { get; }
            public Any(IReadOnlyList<Condition> items) => Items = items ?? Array.Empty<Condition>();
            public override bool Evaluate(IVariableStorage vars)
            {
                foreach (var c in Items) if (c.Evaluate(vars)) return true;
                return false;
            }
        }

        public sealed class Not : Condition
        {
            public Condition Inner { get; }
            public Not(Condition inner) => Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            public override bool Evaluate(IVariableStorage vars) => !Inner.Evaluate(vars);
        }
    }
}