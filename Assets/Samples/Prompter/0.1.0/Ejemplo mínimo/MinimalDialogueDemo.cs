using System.Threading;
using UnityEngine;

namespace Neurohard.Prompter.Samples
{
    /// <summary>Arrastra este componente a un GameObject vacío y dale a Play.</summary>
    public sealed class MinimalDialogueDemo : MonoBehaviour
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private async void Start()
        {
            var prompter = new DialoguePlayer(new PrompterOptions
            {
                Presenters = { new ConsolePresenter() },
                Input = gameObject.AddComponent<ImguiDialogueInput>(),
                Log = Debug.LogWarning
            });

            var result = await prompter.Play(DialogueSource.FromLines(
                "Alba: ¿Y esto qué es?",
                "Nilo: Ni idea. Pero brilla.",
                ">> registrar hallazgo",
                "Alba: Pues no lo toques."
            ), _cts.Token);

            Debug.Log($"Resultado: {result.Outcome}");
        }

        private void OnDestroy() => _cts.Cancel();
    }
}