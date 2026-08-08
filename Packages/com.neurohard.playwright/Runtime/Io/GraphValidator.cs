using System;
using System.Collections.Generic;
using System.Linq;

namespace Neurohard.Playwright
{
    public enum IssueSeverity { Warning, Error }

    public sealed class ValidationIssue
    {
        public IssueSeverity Severity { get; }
        public string NodeId { get; }
        public string Message { get; }

        public ValidationIssue(IssueSeverity severity, string nodeId, string message)
        {
            Severity = severity; NodeId = nodeId; Message = message;
        }

        public override string ToString()
            => $"{(Severity == IssueSeverity.Error ? "ERROR" : "aviso")}" +
               $"{(string.IsNullOrEmpty(NodeId) ? "" : $" [{NodeId}]")}: {Message}";
    }

    public sealed class ValidationReport
    {
        public IReadOnlyList<ValidationIssue> Issues { get; }
        public bool HasErrors => Issues.Any(i => i.Severity == IssueSeverity.Error);
        public bool IsValid => !HasErrors;

        public ValidationReport(IReadOnlyList<ValidationIssue> issues)
            => Issues = issues ?? Array.Empty<ValidationIssue>();

        public void ThrowIfInvalid()
        {
            if (!HasErrors) return;
            throw new InvalidOperationException(
                "El grafo tiene errores:\n" +
                string.Join("\n", Issues.Where(i => i.Severity == IssueSeverity.Error)));
        }

        public override string ToString()
            => Issues.Count == 0 ? "Sin incidencias." : string.Join("\n", Issues);
    }

    public static class GraphValidator
    {
        public static ValidationReport Validate(DialogueGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var issues = new List<ValidationIssue>();

            if (string.IsNullOrEmpty(graph.Start))
                issues.Add(Error(null, "El grafo no declara nodo de inicio."));
            else if (graph.Find(graph.Start) == null)
                issues.Add(Error(null, $"El nodo de inicio '{graph.Start}' no existe."));

            foreach (var node in graph.Nodes)
                ValidateNode(graph, node, issues);

            ReportUnreachable(graph, issues);
            return new ValidationReport(issues);
        }

        private static void ValidateNode(DialogueGraph graph, GraphNode node, List<ValidationIssue> issues)
        {
            switch (node.Type)
            {
                case NodeType.Line:
                    if (node.Line == null)
                        issues.Add(Error(node.Id, "Nodo de tipo line sin contenido."));
                    if (node.Out.Count == 0)
                        issues.Add(Warning(node.Id, "Nodo line sin salidas: la conversación terminará aquí."));
                    if (node.Out.Any(e => e.IsOption))
                        issues.Add(Warning(node.Id, "Un nodo line tiene salidas con 'line'; se ignorarán como opciones."));
                    break;

                case NodeType.Choice:
                    var options = node.Out.Where(e => e.IsOption).ToList();
                    if (options.Count == 0)
                        issues.Add(Error(node.Id, "Nodo choice sin ninguna salida que tenga 'line'."));
                    if (options.All(e => e.HideWhenUnavailable) && options.Count > 0 &&
                        options.All(e => !(e.When is Condition.Always)))
                        issues.Add(Warning(node.Id,
                            "Todas las opciones son condicionales y ocultables: puede quedar sin opciones visibles."));
                    break;

                case NodeType.Hub:
                    if (node.Out.Count > 0 &&
                        node.Fallthrough == FallthroughMode.Error &&
                        !node.Out.Any(e => e.When is Condition.Always))
                        issues.Add(Warning(node.Id,
                            "Hub sin salida incondicional y sin \"fallthrough\": \"end\": " +
                            "fallará si ninguna condición se cumple."));
                    if (node.Line != null)
                        issues.Add(Warning(node.Id, "Un hub tiene 'line'; no se mostrará."));
                    break;
            }

            var seenOptionIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in node.Out)
            {
                if (string.IsNullOrEmpty(edge.To))
                    issues.Add(Warning(node.Id, "Una salida no declara destino: terminará la conversación."));
                else if (graph.Find(edge.To) == null)
                    issues.Add(Error(node.Id, $"Una salida apunta a '{edge.To}', que no existe."));

                if (!string.IsNullOrEmpty(edge.OptionId) && !seenOptionIds.Add(edge.OptionId))
                    issues.Add(Error(node.Id, $"Id de opción duplicado: '{edge.OptionId}'."));

                if (edge.Line != null &&
                    string.IsNullOrEmpty(edge.Line.Text) && string.IsNullOrEmpty(edge.Line.LineId))
                    issues.Add(Error(node.Id, "Una opción no tiene ni 'text' ni 'lineId'."));
            }
        }

        private static void ReportUnreachable(DialogueGraph graph, List<ValidationIssue> issues)
        {
            var start = graph.Find(graph.Start);
            if (start == null) return;

            var reached = new HashSet<string>(StringComparer.Ordinal) { start.Id };
            var pending = new Queue<GraphNode>();
            pending.Enqueue(start);

            while (pending.Count > 0)
            {
                foreach (var edge in pending.Dequeue().Out)
                {
                    if (string.IsNullOrEmpty(edge.To) || !reached.Add(edge.To)) continue;
                    var next = graph.Find(edge.To);
                    if (next != null) pending.Enqueue(next);
                }
            }

            foreach (var node in graph.Nodes)
                if (!reached.Contains(node.Id))
                    issues.Add(Warning(node.Id, "Nodo inalcanzable desde el inicio."));
        }

        private static ValidationIssue Error(string nodeId, string message)
            => new ValidationIssue(IssueSeverity.Error, nodeId, message);

        private static ValidationIssue Warning(string nodeId, string message)
            => new ValidationIssue(IssueSeverity.Warning, nodeId, message);
    }
}