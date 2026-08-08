using System.Linq;

namespace Neurohard.Playwright.Editor
{
    internal static class ConditionText
    {
        public static string Describe(Condition condition)
        {
            switch (condition)
            {
                case Condition.Always _: return "siempre";
                case Condition.Not not: return $"no({Describe(not.Inner)})";
                case Condition.All all: return string.Join(" y ", all.Items.Select(Describe));
                case Condition.Any any: return string.Join(" o ", any.Items.Select(Describe));
                case Condition.Compare cmp:
                    if (cmp.Op == ComparisonOp.Exists) return $"existe {cmp.Variable}";
                    if (cmp.Op == ComparisonOp.NotExists) return $"no existe {cmp.Variable}";
                    return $"{cmp.Variable} {Op(cmp.Op)} {cmp.Value}";
                default: return "?";
            }
        }

        private static string Op(ComparisonOp op) => op switch
        {
            ComparisonOp.Equal => "==",
            ComparisonOp.NotEqual => "!=",
            ComparisonOp.Greater => ">",
            ComparisonOp.GreaterOrEqual => ">=",
            ComparisonOp.Less => "<",
            _ => "<="
        };
    }
}