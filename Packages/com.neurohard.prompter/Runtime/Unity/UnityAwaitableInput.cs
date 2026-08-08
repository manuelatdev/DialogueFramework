using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter.Unity
{
    /// <summary>
    /// Input pasivo: espera a que alguien llame a Advance() o Select().
    /// Conéctalo a tus botones, a tu Input Action o a lo que uses.
    /// </summary>
    public sealed class UnityAwaitableInput : IDialogueInput
    {
        private TaskCompletionSource<bool> _advance;
        private TaskCompletionSource<string> _selection;

        /// <summary>Opciones a la espera de selección, o null.</summary>
        public IReadOnlyList<ResolvedOption> PendingOptions { get; private set; }

        public bool IsWaitingForAdvance => _advance != null && !_advance.Task.IsCompleted;

        public Task WaitForAdvanceAsync(CancellationToken ct)
        {
            _advance = New<bool>();
            var tcs = _advance;
            ct.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public Task<string> WaitForSelectionAsync(IReadOnlyList<ResolvedOption> options, CancellationToken ct)
        {
            PendingOptions = options;
            _selection = New<string>();
            var tcs = _selection;
            ct.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }

        /// <summary>Confirma la línea actual. Ignorado si no hay espera activa.</summary>
        public void Advance() => _advance?.TrySetResult(true);

        /// <summary>Elige una opción por id. Ignora ids desconocidos.</summary>
        public void Select(string optionId)
        {
            var options = PendingOptions;
            if (options == null) return;

            foreach (var o in options)
                if (o.OptionId == optionId && o.IsAvailable)
                {
                    PendingOptions = null;
                    _selection?.TrySetResult(optionId);
                    return;
                }
        }

        public void SelectIndex(int index)
        {
            var options = PendingOptions;
            if (options != null && index >= 0 && index < options.Count)
                Select(options[index].OptionId);
        }

        private static TaskCompletionSource<T> New<T>()
            => new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}