using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    public sealed class ResolvedLine
    {
        public LineId Id { get; }
        public string SpeakerId { get; }
        public string Text { get; }
        public IReadOnlyList<string> Tags { get; }

        public ResolvedLine(LineId id, string text, string speakerId = null, IReadOnlyList<string> tags = null)
        {
            Id = id;
            Text = text ?? string.Empty;
            SpeakerId = speakerId ?? string.Empty;
            Tags = tags ?? Array.Empty<string>();
        }
    }

    public sealed class ResolvedOption
    {
        public string OptionId { get; }
        public ResolvedLine Line { get; }
        public bool IsAvailable { get; }

        public ResolvedOption(string optionId, ResolvedLine line, bool isAvailable = true)
        {
            OptionId = optionId;
            Line = line ?? throw new ArgumentNullException(nameof(line));
            IsAvailable = isAvailable;
        }
    }
}