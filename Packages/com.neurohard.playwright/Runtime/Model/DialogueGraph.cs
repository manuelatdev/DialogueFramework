using System;
using System.Collections.Generic;
using System.Linq;

namespace Neurohard.Playwright
{
    public enum NodeType { Line, Choice, Hub }
    public enum FallthroughMode { Error, End }

    public sealed class EditorMetadata
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    /// <summary>Contenido textual. Text para modo directo, LineId para localizado.</summary>
    public sealed class GraphLine
    {
        public string Text { get; set; }
        public string LineId { get; set; }
        public string Speaker { get; set; }
        public IList<string> Tags { get; } = new List<string>();

        public string ResolveId() => !string.IsNullOrEmpty(LineId) ? LineId : Text;
    }

    /// <summary>Una salida. Con Line, es una opción elegible; sin ella, una transición.</summary>
    public sealed class GraphEdge
    {
        public string To { get; set; }
        public GraphLine Line { get; set; }
        public Condition When { get; set; } = Condition.Always.Instance;
        public IList<Effect> Then { get; } = new List<Effect>();
        public bool HideWhenUnavailable { get; set; }

        public string OptionId { get; set; }
        public bool IsOption => Line != null;
    }

    public sealed class GraphNode
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public NodeType Type { get; set; }
        public GraphLine Line { get; set; }
        public IList<GraphEdge> Out { get; } = new List<GraphEdge>();
        public FallthroughMode Fallthrough { get; set; } = FallthroughMode.Error;
        public EditorMetadata Editor { get; set; } = new EditorMetadata();
    }

    public sealed class DialogueGraph
    {
        private readonly List<GraphNode> _nodes = new List<GraphNode>();
        private readonly Dictionary<string, GraphNode> _index = new Dictionary<string, GraphNode>(StringComparer.Ordinal);

        public int Version { get; set; } = 1;
        public string Start { get; set; }
        public IReadOnlyList<GraphNode> Nodes => _nodes;

        public GraphNode Find(string id)
            => id != null && _index.TryGetValue(id, out var n) ? n : null;

        public GraphNode Add(GraphNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (string.IsNullOrEmpty(node.Id)) throw new ArgumentException("Nodo sin id.");
            if (_index.ContainsKey(node.Id)) throw new ArgumentException($"Id de nodo duplicado: '{node.Id}'.");

            _nodes.Add(node);
            _index[node.Id] = node;
            if (string.IsNullOrEmpty(Start)) Start = node.Id;
            return node;
        }

        public bool Remove(string id)
        {
            var node = Find(id);
            if (node == null) return false;

            _nodes.Remove(node);
            _index.Remove(id);
            foreach (var n in _nodes)
                for (var i = n.Out.Count - 1; i >= 0; i--)
                    if (n.Out[i].To == id) n.Out.RemoveAt(i);

            if (Start == id) Start = _nodes.FirstOrDefault()?.Id;
            return true;
        }

        /// <summary>Renombra un nodo y actualiza todas las aristas que lo apuntan.</summary>
        public void Rename(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(newId))
                throw new ArgumentException("El nuevo Id no puede estar vacío.");

            var node = Find(oldId) ?? throw new ArgumentException($"No existe el nodo '{oldId}'.");
            if (_index.ContainsKey(newId)) throw new ArgumentException($"Ya existe un nodo '{newId}'.");

            _index.Remove(oldId);
            node.Id = newId;
            _index[newId] = node;

            foreach (var n in _nodes)
                foreach (var e in n.Out)
                    if (e.To == oldId) e.To = newId;

            if (Start == oldId) Start = newId;
        }

        public static string NewId(string prefix = "n")
            => $"{prefix}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
    }
}