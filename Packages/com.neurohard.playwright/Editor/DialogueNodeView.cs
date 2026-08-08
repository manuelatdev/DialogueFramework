using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neurohard.Playwright.Editor
{
    internal sealed class DialogueNodeView : Node
    {
        private static readonly Color Highlight = new Color(1f, 0.72f, 0.2f);

        public GraphNode Model { get; }
        public Port Input { get; }
        public List<Port> Outputs { get; } = new List<Port>();

        public DialogueNodeView(GraphNode model)
        {
            Model = model;
            viewDataKey = model.Id;
            title = string.IsNullOrEmpty(model.Title) ? model.Id : model.Title;

            // Solo lectura: ni mover, ni borrar, ni copiar.
            capabilities &= ~(Capabilities.Deletable | Capabilities.Movable | Capabilities.Copiable);

            Input = InstantiatePort(Orientation.Horizontal, Direction.Input,
                                    Port.Capacity.Multi, typeof(bool));
            Input.portName = string.Empty;
            inputContainer.Add(Input);

            AddBadge(TypeLabel(model.Type), TypeColor(model.Type));

            if (model.Line != null)
                AddPreview(model.Line);

            foreach (var edge in model.Out)
                AddOutputPort(edge);

            if (model.Type == NodeType.Hub && model.Fallthrough == FallthroughMode.End)
                AddBadge("fin si no hay salida", new Color(0.5f, 0.5f, 0.5f));

            SetPosition(new Rect(model.Editor.X, model.Editor.Y, 0f, 0f));
            RefreshExpandedState();
            RefreshPorts();
        }

        public void SetActive(bool active)
        {
            if (active)
            {
                style.borderTopWidth = 2;
                style.borderBottomWidth = 2;
                style.borderLeftWidth = 2;
                style.borderRightWidth = 2;
                style.borderTopColor = Highlight;
                style.borderBottomColor = Highlight;
                style.borderLeftColor = Highlight;
                style.borderRightColor = Highlight;
                mainContainer.style.backgroundColor = new Color(1f, 0.72f, 0.2f, 0.25f);
            }
            else
            {
                style.borderTopWidth = 0;
                style.borderBottomWidth = 0;
                style.borderLeftWidth = 0;
                style.borderRightWidth = 0;
                mainContainer.style.backgroundColor = StyleKeyword.Null;
            }
        }

        private void AddOutputPort(GraphEdge edge)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output,
                                       Port.Capacity.Single, typeof(bool));
            port.portName = PortLabel(edge);
            port.userData = edge;
            outputContainer.Add(port);
            Outputs.Add(port);
        }

        private void AddPreview(GraphLine line)
        {
            var text = line.Text ?? line.LineId ?? string.Empty;
            if (text.Length > 70) text = text.Substring(0, 67) + "…";

            var label = new Label(string.IsNullOrEmpty(line.Speaker) ? text : $"{line.Speaker}: {text}");
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.maxWidth = 220;
            label.style.marginLeft = 6;
            label.style.marginRight = 6;
            label.style.marginTop = 4;
            label.style.marginBottom = 4;
            label.style.opacity = 0.85f;
            extensionContainer.Add(label);
        }

        private void AddBadge(string text, Color color)
        {
            var badge = new Label(text);
            badge.style.fontSize = 10;
            badge.style.color = color;
            badge.style.marginLeft = 6;
            badge.style.marginTop = 2;
            titleContainer.Add(badge);
        }

        private static string PortLabel(GraphEdge edge)
        {
            var parts = new List<string>();

            if (edge.IsOption)
            {
                var text = edge.Line.Text ?? edge.Line.LineId ?? "(opción)";
                if (text.Length > 28) text = text.Substring(0, 25) + "…";
                parts.Add(text);
            }

            if (edge.When != null && !(edge.When is Condition.Always))
                parts.Add($"[{ConditionText.Describe(edge.When)}]");

            if (edge.Then.Count > 0)
                parts.Add($"⚡{edge.Then.Count}");

            return parts.Count == 0 ? "→" : string.Join(" ", parts);
        }

        private static string TypeLabel(NodeType type) => type switch
        {
            NodeType.Line => "línea",
            NodeType.Choice => "opciones",
            _ => "hub"
        };

        private static Color TypeColor(NodeType type) => type switch
        {
            NodeType.Line => new Color(0.45f, 0.75f, 0.95f),
            NodeType.Choice => new Color(0.95f, 0.75f, 0.35f),
            _ => new Color(0.65f, 0.65f, 0.65f)
        };

        public void SetDimmed(bool dimmed) => style.opacity = dimmed ? 0.3f : 1f;
    }
}