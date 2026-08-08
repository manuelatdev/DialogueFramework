using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Neurohard.Prompter.Samples
{
    /// <summary>
    /// Presentador mínimo: escribe en la consola de Unity con un retardo
    /// proporcional a la longitud del texto, para simular un efecto de escritura.
    /// </summary>
    public sealed class ConsolePresenter : IDialoguePresenter
    {
        private readonly float _secondsPerChar;
        private volatile bool _skipRequested;

        public ConsolePresenter(float secondsPerChar = 0.03f)
            => _secondsPerChar = secondsPerChar;

        public async Task ShowLineAsync(ResolvedLine line, CancellationToken ct)
        {
            await PresentAsync(Format(line), ct);
        }

        public async Task ShowOptionsAsync(
            IReadOnlyList<ResolvedOption> options, ResolvedLine prompt, CancellationToken ct)
        {
            if (prompt != null)
                await PresentAsync(Format(prompt), ct);

            for (var i = 0; i < options.Count; i++)
            {
                var o = options[i];
                var marca = o.IsAvailable ? " " : "×";
                Debug.Log($"  [{i + 1}]{marca} {o.Line.Text}");
            }
        }

        public void SkipCurrentPresentation() => _skipRequested = true;

        public Task ClearAsync(CancellationToken ct)
        {
            Debug.Log("— fin de la conversación —");
            return Task.CompletedTask;
        }

        private async Task PresentAsync(string text, CancellationToken ct)
        {
            _skipRequested = false;

            var remaining = text.Length * _secondsPerChar;
            while (remaining > 0f && !_skipRequested)
            {
                ct.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(ct);
                remaining -= Time.deltaTime;
            }

            Debug.Log(text + (_skipRequested ? "  (saltado)" : string.Empty));
        }

        private static string Format(ResolvedLine line)
            => string.IsNullOrEmpty(line.SpeakerId) ? line.Text : $"{line.SpeakerId}: {line.Text}";
    }
}