using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Neurohard.Prompter
{
    /// <summary>
    /// Presenta el diálogo. NO captura input: eso es IDialogueInput.
    /// Las tareas se completan cuando la PRESENTACIÓN termina (animación,
    /// clip de voz), no cuando el jugador confirma.
    /// </summary>
    public interface IDialoguePresenter
    {
        Task ShowLineAsync(ResolvedLine line, CancellationToken ct);

        Task ShowOptionsAsync(IReadOnlyList<ResolvedOption> options,
                              ResolvedLine prompt,
                              CancellationToken ct);

        /// <summary>
        /// Termina de golpe la presentación en curso (texto completo al instante).
        /// Debe ser idempotente y no lanzar si no hay nada en curso.
        /// </summary>
        void SkipCurrentPresentation();

        Task ClearAsync(CancellationToken ct);
    }
}