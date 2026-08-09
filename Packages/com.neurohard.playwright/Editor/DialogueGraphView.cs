using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using Neurohard.Playwright.Io;

namespace Neurohard.Playwright.Editor
{
    internal sealed class DialogueGraphView : GraphView
    {
        /// <summary>El grafo que la vista está editando. Necesario para aplicar cambios.</summary>
        private DialogueGraph _graph;
        private readonly Dictionary<string, DialogueNodeView> _nodes =
            new Dictionary<string, DialogueNodeView>();

        private readonly List<(Edge view, GraphEdge model, string ownerId)> _edges =
            new List<(Edge, GraphEdge, string)>();

        /// <summary>
        /// Aristas borradas del modelo en el ciclo actual. Una reconexión llega como
        /// dos invocaciones separadas: primero el borrado, después la creación.
        /// </summary>
        private readonly Dictionary<GraphEdge, (string ownerId, int index)> _recentlyRemoved =
            new Dictionary<GraphEdge, (string, int)>();

        private static readonly Color PathColor = new Color(0.4f, 0.9f, 0.5f);

        private string _activeNodeId;
        private bool _needsFraming;
        private bool _reloading;

/// <summary>El borrado o la reconexión cambió la forma del grafo.</summary>
        public event System.Action StructureChanged;

        /// <summary>La vista pide una transacción antes de modificar el modelo.</summary>
        public System.Func<GraphTransaction> BeginTransaction { get; set; }
        

        public DialogueGraphView()
        {
            style.flexGrow = 1;
            this.focusable = true;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // UX: Anclar el grid al tamaño del padre para evitar que desaparezca
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;

            // Escuchar cambios de geometría para hacer el FrameAll de forma segura
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(_ =>
                UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged));

            RegisterCallback<KeyDownEvent>(evt =>
            {
                // No interferir mientras se escribe en un campo de texto.
                if (evt.target is VisualElement ve && ve.GetFirstAncestorOfType<TextField>() != null) return;

                switch (evt.keyCode)
                {
                    case KeyCode.Backspace:
                    case KeyCode.Delete:
                        if (evt.commandKey || evt.ctrlKey || evt.altKey) return;
                        DeleteSelection();
                        evt.StopPropagation();
                        break;

                    case KeyCode.F:
                        if (selection.Count > 0) FrameSelection(); else FrameAll();
                        evt.StopPropagation();
                        break;
                }
            });
        }

        /// <summary>
        /// Cualquier entrada de otro nodo. Los bucles de un nodo consigo mismo se
        /// excluyen: en un diálogo casi siempre son un error de autoría.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
            => ports.ToList()
                    .Where(p => p.direction != startPort.direction && p.node != startPort.node)
                    .ToList();

        public void Load(DialogueGraph graph, bool encuadrar = true)
        {
            _graph = graph;

            _reloading = true;
            DeleteElements(graphElements.ToList());
            _reloading = false;

            _nodes.Clear();
            _edges.Clear();
            _recentlyRemoved.Clear();
            _activeNodeId = null;

            if (graph == null) return;

foreach (var node in graph.Nodes)
{
    var view = new DialogueNodeView(node) { BeginTransaction = BeginTransaction };
    _nodes[node.Id] = view;
    AddElement(view);
}

            foreach (var node in graph.Nodes)
            {
                if (!_nodes.TryGetValue(node.Id, out var from)) continue;

                for (var i = 0; i < node.Out.Count && i < from.Outputs.Count; i++)
                {
                    var model = node.Out[i];
                    if (string.IsNullOrEmpty(model.To)) continue;
                    if (!_nodes.TryGetValue(model.To, out var to)) continue;

                    var edge = from.Outputs[i].ConnectTo(to.Input);
                    edge.capabilities &= ~Capabilities.Movable;
                    edge.userData = model;
                    AddElement(edge);
                    _edges.Add((edge, model, node.Id));
                }
            }

            if (!string.IsNullOrEmpty(graph.Start) && _nodes.TryGetValue(graph.Start, out var start))
                start.title = "▶ " + start.title;

            // Marcar que necesitamos reencuadrar cuando UI Toolkit termine de calcular tamaños
            _needsFraming = encuadrar;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (_needsFraming && layout.width > 0 && layout.height > 0)
            {
                _needsFraming = false;
                schedule.Execute(() => FrameAll());
            }
        }

        /// <summary>Resalta el nodo por el que va el cursor en Play Mode.</summary>
        public void SetActiveNode(string nodeId)
        {
            if (_activeNodeId == nodeId) return;

            if (_activeNodeId != null && _nodes.TryGetValue(_activeNodeId, out var previous))
                previous.SetActive(false);

            _activeNodeId = nodeId;

            if (nodeId != null && _nodes.TryGetValue(nodeId, out var current))
                current.SetActive(true);
        }

