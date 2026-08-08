using System;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    public sealed class LoggingCommandDispatcher : ICommandDispatcher
    {
        private readonly Action<string> _log;

        public LoggingCommandDispatcher(Action<string> log = null)
            => _log = log ?? (_ => { });

        public bool CanHandle(string commandName) => true;

        public Task DispatchAsync(DialogueStep.Command command, CancellationToken ct)
        {
            _log($"[Prompter] Comando '{command.Name}' recibido pero sin manejador registrado. " +
                 "Registra un ICommandDispatcher en PrompterOptions.");
            return Task.CompletedTask;
        }
    }
}