using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Neurohard.Prompter.Unity
{
    /// <summary>
    /// Aloja un DialoguePlayer y lo ata al ciclo de vida del GameObject.
    /// Cancela la conversación en curso al destruirse o deshabilitarse.
    /// </summary>
    [AddComponentMenu("Neurohard/Prompter")]
    public sealed class PrompterBehaviour : MonoBehaviour
    {
        [Tooltip("Cancelar la conversación al deshabilitar el objeto, no solo al destruirlo.")]
        [SerializeField] private bool cancelOnDisable = true;

        private readonly List<IDialoguePresenter> _presenters = new List<IDialoguePresenter>();
        private DialoguePlayer _player;
        private CancellationTokenSource _cts;

        public IVariableStorage Variables { get; set; }
        public ILineProvider LineProvider { get; set; }
        public ICommandDispatcher Commands { get; set; }
        public IQueryResolver Queries { get; set; }
        public IDialogueInput Input { get; set; }

        public bool IsPlaying => _player?.IsPlaying ?? false;

        /// <summary>Se emite al terminar una conversación, sea cual sea el motivo.</summary>
        public event Action<DialogueResult> Finished;

        /// <summary>
        /// Registra un presentador. Debe llamarse antes del primer Play; después,
        /// el player ya está construido y los cambios no tienen efecto.
        /// </summary>
        public void AddPresenter(IDialoguePresenter presenter)
        {
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            if (_player != null)
                throw new InvalidOperationException(
                    "No se pueden añadir presentadores después del primer Play. " +
                    "Regístralos en Awake o antes de la primera conversación.");
            _presenters.Add(presenter);
        }

        public Task<DialogueResult> Play(IDialogueSource source)
            => Play(source, null);

        public async Task<DialogueResult> Play(
            IDialogueSource source, IReadOnlyDictionary<string, string> parameters)
        {
            EnsurePlayer();

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            var result = await _player.Play(source, parameters, _cts.Token);
            Finished?.Invoke(result);
            return result;
        }

        /// <summary>Para llamar desde un UnityEvent o un botón del inspector.</summary>
        public void PlayAndForget(IDialogueSource source) => _ = Play(source);

        public void Cancel() => _cts?.Cancel();

        private void EnsurePlayer()
        {
            if (_player != null) return;

            var options = new DialoguePlayerOptions
            {
                Variables = Variables,
                LineProvider = LineProvider,
                Commands = Commands,
                Queries = Queries,
                Input = Input,
                Log = UnityLog
            };

            foreach (var p in _presenters) options.Presenters.Add(p);

            _player = new DialoguePlayer(options);
        }

        private static void UnityLog(string message) => Debug.LogWarning(message);

        private void OnDisable() { if (cancelOnDisable) Cancel(); }

        private void OnDestroy()
        {
            Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}