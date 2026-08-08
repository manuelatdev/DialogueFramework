using System;

namespace Neurohard.Prompter
{
    public sealed class DialogueOption
    {
        public string OptionId { get; }
        public DialogueLine Line { get; }
        public bool IsAvailable { get; }

        public DialogueOption(string optionId, DialogueLine line, bool isAvailable = true)
        {
            if (string.IsNullOrEmpty(optionId)) throw new ArgumentException("OptionId vacío.", nameof(optionId));
            OptionId = optionId;
            Line = line ?? throw new ArgumentNullException(nameof(line));
            IsAvailable = isAvailable;
        }
    }
}