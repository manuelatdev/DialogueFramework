using System;
using System.Collections.Generic;

namespace Neurohard.Playwright
{
    public sealed class QueryUsage
    {
        public string Name { get; }

        /// <summary>Combinaciones de argumentos con las que se invoca.</summary>
        public HashSet<string> ArgumentSets { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> UsedIn { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>true si en algún sitio se compara contra un valor, no solo por verdadero.</summary>
        public bool NeedsValue { get; internal set; }

        public QueryUsage(string name) => Name = name;
    }

    /// <summary>
    /// Recorre el grafo y lista las consultas al juego que exige.
    /// Es, de hecho, el contrato que el IQueryResolver debe implementar.
    /// </summary>
    public static class QueryInventory
    {
        public static IReadOnlyDictionary<string, QueryUsage> Collect(DialogueGraph graph)
        {
            var result = new Dictionary<string, QueryUsage>(StringComparer.Ordinal);
            if (graph == null) return result;

            foreach (var node in graph.Nodes)
                foreach (var edge in node.Out)
                    if (edge.When != null)
                        Walk(edge.When, node.Id, result);

            return result;
        }

        /// <summary>Clave única de una invocación concreta: nombre + argumentos.</summary>
        public static string KeyOf(string name, IReadOnlyList<string> args)
            => args == null || args.Count == 0 ? name : $"{name}({string.Join(",", args)})";

        private static void Walk(Condition condition, string nodeId, Dictionary<string, QueryUsage> result)
        {
            switch (condition)
            {
                case Condition.Query q:
                    if (!result.TryGetValue(q.Name, out var usage))
                        result[q.Name] = usage = new QueryUsage(q.Name);

                    usage.ArgumentSets.Add(KeyOf(q.Name, q.Arguments));
                    usage.UsedIn.Add(nodeId);
                    if (q.Value != null) usage.NeedsValue = true;
                    break;

                case Condition.Not not:
                    Walk(not.Inner, nodeId, result);
                    break;

                case Condition.All all:
                    foreach (var c in all.Items) Walk(c, nodeId, result);
                    break;

                case Condition.Any any:
                    foreach (var c in any.Items) Walk(c, nodeId, result);
                    break;
            }
        }
    }
}