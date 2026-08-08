using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Neurohard.Playwright.Io
{
    internal sealed class JsonParser
    {
        private readonly string _s;
        private int _i;
        private int _line = 1;

        private JsonParser(string source) => _s = source ?? string.Empty;

        public static JsonValue Parse(string source)
        {
            var p = new JsonParser(source);
            p.SkipWhitespace();
            var value = p.ParseValue();
            p.SkipWhitespace();
            if (p._i < p._s.Length)
                throw new GraphFormatException("Contenido sobrante tras el valor JSON raíz.", p._line);
            return value;
        }

        private JsonValue ParseValue()
        {
            SkipWhitespace();
            if (_i >= _s.Length) throw new GraphFormatException("Fin de archivo inesperado.", _line);

            switch (_s[_i])
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': { var line = _line; return JsonValue.Str(ParseString(), line); }
                case 't': Expect("true"); return JsonValue.Bool(true, _line);
                case 'f': Expect("false"); return JsonValue.Bool(false, _line);
                case 'n': Expect("null"); return JsonValue.Null(_line);
                default: return ParseNumber();
            }
        }

        private JsonValue ParseObject()
        {
            var line = _line;
            var members = new Dictionary<string, JsonValue>();
            _i++; // {
            SkipWhitespace();

            if (Peek() == '}') { _i++; return JsonValue.Object(members, line); }

            while (true)
            {
                SkipWhitespace();
                if (Peek() != '"') throw new GraphFormatException("Se esperaba el nombre de una propiedad.", _line);

                var key = ParseString();
                SkipWhitespace();
                if (Peek() != ':') throw new GraphFormatException($"Falta ':' tras la propiedad '{key}'.", _line);
                _i++;

                if (members.ContainsKey(key))
                    throw new GraphFormatException($"Propiedad duplicada '{key}'.", _line);

                members[key] = ParseValue();
                SkipWhitespace();

                var c = Peek();
                if (c == ',') { _i++; continue; }
                if (c == '}') { _i++; return JsonValue.Object(members, line); }
                throw new GraphFormatException("Se esperaba ',' o '}'.", _line);
            }
        }

        private JsonValue ParseArray()
        {
            var line = _line;
            var items = new List<JsonValue>();
            _i++; // [
            SkipWhitespace();

            if (Peek() == ']') { _i++; return JsonValue.Array(items, line); }

            while (true)
            {
                items.Add(ParseValue());
                SkipWhitespace();

                var c = Peek();
                if (c == ',') { _i++; continue; }
                if (c == ']') { _i++; return JsonValue.Array(items, line); }
                throw new GraphFormatException("Se esperaba ',' o ']'.", _line);
            }
        }

        private string ParseString()
        {
            _i++; // "
            var sb = new StringBuilder();

            while (_i < _s.Length)
            {
                var c = _s[_i++];
                if (c == '"') return sb.ToString();

                if (c != '\\') { if (c == '\n') _line++; sb.Append(c); continue; }

                if (_i >= _s.Length) break;
                var e = _s[_i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (_i + 4 > _s.Length) throw new GraphFormatException("Escape \\u incompleto.", _line);
                        sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        _i += 4;
                        break;
                    default: throw new GraphFormatException($"Escape desconocido '\\{e}'.", _line);
                }
            }
            throw new GraphFormatException("Cadena sin cerrar.", _line);
        }

        private JsonValue ParseNumber()
        {
            var line = _line;
            var start = _i;
            if (Peek() == '-') _i++;

            while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.' ||
                   _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-')) _i++;

            var text = _s.Substring(start, _i - start);
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new GraphFormatException($"Número no válido: '{text}'.", line);

            return JsonValue.Number(value, line);
        }

        private char Peek() => _i < _s.Length ? _s[_i] : '\0';

        private void Expect(string literal)
        {
            if (_i + literal.Length > _s.Length ||
                string.CompareOrdinal(_s, _i, literal, 0, literal.Length) != 0)
                throw new GraphFormatException($"Se esperaba '{literal}'.", _line);
            _i += literal.Length;
        }

        private void SkipWhitespace()
        {
            while (_i < _s.Length)
            {
                var c = _s[_i];
                if (c == '\n') { _line++; _i++; }
                else if (c == ' ' || c == '\t' || c == '\r') _i++;
                else if (c == '/' && _i + 1 < _s.Length && _s[_i + 1] == '/')
                {
                    while (_i < _s.Length && _s[_i] != '\n') _i++;   // comentario de línea
                }
                else break;
            }
        }
    }
}