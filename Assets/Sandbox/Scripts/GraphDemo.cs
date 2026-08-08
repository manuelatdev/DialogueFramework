using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Neurohard.Prompter;
using Neurohard.Prompter.Unity;
using Neurohard.Prompter.Samples;
using Neurohard.Playwright;
using Neurohard.Playwright.Unity;

public sealed class GraphDemo : MonoBehaviour
{
    [SerializeField] private DialogueGraphAsset graphAsset;
    [SerializeField] private int oroInicial = 50;
    [SerializeField] private int reputacionInicial = 0;

    private readonly List<string> _comandos = new List<string>();
    private InMemoryVariableStorage _vars;
    private GraphDialogueSource _source;
    private PrompterBehaviour _prompter;
    private string _estado = "cargando…";

    private async void Start()
    {
        if (graphAsset == null) { Debug.LogError("Falta asignar el DialogueGraphAsset."); return; }

        var report = GraphValidator.Validate(graphAsset.Graph);
        Debug.Log($"[Validación] {report}");
        if (report.HasErrors) { _estado = "grafo inválido"; return; }

        _vars = new InMemoryVariableStorage();
        _vars.Set("oro", oroInicial);
        _vars.Set("reputacion", reputacionInicial);

        _source = graphAsset.CreateSource();

        _prompter = gameObject.AddComponent<PrompterBehaviour>();
        _prompter.AddPresenter(new ConsolePresenter());
        _prompter.Input = gameObject.AddComponent<ImguiDialogueInput>();
        _prompter.Variables = _vars;
        _prompter.Commands = new LoggingDispatcher(_comandos);

        _estado = "en curso";
        var result = await _prompter.Play(_source);

        _estado = result.Outcome.ToString();
        foreach (var c in result.Choices) Debug.Log($"Elegido: {c.OptionId}");
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(420, 20, 320, 260), GUI.skin.box);
        GUILayout.Label($"Estado: {_estado}");
        GUILayout.Label($"Nodo actual: {_source?.CurrentNodeId ?? "—"}");

        if (_vars != null)
        {
            _vars.TryGet<int>("oro", out var oro);
            _vars.TryGet<int>("reputacion", out var rep);
            GUILayout.Label($"oro: {oro}   reputación: {rep}");
        }

        GUILayout.Label($"Comandos: {(_comandos.Count == 0 ? "—" : string.Join(", ", _comandos))}");
        GUILayout.EndArea();
    }

    private sealed class LoggingDispatcher : ICommandDispatcher
    {
        private readonly List<string> _sink;
        public LoggingDispatcher(List<string> sink) => _sink = sink;
        public bool CanHandle(string name) => true;
        public Task DispatchAsync(DialogueStep.Command c, CancellationToken ct)
        {
            _sink.Add(c.Name);
            Debug.Log($"[Comando] {c.Name}({string.Join(", ", c.Arguments)})");
            return Task.CompletedTask;
        }
    }
}