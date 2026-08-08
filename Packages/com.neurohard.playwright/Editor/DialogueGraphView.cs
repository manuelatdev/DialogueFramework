using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace Neurohard.Playwright.Editor
{
    internal sealed class DialogueGraphView : GraphView
    {
        private readonly Dictionary<string, DialogueNodeView> _nodes =
            new Dictionary<string, DialogueNodeView>();

        private readonly List<(Edge view, string nodeId, int index)> _edges =
    new List<(Edge, string, int)>();

        private static readonly Color PathColor = new Color(0.4f, 0.9f, 0.5f);

        private string _activeNodeId;

        public DialogueGraphView()
        {
            style.flexGrow = 1;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());      // pan
            this.AddManipulator(new RectangleSelector());   // selección
            // Sin SelectionDragger: los nodos no se mueven (el visor es de solo lectura).

            Insert(0, new GridBackground());
        }

        /// <summary>Sin conexiones posibles: el visor no edita.</summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
            => new List<Port>();

        public void Load(DialogueGraph graph)
        {
            DeleteElements(graphElements.ToList());
            _nodes.Clear();
            _edges.Clear();
            _activeNodeId = null;

            if (graph == null) return;

            foreach (var node in graph.Nodes)
            {
                var view = new DialogueNodeView(node);
                _nodes[node.Id] = view;
                AddElement(view);
            }

            foreach (var node in graph.Nodes)
            {
                if (!_nodes.TryGetValue(node.Id, out var from)) continue;

                for (var i = 0; i < node.Out.Count && i < from.Outputs.Count; i++)
                {
                    var target = node.Out[i].To;
                    if (string.IsNullOrEmpty(target)) continue;
                    if (!_nodes.TryGetValue(target, out var to)) continue;

                    var edge = from.Outputs[i].ConnectTo(to.Input);
                    edge.capabilities &= ~(Capabilities.Deletable | Capabilities.Movable);
                    AddElement(edge);
                    _edges.Add((edge, node.Id, i));
                }
            }

            if (!string.IsNullOrEmpty(graph.Start) && _nodes.TryGetValue(graph.Start, out var start))
                start.title = "▶ " + start.title;

            schedule.Execute(() => FrameAll()).ExecuteLater(50);
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

            foreach (var (view, nodeId, index) in _edges)
            {
                var state = result.StateOf(nodeId, index);
                var inPath = path.Contains(nodeId) && state == EdgeState.Passable;

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
            edge.MarkDirtyRepaint();
        }

        private static void ResetEdge(Edge edge)
        {
            edge.edgeControl.inputColor = Color.white;
            edge.edgeControl.outputColor = Color.white;
            edge.edgeControl.edgeWidth = 1;
            edge.style.opacity = 1f;
            edge.MarkDirtyRepaint();
        }

        public void FocusNode(string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var node)) return;

            ClearSelection();
            AddToSelection(node);
            FrameSelection();
        }
    }
}