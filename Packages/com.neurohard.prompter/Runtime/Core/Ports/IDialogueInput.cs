using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    public interface IDialogueInput
    {
        /// <summary>Espera a que el jugador pida avanzar.</summary>
        Task WaitForAdvanceAsync(CancellationToken ct);

        /// <summary>Espera una selección. Devuelve el OptionId elegido.</summary>
        Task<string> WaitForSelectionAsync(IReadOnlyList<ResolvedOption> options,
                                           CancellationToken ct);
    }
}