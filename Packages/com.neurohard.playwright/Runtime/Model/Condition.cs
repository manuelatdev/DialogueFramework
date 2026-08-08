using System;
using System.Collections.Generic;

namespace Neurohard.Playwright
{
    public enum ComparisonOp { Equal, NotEqual, Greater, GreaterOrEqual, Less, LessOrEqual, Exists, NotExists }

    /// <summary>Condición evaluable contra variables y consultas al juego.</summary>
    public abstract class Condition
    {
        private Condition() { }

        public abstract bool Evaluate(EvaluationContext ctx);

        public sealed class Always : Condition
        {
            public static readonly Always Instance = new Always();
            private Always() { }
            public override bool Evaluate(EvaluationContext ctx) => true;
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

            public override bool Evaluate(EvaluationContext ctx)
            {
                var vars = ctx.Variables;

                if (Op == ComparisonOp.Exists) return vars.Has(Variable);
                if (Op == ComparisonOp.NotExists) return !vars.Has(Variable);

                vars.TryGet<object>(Variable, out var actual);
                return VariableMath.Compare(actual, ValueResolver.Resolve(Value, vars), Op);
            }
        }

        public sealed class All : Condition
        {
            public IReadOnlyList<Condition> Items { get; }
            public All(IReadOnlyList<Condition> items) => Items = items ?? Array.Empty<Condition>();

            public override bool Evaluate(EvaluationContext ctx)
            {
                foreach (var c in Items) if (!c.Evaluate(ctx)) return false;
                return true;
            }
        }

        public sealed class Any : Condition
        {
            public IReadOnlyList<Condition> Items { get; }
            public Any(IReadOnlyList<Condition> items) => Items = items ?? Array.Empty<Condition>();

            public override bool Evaluate(EvaluationContext ctx)
            {
                foreach (var c in Items) if (c.Evaluate(ctx)) return true;
                return false;
            }
        }

        public sealed class Not : Condition
        {
            public Condition Inner { get; }
            public Not(Condition inner) => Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            public override bool Evaluate(EvaluationContext ctx) => !Inner.Evaluate(ctx);
        }

        /// <summary>Consulta al juego a través de IQueryResolver.</summary>
        public sealed class Query : Condition
        {
            public string Name { get; }
            public IReadOnlyList<string> Arguments { get; }
            public ComparisonOp Op { get; }
            public object Value { get; }

            /// <summary>Sin op ni value, se interpreta como "la consulta es verdadera".</summary>
            public Query(string name, IReadOnlyList<string> arguments = null,
                         ComparisonOp op = ComparisonOp.Equal, object value = null)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Consulta sin nombre.", nameof(name));
                Name = name;
                Arguments = arguments ?? Array.Empty<string>();
                Op = op;
                Value = value;
            }

            public override bool Evaluate(EvaluationContext ctx)
            {
                if (!ctx.Queries.CanResolve(Name)) return false;

                var result = ctx.Queries.Resolve(Name, Arguments);

                if (Op == ComparisonOp.Equal && Value == null)
                    return result is bool b ? b : result != null;

                return VariableMath.Compare(result, ValueResolver.Resolve(Value, ctx.Variables), Op);
            }
        }
    }
}