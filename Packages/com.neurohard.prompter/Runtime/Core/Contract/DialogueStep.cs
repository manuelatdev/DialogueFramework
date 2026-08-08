using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    /// <summary>
    /// Unión cerrada: solo existen Line, Options, Command y Complete.
    /// El constructor privado impide que nadie de fuera añada variantes.
    /// </summary>
    public abstract class DialogueStep
    {
        private DialogueStep() { }

        public sealed class Line : DialogueStep
        {
            public DialogueLine Value { get; }
            public Line(DialogueLine value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public sealed class Options : DialogueStep
        {
            /// <summary>Línea que introduce las opciones. Puede ser null.</summary>
            public DialogueLine Prompt { get; }
            public IReadOnlyList<DialogueOption> Choices { get; }

            public Options(IReadOnlyList<DialogueOption> choices, DialogueLine prompt = null)
            {
                if (choices == null || choices.Count == 0)
                    throw new ArgumentException("Un paso Options necesita al menos una opción.", nameof(choices));
                Choices = choices;
                Prompt = prompt;
            }
        }

        public sealed class Command : DialogueStep
        {
            public string Name { get; }
            public IReadOnlyList<string> Arguments { get; }

            public Command(string name, IReadOnlyList<string> arguments = null)
            {
                if (string.IsNullOrEmpty(name)) throw new ArgumentException("Comando sin nombre.", nameof(name));
                Name = name;
                Arguments = arguments ?? Array.Empty<string>();
            }
        }

        public sealed class Complete : DialogueStep
        {
            public static readonly Complete Instance = new Complete();
            private Complete() { }
        }
    }
}