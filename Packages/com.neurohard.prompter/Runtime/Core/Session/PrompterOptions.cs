using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    /// <summary>Cableado de una instancia de Prompter. Solo Presenters es obligatorio.</summary>
    public sealed class PrompterOptions
    {
        public IList<IDialoguePresenter> Presenters { get; } = new List<IDialoguePresenter>();

        public IDialogueInput Input { get; set; }
        public ILineProvider LineProvider { get; set; }
        public IVariableStorage Variables { get; set; }
        public ICommandDispatcher Commands { get; set; }

        /// <summary>Destino de avisos y errores. Core no conoce UnityEngine.</summary>
        public Action<string> Log { get; set; }

        internal PrompterOptions Materialize()
        {
            if (Presenters.Count == 0)
                throw new InvalidOperationException(
                    "Prompter no tiene presentadores. Añade al menos uno en PrompterOptions.Presenters " +
                    "(implementa IDialoguePresenter) antes de llamar a Play.");

            Log ??= _ => { };
            Input ??= new ImmediateInput();
            LineProvider ??= new PassthroughLineProvider();
            Variables ??= new InMemoryVariableStorage();
            Commands ??= new LoggingCommandDispatcher(Log);
            return this;
        }
    }
}