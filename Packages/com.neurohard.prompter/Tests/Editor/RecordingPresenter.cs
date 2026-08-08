using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter.Tests
{
    internal sealed class RecordingPresenter : IDialoguePresenter
    {
        public List<ResolvedLine> LinesShown { get; } = new List<ResolvedLine>();
        public List<IReadOnlyList<ResolvedOption>> OptionsShown { get; } = new List<IReadOnlyList<ResolvedOption>>();
        public int ClearCount { get; private set; }
        public int SkipCount { get; private set; }

        public Task ShowLineAsync(ResolvedLine line, CancellationToken ct)
        {
            LinesShown.Add(line);
            return Task.CompletedTask;
        }

        public Task ShowOptionsAsync(IReadOnlyList<ResolvedOption> options, ResolvedLine prompt, CancellationToken ct)
        {
            OptionsShown.Add(options);
            return Task.CompletedTask;
        }

        public void SkipCurrentPresentation() => SkipCount++;

        public Task ClearAsync(CancellationToken ct)
        {
            ClearCount++;
            return Task.CompletedTask;
        }
    }
}