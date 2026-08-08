using System;
using System.Collections.Generic;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    public enum AssignOp { Set, Add, Subtract }

    /// <summary>Lo que ocurre al recorrer una arista.</summary>
    public abstract class Effect
    {
        private Effect() { }

        public sealed class Assign : Effect
        {
            public string Variable { get; }
            public AssignOp Op { get; }
            public object Value { get; }

            public Assign(string variable, AssignOp op, object value)
            {
                if (string.IsNullOrEmpty(variable))
                    throw new ArgumentException("Efecto sin nombre de variable.", nameof(variable));
                Variable = variable; Op = op; Value = value;
            }

            public void Apply(IVariableStorage vars)
            {
                if (Op == AssignOp.Set) { vars.Set(Variable, Value); return; }

                vars.TryGet<object>(Variable, out var current);
                vars.Set(Variable, VariableMath.Add(current, Value, Op == AssignOp.Add ? 1 : -1));
            }
        }

        /// <summary>Se emite como DialogueStep.Command; Playwright no ejecuta nada del juego.</summary>
        public sealed class Command : Effect
        {
            public string Name { get; }
            public IReadOnlyList<string> Arguments { get; }

            public Command(string name, IReadOnlyList<string> arguments = null)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("Comando sin nombre.", nameof(name));
                Name = name; Arguments = arguments ?? Array.Empty<string>();
            }
        }
    }
}