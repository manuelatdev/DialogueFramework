using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neurohard.Playwright.Editor
{
    internal sealed class DialogueGraphView : GraphView
    {
        private readonly Dictionary<string, DialogueNodeView> _nodes =
            new Dictionary<string, DialogueNodeView>();

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
    }
}