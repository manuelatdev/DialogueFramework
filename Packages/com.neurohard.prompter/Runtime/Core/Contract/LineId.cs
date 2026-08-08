using System;

namespace Neurohard.Prompter
{
    /// <summary>Identificador de una línea. Nunca contiene el texto final.</summary>
    public readonly struct LineId : IEquatable<LineId>
    {
        public string Value { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Value);

        public LineId(string value) => Value = value;

        public bool Equals(LineId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LineId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();
        public override string ToString() => Value ?? "<vacío>";

        public static implicit operator LineId(string value) => new LineId(value);
    }
}