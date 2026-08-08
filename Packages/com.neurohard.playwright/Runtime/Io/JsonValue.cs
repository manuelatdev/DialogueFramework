using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neurohard.Playwright.Io
{
    /// <summary>Árbol JSON mínimo. Solo lo que necesita el formato de grafos.</summary>
    public sealed class JsonValue
    {
        public enum Kind { Null, Bool, Number, String, Array, Object }

        public Kind Type { get; private set; }
        public bool BoolValue { get; private set; }
        public double NumberValue { get; private set; }
        public string StringValue { get; private set; }
        public List<JsonValue> Items { get; private set; }
        public Dictionary<string, JsonValue> Members { get; private set; }
        public int Line { get; internal set; }

        public static JsonValue Null(int line) => new JsonValue { Type = Kind.Null, Line = line };
        public static JsonValue Bool(bool v, int line) => new JsonValue { Type = Kind.Bool, BoolValue = v, Line = line };
        public static JsonValue Number(double v, int line) => new JsonValue { Type = Kind.Number, NumberValue = v, Line = line };
        public static JsonValue Str(string v, int line) => new JsonValue { Type = Kind.String, StringValue = v, Line = line };
        public static JsonValue Array(List<JsonValue> items, int line) => new JsonValue { Type = Kind.Array, Items = items, Line = line };
        public static JsonValue Object(Dictionary<string, JsonValue> members, int line) => new JsonValue { Type = Kind.Object, Members = members, Line = line };

        public JsonValue this[string key]
            => Type == Kind.Object && Members.TryGetValue(key, out var v) ? v : null;

        public bool Has(string key) => Type == Kind.Object && Members.ContainsKey(key);

        public string AsString(string context)
            => Type == Kind.String ? StringValue
             : throw new GraphFormatException($"{context} debe ser una cadena.", Line);

        public double AsNumber(string context)
            => Type == Kind.Number ? NumberValue
             : throw new GraphFormatException($"{context} debe ser un número.", Line);

        public bool AsBool(string context)
            => Type == Kind.Bool ? BoolValue
             : throw new GraphFormatException($"{context} debe ser true o false.", Line);

        public List<JsonValue> AsArray(string context)
            => Type == Kind.Array ? Items
             : throw new GraphFormatException($"{context} debe ser un array.", Line);

        public JsonValue AsObject(string context)
            => Type == Kind.Object ? this
             : throw new GraphFormatException($"{context} debe ser un objeto.", Line);

        /// <summary>Valor sin tipar, para condiciones y efectos.</summary>
        public object AsLoose()
        {
            switch (Type)
            {
                case Kind.Bool: return BoolValue;
                case Kind.String: return StringValue;
                case Kind.Number:
                    return Math.Abs(NumberValue % 1) < double.Epsilon &&
                           NumberValue >= int.MinValue && NumberValue <= int.MaxValue
                        ? (object)(int)NumberValue : NumberValue;
                default: return null;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Write(sb);
            return sb.ToString();
        }

        private void Write(StringBuilder sb)
        {
            switch (Type)
            {
                case Kind.Null: sb.Append("null"); break;
                case Kind.Bool: sb.Append(BoolValue ? "true" : "false"); break;
                case Kind.Number: sb.Append(NumberValue.ToString("R", CultureInfo.InvariantCulture)); break;
                case Kind.String: WriteString(sb, StringValue); break;
                case Kind.Array:
                    sb.Append('[');
                    for (var i = 0; i < Items.Count; i++) { if (i > 0) sb.Append(','); Items[i].Write(sb); }
                    sb.Append(']');
                    break;
                case Kind.Object:
                    sb.Append('{');
                    var first = true;
                    foreach (var kv in Members)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        kv.Value.Write(sb);
                    }
                    sb.Append('}');
                    break;
            }
        }

        internal static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var c in s ?? string.Empty)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    public sealed class GraphFormatException : Exception
    {
        public int Line { get; }
        public GraphFormatException(string message, int line)
            : base(line > 0 ? $"[línea {line}] {message}" : message) => Line = line;
    }
}