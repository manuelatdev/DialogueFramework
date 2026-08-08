using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    /// <summary>
    /// Cursor de solo lectura sobre un DialogueGraph. Traduce el recorrido del
    /// grafo a los pasos del contrato de Prompter.
    /// </summary>
    public sealed class GraphDialogueSource : IDialogueSource, ISerializableSource
    {
        private const int MaxTransitionsPerStep = 100;

        private readonly DialogueGraph _graph;
        private readonly Queue<Effect> _pendingEffects = new Queue<Effect>();

        private IVariableStorage _vars;
        private EvaluationContext _ctx;

        private GraphNode _current;
        private Dictionary<string, GraphEdge> _awaiting;
        private bool _needsTraversal;
        private bool _finished;

        public GraphDialogueSource(DialogueGraph graph)
            => _graph = graph ?? throw new ArgumentNullException(nameof(graph));

        /// <summary>Nodo en el que está el cursor. Útil para depuración.</summary>
        public string CurrentNodeId => _current?.Id;

        public ValueTask StartAsync(DialogueContext context, CancellationToken ct)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _vars = context.Variables;
            _ctx = new EvaluationContext(context.Variables, context.Queries);
            _pendingEffects.Clear();
            _awaiting = null;
            _needsTraversal = false;
            _finished = false;

            var startId = _graph.Start;
            if (context.Parameters != null &&
                context.Parameters.TryGetValue("start", out var requested) &&
                !string.IsNullOrEmpty(requested))
                startId = requested;

            _current = _graph.Find(startId);
            if (_current == null)
                throw new InvalidOperationException(
                    $"El grafo no contiene el nodo de inicio '{startId}'.");

            return default;
        }

        public ValueTask<DialogueStep> AdvanceAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<DialogueStep>(NextStep());
        }

        public ValueTask SelectOptionAsync(string optionId, CancellationToken ct)
        {
            if (_awaiting == null)
                throw new InvalidOperationException(
                    "SelectOptionAsync sin opciones pendientes: el último paso no fue un Options.");

            if (!_awaiting.TryGetValue(optionId, out var edge))
                throw new InvalidOperationException(
                    $"OptionId desconocido '{optionId}' en el nodo '{_current?.Id}'.");

            _awaiting = null;
            Enter(edge);
            return default;
        }

        // --- recorrido --------------------------------------------------------

        private DialogueStep NextStep()
        {
            if (_awaiting != null)
                throw new InvalidOperationException(
                    $"El nodo '{_current?.Id}' está esperando una selección. " +
                    "Llama a SelectOptionAsync antes de AdvanceAsync.");

            for (var i = 0; i < MaxTransitionsPerStep; i++)
            {
                var command = DrainEffects();
                if (command != null) return command;

                if (_finished || _current == null)
                    return DialogueStep.Complete.Instance;

                if (_needsTraversal)
                {
                    _needsTraversal = false;
                    if (!TryTraverse(_current)) { _finished = true; return DialogueStep.Complete.Instance; }
                    continue;
                }

                switch (_current.Type)
                {
                    case NodeType.Line:
                        if (_current.Line == null)
                            throw new InvalidOperationException(
                                $"El nodo '{_current.Id}' es de tipo line pero no tiene contenido.");
                        _needsTraversal = true;
                        return new DialogueStep.Line(ToDialogueLine(_current.Line, _current.Id));

                    case NodeType.Choice:
                        return BuildOptions(_current);

                    case NodeType.Hub:
                        if (!TryTraverse(_current)) { _finished = true; return DialogueStep.Complete.Instance; }
                        continue;

                    default:
                        throw new InvalidOperationException(
                            $"Tipo de nodo no contemplado en '{_current.Id}': {_current.Type}.");
                }
            }

            throw new InvalidOperationException(
                $"Se superaron {MaxTransitionsPerStep} transiciones sin producir un paso. " +
                $"Probable ciclo de nodos hub alrededor de '{_current?.Id}'.");
        }

        /// <summary>Toma la primera arista cuya condición se cumple. false = fin.</summary>
        private bool TryTraverse(GraphNode node)
        {
            if (node.Out.Count == 0) return false;   // terminal declarado sin ambigüedad

            foreach (var edge in node.Out)
            {
                if (edge.When == null || edge.When.Evaluate(_ctx)) { Enter(edge); return true; }
            }

            if (node.Fallthrough == FallthroughMode.End) return false;

            throw new InvalidOperationException(
                $"El nodo '{node.Id}' tiene {node.Out.Count} salidas pero ninguna cumple su condición. " +
                "Añade una salida sin condición o declara \"fallthrough\": \"end\".");
        }

        private void Enter(GraphEdge edge)
        {
            foreach (var effect in edge.Then)
                _pendingEffects.Enqueue(effect);

            if (string.IsNullOrEmpty(edge.To)) { _current = null; _finished = true; return; }

            _current = _graph.Find(edge.To)
                ?? throw new InvalidOperationException(
                    $"Una arista apunta a '{edge.To}', que no existe en el grafo.");
        }

        /// <summary>
        /// Aplica los efectos en orden. Los Assign se ejecutan al vuelo; el primer
        /// Command interrumpe el drenaje y se emite como paso.
        /// </summary>
        private DialogueStep DrainEffects()
        {
            while (_pendingEffects.Count > 0)
            {
                switch (_pendingEffects.Dequeue())
                {
                    case Effect.Assign assign:
                        assign.Apply(_vars);
                        break;
                    case Effect.Command command:
                        return new DialogueStep.Command(command.Name, command.Arguments);
                }
            }
            return null;
        }

        private DialogueStep BuildOptions(GraphNode node)
        {
            var choices = new List<DialogueOption>();
            var map = new Dictionary<string, GraphEdge>(StringComparer.Ordinal);

            for (var i = 0; i < node.Out.Count; i++)
            {
                var edge = node.Out[i];
                if (!edge.IsOption) continue;

                var available = edge.When == null || edge.When.Evaluate(_ctx);
                if (!available && edge.HideWhenUnavailable) continue;

                var id = string.IsNullOrEmpty(edge.OptionId) ? $"{node.Id}#{i}" : edge.OptionId;
                if (map.ContainsKey(id))
                    throw new InvalidOperationException(
                        $"OptionId duplicado '{id}' en el nodo '{node.Id}'.");

                map[id] = edge;
                choices.Add(new DialogueOption(id, ToDialogueLine(edge.Line, node.Id), available,
                                               available ? null : edge.Reason));
            }

            if (choices.Count == 0)
                throw new InvalidOperationException(
                    $"El nodo choice '{node.Id}' no ofrece ninguna opción visible.");

            if (!choices.Any(c => c.IsAvailable))
                throw new InvalidOperationException(
                    $"El nodo choice '{node.Id}' no tiene ninguna opción disponible; " +
                    "la conversación quedaría bloqueada.");

            _awaiting = map;
            return new DialogueStep.Options(
                choices, node.Line != null ? ToDialogueLine(node.Line, node.Id) : null);
        }

        private static DialogueLine ToDialogueLine(GraphLine line, string nodeId)
        {
            var id = line?.ResolveId();
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException(
                    $"Una línea del nodo '{nodeId}' no tiene ni 'text' ni 'lineId'.");

            return new DialogueLine(id, line.Speaker,
                line.Tags != null && line.Tags.Count > 0 ? line.Tags.ToArray() : null);
        }

        // --- persistencia -----------------------------------------------------

        public string CaptureState() => _current?.Id ?? string.Empty;

        public void RestoreState(string state)
        {
            _pendingEffects.Clear();
            _awaiting = null;
            _needsTraversal = false;

            _current = _graph.Find(state);
            _finished = _current == null;
        }
    }
}