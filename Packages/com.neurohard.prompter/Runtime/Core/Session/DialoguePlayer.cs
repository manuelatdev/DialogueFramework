using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    /// <summary>Reproductor de diálogo. Reutilizable; una sesión activa a la vez.</summary>
    public sealed class DialoguePlayer
    {
        private readonly DialoguePlayerOptions _options;
        private int _busy;

        public DialoguePlayer(DialoguePlayerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            _options = options.Materialize();
        }

        public bool IsPlaying => Volatile.Read(ref _busy) == 1;

        public Task<DialogueResult> Play(IDialogueSource source, CancellationToken ct = default)
            => Play(source, null, ct);

        public async Task<DialogueResult> Play(
            IDialogueSource source,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken ct = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            if (Interlocked.CompareExchange(ref _busy, 1, 0) == 1)
                throw new InvalidOperationException(
                    "Ya hay una conversación en curso. Espera a que termine, " +
                    "cancélala con su CancellationToken, o usa otra instancia. " +
                    "La cola con prioridades llegará en una versión posterior.");

            try
            {
                return await new DialogueSession(source, _options).RunAsync(parameters, ct);
            }
            finally
            {
                Volatile.Write(ref _busy, 0);
            }
        }
    }
}