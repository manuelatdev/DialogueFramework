using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    /// <summary>
    /// Fuente que reproduce una lista fija de pasos, en orden.
    /// Si encuentra un paso Options, espera SelectOptionAsync pero ignora
    /// la elección: no hay ramas. Sirve para probar la UI de opciones.
    /// </summary>
    public sealed class LinearScript : IDialogueSource, ISerializableSource
    {
        private readonly IReadOnlyList<DialogueStep> _steps;
        private int _index;

        public LinearScript(IReadOnlyList<DialogueStep> steps)
            => _steps = steps ?? throw new ArgumentNullException(nameof(steps));

        public ValueTask StartAsync(DialogueContext context, CancellationToken ct)
        {
            _index = 0;
            return default;
        }

        public ValueTask<DialogueStep> AdvanceAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (_index >= _steps.Count)
                return new ValueTask<DialogueStep>(DialogueStep.Complete.Instance);

            return new ValueTask<DialogueStep>(_steps[_index++]);
        }

        public ValueTask SelectOptionAsync(string optionId, CancellationToken ct) => default;

        public string CaptureState() => _index.ToString();

        public void RestoreState(string state)
            => _index = int.TryParse(state, out var i) ? i : 0;
    }
}