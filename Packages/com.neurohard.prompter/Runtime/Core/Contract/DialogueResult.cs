using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    public enum DialogueOutcome
    {
        Completed,      // la fuente llegó a Complete
        Cancelled,      // cancelación explícita del llamante
        Interrupted,    // otra sesión con más prioridad la desplazó
        Failed          // error durante la reproducción
    }

    public readonly struct OptionChoice
    {
        public string OptionId { get; }
        public LineId LineId { get; }
        public OptionChoice(string optionId, LineId lineId) { OptionId = optionId; LineId = lineId; }
    }

    public sealed class DialogueResult
    {
        public DialogueOutcome Outcome { get; }
        public IReadOnlyList<OptionChoice> Choices { get; }
        public string FailureReason { get; }

        public bool WasCompleted => Outcome == DialogueOutcome.Completed;

        private DialogueResult(DialogueOutcome outcome, IReadOnlyList<OptionChoice> choices, string failureReason = null)
        {
            Outcome = outcome;
            Choices = choices ?? Array.Empty<OptionChoice>();
            FailureReason = failureReason;
        }

        public static DialogueResult Completed(IReadOnlyList<OptionChoice> choices = null)
            => new DialogueResult(DialogueOutcome.Completed, choices);

        public static DialogueResult Cancelled(IReadOnlyList<OptionChoice> choices = null)
            => new DialogueResult(DialogueOutcome.Cancelled, choices);

        public static DialogueResult Interrupted(IReadOnlyList<OptionChoice> choices = null)
            => new DialogueResult(DialogueOutcome.Interrupted, choices);

        public static DialogueResult Failed(string reason, IReadOnlyList<OptionChoice> choices = null)
            => new DialogueResult(DialogueOutcome.Failed, choices, reason);
    }
}