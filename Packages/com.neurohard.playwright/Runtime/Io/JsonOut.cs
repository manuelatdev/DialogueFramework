using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neurohard.Playwright.Io
{
    /// <summary>Nodo de salida. Sin estado: se construye y luego se formatea.</summary>
    internal abstract class JsonOut
    {
        public static JsonOut Str(string v) => new Raw(Escape(v));
        public static JsonOut Bool(bool v) => new Raw(v ? "true" : "false");
        public static JsonOut Null() => new Raw("null");

        public static JsonOut Num(double v)
            => new Raw(System.Math.Abs(v % 1) < double.Epsilon && v >= int.MinValue && v <= int.MaxValue
                ? ((int)v).ToString(CultureInfo.InvariantCulture)
                : v.ToString("R", CultureInfo.InvariantCulture));

        public static JsonOut Loose(object v) => v switch
        {
            null => Null(),
            bool b => Bool(b),
            string s => Str(s),
            int i => Num(i),
            float f => Num(f),
            double d => Num(d),
            VariableRef r => new JsonObj().Set("$var", r.Name),
            _ => Str(v.ToString())
        };

        public abstract void Render(StringBuilder sb, int depth, bool indent);

        private sealed class Raw : JsonOut
        {
            private readonly string _text;
            public Raw(string text) => _text = text;
            public override void Render(StringBuilder sb, int depth, bool indent) => sb.Append(_text);
        }

        private static string Escape(string s)
        {
            var sb = new StringBuilder();
            JsonValue.WriteString(sb, s);
            return sb.ToString();
        }
    }

    internal sealed class JsonArr : JsonOut
    {
        private readonly List<JsonOut> _items = new List<JsonOut>();
        public int Count => _items.Count;

        public JsonArr Add(JsonOut item) { if (item != null) _items.Add(item); return this; }

        public override void Render(StringBuilder sb, int depth, bool indent)
        {
            if (_items.Count == 0) { sb.Append("[]"); return; }
            sb.Append('[');
            for (var i = 0; i < _items.Count; i++)
            {
                if (i > 0) sb.Append(',');
                Pad(sb, depth + 1, indent);
                _items[i].Render(sb, depth + 1, indent);
            }
            Pad(sb, depth, indent);
            sb.Append(']');
        }

        internal static void Pad(StringBuilder sb, int depth, bool indent)
        {
            if (!indent) return;
            sb.Append('\n').Append(' ', depth * 2);
        }
    }

  internal sealed class JsonObj : JsonOut
{
    private readonly List<KeyValuePair<string, JsonOut>> _members
        = new List<KeyValuePair<string, JsonOut>>();

    /// <summary>Un valor null se omite: así los defaults no ensucian el diff.</summary>
    public JsonObj Set(string key, JsonOut value)
    {
        if (value != null) _members.Add(new KeyValuePair<string, JsonOut>(key, value));
        return this;
    }

    /// <summary>Las cadenas vacías o nulas se omiten.</summary>
    public JsonObj Set(string key, string value)
        => string.IsNullOrEmpty(value) ? this : Set(key, Str(value));

    public JsonObj Set(string key, int value) => Set(key, Num(value));
    public JsonObj Set(string key, float value) => Set(key, Num(value));
    public JsonObj Set(string key, double value) => Set(key, Num(value));

    /// <summary>
    /// Ojo: escribe el valor siempre, también cuando es false. Para omitir los
    /// false por defecto, usa SetIf.
    /// </summary>
    public JsonObj Set(string key, bool value) => Set(key, Bool(value));

    public JsonObj SetIf(bool condition, string key, JsonOut value)
        => condition ? Set(key, value) : this;

    public override void Render(StringBuilder sb, int depth, bool indent)
    {
            if (_members.Count == 0) { sb.Append("{}"); return; }
            sb.Append('{');
            for (var i = 0; i < _members.Count; i++)
            {
                if (i > 0) sb.Append(',');
                JsonArr.Pad(sb, depth + 1, indent);
                var sbKey = new StringBuilder();
                JsonValue.WriteString(sbKey, _members[i].Key);
                sb.Append(sbKey).Append(indent ? ": " : ":");
                _members[i].Value.Render(sb, depth + 1, indent);
            }
            JsonArr.Pad(sb, depth, indent);
            sb.Append('}');
        }
    }
}