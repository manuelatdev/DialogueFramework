using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    public sealed class DialogueLine
    {
        public LineId Id { get; }
        public string SpeakerId { get; }
        public IReadOnlyList<string> Tags { get; }

        public DialogueLine(LineId id, string speakerId = null, IReadOnlyList<string> tags = null)
        {
            if (id.IsEmpty) throw new ArgumentException("Una DialogueLine necesita un LineId.", nameof(id));
            Id = id;
            SpeakerId = speakerId ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
        }
    }
}