        /// <summary>Tiñe aristas y nodos según el resultado de la simulación.</summary>
        public void ApplySimulation(SimulationResult result)
        {
            if (result == null)
            {
                foreach (var (view, _, _) in _edges) ResetEdge(view);
                foreach (var node in _nodes.Values) node.SetDimmed(false);
                return;
            }

            var path = new HashSet<string>(result.DeterministicPath);

            foreach (var (view, model, ownerId) in _edges)
            {
                var state = result.StateOf(model);
                var inPath = path.Contains(ownerId) && state == EdgeState.Passable;

                switch (state)
                {
                    case EdgeState.Passable:
                        Tint(view, inPath ? PathColor : Color.white, inPath ? 3f : 1f, 1f);
                        break;
                    case EdgeState.Blocked:
                        Tint(view, new Color(0.9f, 0.4f, 0.4f), 1f, 0.35f);
                        break;
                    case EdgeState.Hidden:
                        Tint(view, Color.gray, 1f, 0.15f);
                        break;
                    case EdgeState.Broken:
                        Tint(view, Color.red, 2f, 1f);
                        break;
                }
            }

            foreach (var kv in _nodes)
                kv.Value.SetDimmed(!result.Reachable.Contains(kv.Key));
        }

        private static void Tint(Edge edge, Color color, float width, float opacity)
        {
            edge.edgeControl.inputColor = color;
            edge.edgeControl.outputColor = color;
            edge.edgeControl.edgeWidth = Mathf.RoundToInt(width);
            edge.style.opacity = opacity;

            // Forzar actualización de la geometría de la curva para que el grosor aplique bien
            edge.UpdateEdgeControl();
            edge.MarkDirtyRepaint();
        }

        private static void ResetEdge(Edge edge)
        {
            edge.edgeControl.inputColor = Color.white;
            edge.edgeControl.outputColor = Color.white;
            edge.edgeControl.edgeWidth = 1;
            edge.style.opacity = 1f;

            edge.UpdateEdgeControl();
            edge.MarkDirtyRepaint();
        }

        public void FocusNode(string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node)) return;

            ClearSelection();
            AddToSelection(node);
            FrameSelection();
        }

private GraphViewChange OnGraphViewChanged(GraphViewChange change)
{
    if (_graph == null || _reloading) return change;

    var hayMovidos = change.movedElements != null && change.movedElements.Count > 0;
    var hayBorrados = change.elementsToRemove != null && change.elementsToRemove.Count > 0;
    var hayConexiones = change.edgesToCreate != null && change.edgesToCreate.Count > 0;

    if (!hayMovidos && !hayBorrados && !hayConexiones) return change;

    // La transacción compara el JSON al cerrarse: si nada cambió, se descarta sola.
    using (BeginTransaction?.Invoke())
    {
        // Primero las conexiones: una reconexión llega como borrado + creación,
        // y si borramos antes, la arista del modelo desaparece y no hay qué redirigir.
        if (hayConexiones) ApplyConnections(change.edgesToCreate);
        if (hayBorrados) ApplyRemovals(change.elementsToRemove);

        if (hayMovidos)
            foreach (var element in change.movedElements)
                if (element is DialogueNodeView node) node.SyncPositionToModel();
    }

    if (hayBorrados || hayConexiones)
        schedule.Execute(() => StructureChanged?.Invoke()).ExecuteLater(1);

    return change;
}

private void ApplyConnections(List<Edge> edges)
{
    foreach (var edge in edges)
    {
        if (!(edge.output?.userData is GraphEdge model)) continue;
        if (!(edge.input?.node is DialogueNodeView destino)) continue;

        if (_recentlyRemoved.TryGetValue(model, out var location))
        {
            var owner = _graph.Find(location.ownerId);
            if (owner != null && !owner.Out.Contains(model))
                owner.Out.Insert(Mathf.Min(location.index, owner.Out.Count), model);

            _recentlyRemoved.Remove(model);
        }

        model.To = destino.Model.Id;
    }
}

private void ApplyRemovals(List<GraphElement> elements)
{
    foreach (var element in elements)
    {
        switch (element)
        {
            case DialogueNodeView node:
                if (_graph.Remove(node.Model.Id)) _nodes.Remove(node.Model.Id);
                break;

            case Edge edge when edge.userData is GraphEdge model:
                var location = LocateEdge(model);
                if (RemoveEdgeFromModel(model) && location.ownerId != null)
                    _recentlyRemoved[model] = location;
                _edges.RemoveAll(e => e.model == model);
                break;
        }
    }
}

        private bool RemoveEdgeFromModel(GraphEdge model)
        {
            foreach (var node in _graph.Nodes)
                if (node.Out.Remove(model)) return true;
            return false;
        }

        private (string ownerId, int index) LocateEdge(GraphEdge model)
        {
            foreach (var node in _graph.Nodes)
            {
                var i = node.Out.IndexOf(model);
                if (i >= 0) return (node.Id, i);
            }
            return (null, -1);
        }
    }
}