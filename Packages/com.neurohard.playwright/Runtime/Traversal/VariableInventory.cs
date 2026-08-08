using System.Collections.Generic;

namespace Neurohard.Playwright
{
    public sealed class VariableUsage
    {
        public string Name { get; }
        public HashSet<string> ReadIn { get; } = new HashSet<string>();
        public HashSet<string> WrittenIn { get; } = new HashSet<string>();

        public VariableUsage(string name) => Name = name;

        public bool IsNeverWritten => WrittenIn.Count == 0;
        public bool IsNeverRead => ReadIn.Count == 0;
    }

    /// <summary>Recorre el grafo y lista las variables mencionadas.</summary>
    public static class VariableInventory
    {
        public static IReadOnlyDictionary<string, VariableUsage> Collect(DialogueGraph graph)
        {
            var result = new Dictionary<string, VariableUsage>();
            if (graph == null) return result;

            foreach (var node in graph.Nodes)
                foreach (var edge in node.Out)
                {
                    if (edge.When != null) CollectFromCondition(edge.When, node.Id, result);

                    foreach (var effect in edge.Then)
                        if (effect is Effect.Assign assign)
                            Get(result, assign.Variable).WrittenIn.Add(node.Id);
                }

            return result;
        }

        private static void CollectFromCondition(
            Condition condition, string nodeId, Dictionary<string, VariableUsage> result)
        {
            switch (condition)
            {
                case Condition.Compare cmp: Get(result, cmp.Variable).ReadIn.Add(nodeId); break;
                case Condition.Not not: CollectFromCondition(not.Inner, nodeId, result); break;
                case Condition.All all:
                    foreach (var c in all.Items) CollectFromCondition(c, nodeId, result);
                    break;
                case Condition.Any any:
                    foreach (var c in any.Items) CollectFromCondition(c, nodeId, result);
                    break;
            }
        }

        private static VariableUsage Get(Dictionary<string, VariableUsage> map, string name)
        {
            if (!map.TryGetValue(name, out var usage))
                map[name] = usage = new VariableUsage(name);
            return usage;
        }
    }
}