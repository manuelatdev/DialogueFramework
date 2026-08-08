using System;
using System.Collections.Generic;

namespace Neurohard.Prompter
{
    /// <summary>Punto de entrada para crear fuentes sencillas.</summary>
    public static class DialogueSource
    {
        private const string CommandPrefix = ">>";

        /// <summary>
        /// Crea un guion lineal. Formatos aceptados por línea:
        ///   "Alba: Hola."        → línea con hablante
        ///   "Hola."              → línea sin hablante
        ///   ">> dar_objeto espada" → comando
        /// Las líneas vacías se descartan.
        /// </summary>
        public static IDialogueSource FromLines(params string[] lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var steps = new List<DialogueStep>(lines.Length);

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var text = raw.Trim();

                if (text.StartsWith(CommandPrefix, StringComparison.Ordinal))
                {
                    steps.Add(ParseCommand(text.Substring(CommandPrefix.Length).Trim()));
                    continue;
                }

                steps.Add(new DialogueStep.Line(ParseLine(text)));
            }

            return new LinearScript(steps);
        }

        /// <summary>Crea una fuente a partir de pasos ya construidos.</summary>
        public static IDialogueSource FromSteps(params DialogueStep[] steps)
            => new LinearScript(steps ?? throw new ArgumentNullException(nameof(steps)));

        private static DialogueLine ParseLine(string text)
        {
            var colon = text.IndexOf(':');

            // Sin ':' o con ':' pegado al final → todo es texto.
            if (colon <= 0 || colon == text.Length - 1)
                return new DialogueLine(text);

            var speaker = text.Substring(0, colon).Trim();
            var body = text.Substring(colon + 1).Trim();

            // Un hablante con espacios casi siempre es un ':' dentro de la frase.
            if (speaker.Length == 0 || speaker.IndexOf(' ') >= 0 || body.Length == 0)
                return new DialogueLine(text);

            return new DialogueLine(body, speaker);
        }

        private static DialogueStep.Command ParseCommand(string text)
        {
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw new ArgumentException("Comando vacío tras '>>'.");

            var args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            return new DialogueStep.Command(parts[0], args);
        }
    }
}