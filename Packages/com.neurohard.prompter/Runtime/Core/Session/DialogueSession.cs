using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    internal sealed class DialogueSession
    {
        private readonly IDialogueSource _source;
        private readonly DialoguePlayerOptions _options;
        private readonly List<IDialoguePresenter> _presenters;
        private readonly List<OptionChoice> _choices = new List<OptionChoice>();

        public DialogueSession(IDialogueSource source, DialoguePlayerOptions options)
        {
            _source = source;
            _options = options;
            _presenters = new List<IDialoguePresenter>(options.Presenters);
        }

        public async Task<DialogueResult> RunAsync(
            IReadOnlyDictionary<string, string> parameters, CancellationToken ct)
        {
            try
            {
                var context = new DialogueContext(_options.Variables, parameters);
                await _source.StartAsync(context, ct);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    var step = await _source.AdvanceAsync(ct);

                    switch (step)
                    {
                        case DialogueStep.Line line:
                            await RunLineAsync(line, ct);
                            break;

                        case DialogueStep.Options options:
                            await RunOptionsAsync(options, ct);
                            break;

                        case DialogueStep.Command command:
                            await RunCommandAsync(command, ct);
                            break;

                        case DialogueStep.Complete _:
                            await ClearAllAsync(CancellationToken.None);
                            return DialogueResult.Completed(_choices);

                        default:
                            throw new InvalidOperationException(
                                $"Variante de DialogueStep no contemplada: {step.GetType().Name}");
                    }

                    if (_presenters.Count == 0)
                        return DialogueResult.Failed(
                            "Todos los presentadores fallaron; la sesión no puede continuar.", _choices);
                }
            }
            catch (OperationCanceledException)
            {
                await ClearAllAsync(CancellationToken.None);
                return DialogueResult.Cancelled(_choices);
            }
            catch (Exception ex)
            {
                _options.Log($"[Prompter] La sesión falló: {ex}");
                await ClearAllAsync(CancellationToken.None);
                return DialogueResult.Failed(ex.Message, _choices);
            }
        }

        // --- pasos -----------------------------------------------------------

        private async Task RunLineAsync(DialogueStep.Line step, CancellationToken ct)
        {
            var resolved = Resolve(step.Value);
            await PresentAndWaitAsync(
                p => p.ShowLineAsync(resolved, ct),
                () => _options.Input.WaitForAdvanceAsync(ct),
                ct);
        }

        private async Task RunOptionsAsync(DialogueStep.Options step, CancellationToken ct)
        {
            var prompt = step.Prompt != null ? Resolve(step.Prompt) : null;
            var resolved = step.Choices
                .Select(c => new ResolvedOption(c.OptionId, Resolve(c.Line), c.IsAvailable, c.UnavailableReason))
                .ToArray();

            if (!resolved.Any(o => o.IsAvailable))
                throw new InvalidOperationException(
                    "Un paso Options no tiene ninguna opción disponible; la conversación quedaría bloqueada.");

            string selected = null;
            await PresentAndWaitAsync(
                p => p.ShowOptionsAsync(resolved, prompt, ct),
                async () => selected = await _options.Input.WaitForSelectionAsync(resolved, ct),
                ct);

            var chosen = resolved.FirstOrDefault(o => o.OptionId == selected)
                ?? throw new InvalidOperationException(
                    $"El input devolvió el OptionId '{selected}', que no está entre las opciones ofrecidas.");

            _choices.Add(new OptionChoice(chosen.OptionId, chosen.Line.Id));
            await _source.SelectOptionAsync(chosen.OptionId, ct);
        }

        private async Task RunCommandAsync(DialogueStep.Command command, CancellationToken ct)
        {
            if (!_options.Commands.CanHandle(command.Name))
            {
                _options.Log($"[Prompter] Comando '{command.Name}' sin manejador; se omite.");
                return;
            }
            await _options.Commands.DispatchAsync(command, ct);
        }

        // --- núcleo del ciclo mostrar → esperar ------------------------------

        /// <summary>
        /// Presenta en paralelo y espera input. Si el jugador pulsa antes de que
        /// la presentación acabe, la completa de golpe y espera una segunda pulsación.
        /// </summary>
        private async Task PresentAndWaitAsync(
            Func<IDialoguePresenter, Task> present, Func<Task> waitForInput, CancellationToken ct)
        {
            var presentation = RunOnAllAsync(present, ct);
            var input = waitForInput();

            var winner = await Task.WhenAny(presentation, input);

            if (winner == input)
            {
                foreach (var p in _presenters.ToArray())
                    Guard(p, () => p.SkipCurrentPresentation());

                await presentation;
                await input;               // ya completada; propaga excepción si la hubo
                return;
            }

            await presentation;
            await input;
        }

        /// <summary>
        /// Ejecuta la operación en todos los presentadores en paralelo. Un presentador
        /// que falle se desregistra de la sesión y no tumba a los demás.
        /// </summary>
        private async Task RunOnAllAsync(Func<IDialoguePresenter, Task> operation, CancellationToken ct)
        {
            var snapshot = _presenters.ToArray();
            var tasks = new Task[snapshot.Length];

            for (var i = 0; i < snapshot.Length; i++)
            {
                try { tasks[i] = operation(snapshot[i]) ?? Task.CompletedTask; }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { Drop(snapshot[i], ex); tasks[i] = Task.CompletedTask; }
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                for (var i = 0; i < snapshot.Length; i++)
                    if (tasks[i].IsFaulted)
                        Drop(snapshot[i], tasks[i].Exception);
            }

            ct.ThrowIfCancellationRequested();
        }

        private async Task ClearAllAsync(CancellationToken ct)
        {
            try { await RunOnAllAsync(p => p.ClearAsync(ct), ct); }
            catch (Exception ex) { _options.Log($"[Prompter] Error al limpiar: {ex.Message}"); }
        }

        // --- utilidades ------------------------------------------------------

        private ResolvedLine Resolve(DialogueLine line)
        {
            var resolved = _options.LineProvider.Resolve(line);
            if (resolved != null) return resolved;

            _options.Log($"[Prompter] ILineProvider devolvió null para '{line.Id}'.");
            return new ResolvedLine(line.Id, $"[falta: {line.Id}]", line.SpeakerId, line.Tags);
        }

        private void Guard(IDialoguePresenter presenter, Action action)
        {
            try { action(); }
            catch (Exception ex) { Drop(presenter, ex); }
        }

        private void Drop(IDialoguePresenter presenter, Exception ex)
        {
            if (!_presenters.Remove(presenter)) return;
            _options.Log($"[Prompter] El presentador {presenter.GetType().Name} falló y se ha " +
                         $"desregistrado de esta sesión: {ex?.GetBaseException().Message}");
        }
    }
}