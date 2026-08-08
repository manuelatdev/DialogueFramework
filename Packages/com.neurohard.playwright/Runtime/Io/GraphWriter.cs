using System;
using System.Collections.Generic;
using System.Text;

namespace Neurohard.Playwright.Io
{
    /// <summary>
    /// Serializa un grafo al formato v1. El round-trip debe ser estable:
    /// leer y volver a escribir produce un documento equivalente.
    /// Los valores por defecto se omiten para mantener los diffs limpios.
    /// </summary>
    public static class GraphWriter
    {
        public static string ToJson(DialogueGraph graph, bool indent = true)
        {
            var root = new JsonObj()
                .Set("version", graph.Version)
                .Set("start", graph.Start)
                .Set("nodes", Arr(graph.Nodes, Node));

            var sb = new StringBuilder();
            root.Render(sb, 0, indent);
            return sb.ToString();
        }

        private static JsonOut Node(GraphNode node)
            => new JsonObj()
                .Set("id", node.Id)
                .Set("title", node.Title)
                .Set("type", TypeText(node.Type))
                .Set("line", node.Line != null ? Line(node.Line) : null)
                .SetIf(node.Fallthrough == FallthroughMode.End, "fallthrough", JsonOut.Str("end"))
                .Set("editor", new JsonObj()
                    .Set("x", node.Editor.X)
                    .Set("y", node.Editor.Y))
                .Set("out", Arr(node.Out, Edge));

        private static JsonOut Line(GraphLine line)
        {
            var obj = new JsonObj()
                .Set("text", line.Text)
                .Set("lineId", line.LineId)
                .Set("speaker", line.Speaker);

            if (line.Tags.Count > 0)
                obj.Set("tags", Arr(line.Tags, JsonOut.Str));

            return obj;
        }

        private static JsonOut Edge(GraphEdge edge)
        {
            var obj = new JsonObj()
                .Set("to", edge.To)
                .Set("id", edge.OptionId)
                .Set("line", edge.Line != null ? Line(edge.Line) : null)
                .Set("reason", edge.Reason)
                .SetIf(edge.HideWhenUnavailable, "hideWhenUnavailable", JsonOut.Bool(true));

            if (edge.When != null && !(edge.When is Condition.Always))
                obj.Set("when", WriteCondition(edge.When));

            if (edge.Then.Count > 0)
                obj.Set("then", Arr(edge.Then, WriteEffect));

            return obj;
        }

        private static JsonOut WriteCondition(Condition condition)
        {
            switch (condition)
            {
                case Condition.All all:
                    return new JsonObj().Set("all", Arr(all.Items, WriteCondition));

                case Condition.Any any:
                    return new JsonObj().Set("any", Arr(any.Items, WriteCondition));

                case Condition.Not not:
                    return new JsonObj().Set("not", WriteCondition(not.Inner));

                case Condition.Compare cmp:
                    var obj = new JsonObj()
                        .Set("var", cmp.Variable)
                        .Set("op", OpText(cmp.Op));

                    if (cmp.Op != ComparisonOp.Exists && cmp.Op != ComparisonOp.NotExists)
                        obj.Set("value", JsonOut.Loose(cmp.Value));

                    return obj;

                default:
                    return new JsonObj();
            }
        }

        private static JsonOut WriteEffect(Effect effect)
        {
            switch (effect)
            {
                case Effect.Command command:
                    var cmd = new JsonObj().Set("command", command.Name);
                    if (command.Arguments.Count > 0)
                        cmd.Set("args", Arr(command.Arguments, JsonOut.Str));
                    return cmd;

                case Effect.Assign assign:
                    return new JsonObj()
                        .Set("var", assign.Variable)
                        .Set("op", AssignText(assign.Op))
                        .Set("value", JsonOut.Loose(assign.Value));

                default:
                    return new JsonObj();
            }
        }

        // --- utilidades -------------------------------------------------------

        private static JsonArr Arr<T>(IEnumerable<T> items, Func<T, JsonOut> map)
        {
            var arr = new JsonArr();
            foreach (var item in items) arr.Add(map(item));
            return arr;
        }

        private static string TypeText(NodeType type) => type switch
        {
            NodeType.Line => "line",
            NodeType.Choice => "choice",
            _ => "hub"
        };

        private static string AssignText(AssignOp op) => op switch
        {
            AssignOp.Add => "+=",
            AssignOp.Subtract => "-=",
            _ => "="
        };

        private static string OpText(ComparisonOp op) => op switch
        {
            ComparisonOp.Equal => "==",
            ComparisonOp.NotEqual => "!=",
            ComparisonOp.Greater => ">",
            ComparisonOp.GreaterOrEqual => ">=",
            ComparisonOp.Less => "<",
            ComparisonOp.LessOrEqual => "<=",
            ComparisonOp.Exists => "exists",
            _ => "!exists"
        };
    }
}