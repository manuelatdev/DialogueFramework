using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    public interface ICommandDispatcher
    {
        bool CanHandle(string commandName);

        /// <summary>El runner espera: un comando puede mover la cámara antes de seguir.</summary>
        Task DispatchAsync(DialogueStep.Command command, CancellationToken ct);
    }
}