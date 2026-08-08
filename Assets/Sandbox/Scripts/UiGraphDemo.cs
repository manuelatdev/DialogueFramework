using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Neurohard.Prompter;
using Neurohard.Prompter.Unity;
using Neurohard.Playwright;
using Neurohard.Playwright.Unity;

public sealed class UiGraphDemo : MonoBehaviour
{
    [SerializeField] private DialogueGraphAsset graphAsset;
    [SerializeField] private UiToolkitPresenter presenter;
    [SerializeField] private int oroInicial = 50;
    [SerializeField] private int reputacionInicial = 0;

    private async void Start()
    {
        if (graphAsset == null || presenter == null)
        {
            Debug.LogError("Faltan referencias en el inspector.");
            return;
        }

        var vars = new InMemoryVariableStorage();
        vars.Set("oro", oroInicial);
        vars.Set("reputacion", reputacionInicial);

        var prompter = gameObject.AddComponent<PrompterBehaviour>();
        prompter.AddPresenter(presenter);
        prompter.Input = presenter.Input;
        prompter.Variables = vars;
        prompter.Commands = new LogDispatcher();

        var result = await prompter.Play(graphAsset.CreateSource());
        Debug.Log($"Conversación terminada: {result.Outcome}");
    }

    private sealed class LogDispatcher : ICommandDispatcher
    {
        public bool CanHandle(string name) => true;
        public Task DispatchAsync(DialogueStep.Command c, CancellationToken ct)
        {
            Debug.Log($"[Comando] {c.Name}({string.Join(", ", c.Arguments)})");
            return Task.CompletedTask;
        }
    }
}