using System;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    /// <summary>
    /// Referencia a otra variable, usable donde iría un literal.
    /// En JSON se escribe { "$var": "precio" }.
    /// </summary>
    public sealed class VariableRef
    {
        public string Name { get; }

        public VariableRef(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Referencia a variable sin nombre.", nameof(name));
            Name = name;
        }

        /// <summary>Devuelve el valor actual, o null si no está definida.</summary>
        public object Resolve(IVariableStorage vars)
        {
            vars.TryGet<object>(Name, out var value);
            return value;
        }

        public override string ToString() => $"${Name}";
    }

    internal static class ValueResolver
    {
        /// <summary>Desreferencia si hace falta; el resto pasa tal cual.</summary>
        public static object Resolve(object value, IVariableStorage vars)
            => value is VariableRef reference ? reference.Resolve(vars) : value;
    }
}