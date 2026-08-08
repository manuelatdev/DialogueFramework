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

            // Movible y seleccionable; borrar y copiar llegarán con la edición estructural.
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);

            Input = InstantiatePort(Orientation.Horizontal, Direction.Input,
                                    Port.Capacity.Multi, typeof(bool));
            Input.portName = string.Empty;
            inputContainer.Add(Input);

            AddBadge(TypeLabel(model.Type), TypeColor(model.Type));

            if (model.Line != null)
                AddPreview(model.Line);

            for (var i = 0; i < model.Out.Count; i++)
                AddOutputPort(model.Out[i], i, model.Type);

            if (model.Type == NodeType.Hub && model.Fallthrough == FallthroughMode.End)
                AddBadge("fin si no hay salida", new Color(0.5f, 0.5f, 0.5f));

            SetPosition(new Rect(model.Editor.X, model.Editor.Y, 0f, 0f));
            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>Copia la posición actual de la vista al modelo. true si cambió.</summary>
        public bool SyncPositionToModel()
        {
            var pos = GetPosition().position;

            if (Mathf.Approximately(Model.Editor.X, pos.x) &&
                Mathf.Approximately(Model.Editor.Y, pos.y))
                return false;

            Model.Editor.X = pos.x;
            Model.Editor.Y = pos.y;
            return true;
        }

        public void SetActive(bool active)
        {
            // Idealmente, esto debería ser:
            // if (active) AddToClassList("node-active"); else RemoveFromClassList("node-active");
            // Y definir los bordes/colores en un archivo .uss
            
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

        public void SetDimmed(bool dimmed) => style.opacity = dimmed ? 0.3f : 1f;

        // --- construcción -----------------------------------------------------

        private void AddOutputPort(GraphEdge edge, int index, NodeType nodeType)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output,
                                       Port.Capacity.Single, typeof(bool));
            port.portName = PortLabel(edge, index, nodeType);
            port.userData = edge;
            
            // UX: Tooltip con la info completa del puerto por si el nombre está truncado
            port.tooltip = GeneratePortTooltip(edge, index, nodeType);
            
            outputContainer.Add(port);
            Outputs.Add(port);
        }

        private void AddPreview(GraphLine line)
        {
            var fullText = line.Text ?? line.LineId ?? string.Empty;
            var displayText = fullText;
            
            if (displayText.Length > 70) 
                displayText = displayText.Substring(0, 67) + "…";

            var labelText = string.IsNullOrEmpty(line.Speaker) ? displayText : $"{line.Speaker}: {displayText}";
            var label = new Label(labelText);
            
            // UX: Mostrar el texto completo al pasar el ratón
            if (fullText.Length > 70)
                label.tooltip = string.IsNullOrEmpty(line.Speaker) ? fullText : $"{line.Speaker}: {fullText}";

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

        private static string PortLabel(GraphEdge edge, int index, NodeType nodeType)
        {
            var parts = new List<string>();

            // En line y hub el orden es semántico: se toma la primera que cumple.
            var ordenado = nodeType != NodeType.Choice;
            if (ordenado) parts.Add($"{index + 1}.");

            if (edge.IsOption)
            {
                // Blindado contra null reference si edge.Line no estuviera instanciado
                var text = edge.Line?.Text ?? edge.Line?.LineId ?? "(opción)";
                if (text.Length > 28) text = text.Substring(0, 25) + "…";
                parts.Add(text);
            }

            var incondicional = edge.When == null || edge.When is Condition.Always;

            if (!incondicional)
                parts.Add($"[{ConditionText.Describe(edge.When)}]");
            else if (ordenado)
                parts.Add("[siempre]");

            if (edge.Then.Count > 0)
                parts.Add($"⚡{edge.Then.Count}");

            if (!string.IsNullOrEmpty(edge.Reason))
                parts.Add($"↯{edge.Reason}");

            return parts.Count == 0 ? "→" : string.Join(" ", parts);
        }
        
        // Extrae el texto completo sin truncar para el tooltip
        private static string GeneratePortTooltip(GraphEdge edge, int index, NodeType nodeType)
        {
            var tooltip = "";
            if (edge.IsOption)
                tooltip += edge.Line?.Text ?? edge.Line?.LineId ?? "(opción)";
                
            if (edge.When != null && !(edge.When is Condition.Always))
                tooltip += $"\nCondición: {ConditionText.Describe(edge.When)}";
                
            if (edge.Then.Count > 0)
                tooltip += $"\nEventos (Then): {edge.Then.Count}";
                
            if (!string.IsNullOrEmpty(edge.Reason))
                tooltip += $"\nRazón: {edge.Reason}";

            return tooltip.Trim();
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
    }
}