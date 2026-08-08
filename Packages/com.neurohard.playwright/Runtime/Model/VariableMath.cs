using System;
using System.Globalization;

namespace Neurohard.Playwright
{
    internal static class VariableMath
    {
        public static bool Compare(object a, object b, ComparisonOp op)
        {
            // Una variable sin definir cuenta como 0, pero solo frente a un número.
            // Contra una cadena sigue tratándose como cadena vacía, para que
            // "estado != hecho" sea true cuando la variable no existe.
            var aUnset = IsUnset(a);
            var bUnset = IsUnset(b);

            var aIsNum = TryAsDouble(a, out var da);
            var bIsNum = TryAsDouble(b, out var db);

            if (aUnset && bIsNum) { da = 0; aIsNum = true; }
            if (bUnset && aIsNum) { db = 0; bIsNum = true; }

            if (aIsNum && bIsNum)
            {
                var c = da.CompareTo(db);
                return op switch
                {
                    ComparisonOp.Equal => c == 0,
                    ComparisonOp.NotEqual => c != 0,
                    ComparisonOp.Greater => c > 0,
                    ComparisonOp.GreaterOrEqual => c >= 0,
                    ComparisonOp.Less => c < 0,
                    ComparisonOp.LessOrEqual => c <= 0,
                    _ => false
                };
            }

            if (a is bool ba && b is bool bb)
                return op == ComparisonOp.Equal ? ba == bb
                     : op == ComparisonOp.NotEqual ? ba != bb
                     : false;

            var sa = Convert.ToString(a, CultureInfo.InvariantCulture) ?? string.Empty;
            var sb = Convert.ToString(b, CultureInfo.InvariantCulture) ?? string.Empty;
            var sc = string.CompareOrdinal(sa, sb);

            return op switch
            {
                ComparisonOp.Equal => sc == 0,
                ComparisonOp.NotEqual => sc != 0,
                _ => false
            };
        }

        private static bool IsUnset(object value)
            => value == null || (value is string s && string.IsNullOrWhiteSpace(s));

        public static object Add(object current, object delta, int sign)
        {
            var currentVal = IsUnset(current) ? 0 : current;
            var deltaVal = IsUnset(delta) ? 0 : delta;

            if (TryAsDouble(currentVal, out var dc) && TryAsDouble(deltaVal, out var dd))
            {
                var result = dc + sign * dd;

                // Solo colapsa a int si el valor original ya era int.
                if (currentVal is int && Math.Abs(result % 1) < double.Epsilon)
                    return (int)result;

                return result;
            }

            throw new InvalidOperationException(
                $"No se puede sumar '{delta}' a '{current}': ambos deben ser numéricos.");
        }

        private static bool TryAsDouble(object value, out double result)
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case string s when !string.IsNullOrWhiteSpace(s) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p):
                    result = p;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }
    }
}