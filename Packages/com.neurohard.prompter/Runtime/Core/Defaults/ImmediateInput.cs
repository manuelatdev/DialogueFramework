using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    public sealed class ImmediateInput : IDialogueInput
    {
        public Task WaitForAdvanceAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<string> WaitForSelectionAsync(IReadOnlyList<ResolvedOption> options, CancellationToken ct)
        {
            foreach (var o in options)
                if (o.IsAvailable) return Task.FromResult(o.OptionId);
            return Task.FromResult(options[0].OptionId);
        }
    }
}