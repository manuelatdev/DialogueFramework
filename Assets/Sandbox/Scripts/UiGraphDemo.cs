using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Neurohard.Prompter;
using Neurohard.Prompter.Unity;
using Neurohard.Playwright.Unity;

public sealed class UiGraphDemo : MonoBehaviour
{
    [SerializeField] private DialogueGraphAsset graphAsset;
    [SerializeField] private UiToolkitPresenter presenter;
    [SerializeField] private int oroInicial = 0;

    private readonly InMemoryVariableStorage _vars = new InMemoryVariableStorage();
    private PrompterBehaviour _prompter;

    private void Start()
    {
        _vars.Set("oro", oroInicial);          // el resto se crean solas al usarse

        _prompter = gameObject.AddComponent<PrompterBehaviour>();
        _prompter.AddPresenter(presenter);
        _prompter.Input = presenter.Input;
        _prompter.Variables = _vars;
        _prompter.Commands = new LogDispatcher();

        Hablar();
    }

    private async void Hablar()
    {
        var result = await _prompter.Play(graphAsset.CreateSource());
        Debug.Log($"Terminada: {result.Outcome}");
        DumpVars();
    }

    private void Update()
    {
        // Barra espaciadora para volver a hablar con Sarn.
        if (!_prompter.IsPlaying && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            Hablar();
    }

    private void DumpVars()
    {
        foreach (var name in new[] { "veces_hablado", "confianza", "mentiste", "oro", "sarn_hostil", "cruzaste" })
            Debug.Log(_vars.TryGet<object>(name, out var v) ? $"{name} = {v}" : $"{name} = (sin definir)");
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
