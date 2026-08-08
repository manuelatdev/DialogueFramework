using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    /// <summary>De dónde salen los pasos. Un guion, un árbol, un generador.</summary>
    public interface IDialogueSource
    {
        /// <summary>Prepara la fuente. Se llama una vez antes del primer AdvanceAsync.</summary>
        ValueTask StartAsync(DialogueContext context, CancellationToken ct);

        /// <summary>Devuelve el siguiente paso. El runner no interpreta, solo obedece.</summary>
        ValueTask<DialogueStep> AdvanceAsync(CancellationToken ct);

        /// <summary>Comunica la opción elegida. Solo válido tras un paso Options.</summary>
        ValueTask SelectOptionAsync(string optionId, CancellationToken ct);
    }

    /// <summary>Lo que el runner pone a disposición de la fuente al arrancar.</summary>
    public sealed class DialogueContext
    {
        public IVariableStorage Variables { get; }
        public IReadOnlyDictionary<string, string> Parameters { get; }

        public DialogueContext(IVariableStorage variables,
                               IReadOnlyDictionary<string, string> parameters = null)
        {
            Variables = variables;
            Parameters = parameters ?? new Dictionary<string, string>();
        }
    }
}