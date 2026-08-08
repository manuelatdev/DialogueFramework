using System.Collections.Generic;
using Neurohard.Prompter;

namespace Neurohard.Playwright
{
    public enum EdgeState
    {
        /// <summary>La condición se cumple con el estado actual de variables.</summary>
        Passable,
        /// <summary>La condición no se cumple.</summary>
        Blocked,
        /// <summary>No se cumple y además está marcada para ocultarse.</summary>
        Hidden,
        /// <summary>Apunta a un nodo inexistente o sin destino.</summary>
        Broken
    }

    public sealed class SimulationResult
    {
        /// <summary>Estado de cada arista, indexado por nodo y posición en Out.</summary>
        public Dictionary<(string nodeId, int index), EdgeState> Edges { get; }
            = new Dictionary<(string, int), EdgeState>();

        /// <summary>Nodos alcanzables siguiendo solo aristas transitables.</summary>
        public HashSet<string> Reachable { get; } = new HashSet<string>();

        /// <summary>
        /// Camino determinista desde el inicio: solo avanza mientras no haya
        /// que elegir. Se detiene en el primer nodo choice con más de una opción.
        /// </summary>
        public List<string> DeterministicPath { get; } = new List<string>();

        /// <summary>Nodo donde se detuvo el camino determinista, y por qué.</summary>
        public string StoppedAt { get; internal set; }
        public string StopReason { get; internal set; }

        public EdgeState StateOf(string nodeId, int index)
            => Edges.TryGetValue((nodeId, index), out var state) ? state : EdgeState.Broken;
    }

    /// <summary>
    /// Evalúa un grafo contra un estado de variables sin reproducirlo.
    /// No aplica efectos: es un análisis estático del "aquí y ahora".
    /// </summary>
    public static class GraphSimulator
    {
        private const int MaxPathLength = 200;

        public static SimulationResult Simulate(DialogueGraph graph, IVariableStorage vars)
        {
            var result = new SimulationResult();
            if (graph == null) return result;

            EvaluateAllEdges(graph, vars, result);
            ComputeReachable(graph, result);
            TracePath(graph, vars, result);

            return result;
        }

        private static void EvaluateAllEdges(DialogueGraph graph, IVariableStorage vars, SimulationResult result)
        {
            foreach (var node in graph.Nodes)
                for (var i = 0; i < node.Out.Count; i++)
                {
                    var edge = node.Out[i];

                    if (string.IsNullOrEmpty(edge.To) || graph.Find(edge.To) == null)
                    {
                        result.Edges[(node.Id, i)] = EdgeState.Broken;
                        continue;
                    }

                    var passable = edge.When == null || edge.When.Evaluate(vars);

                    result.Edges[(node.Id, i)] =
                        passable ? EdgeState.Passable
                        : edge.HideWhenUnavailable ? EdgeState.Hidden
                        : EdgeState.Blocked;
                }
        }

        private static void ComputeReachable(DialogueGraph graph, SimulationResult result)
        {
            var start = graph.Find(graph.Start);
            if (start == null) return;

            result.Reachable.Add(start.Id);
            var pending = new Queue<GraphNode>();
            pending.Enqueue(start);

            while (pending.Count > 0)
            {
                var node = pending.Dequeue();
                for (var i = 0; i < node.Out.Count; i++)
                {
                    if (result.StateOf(node.Id, i) != EdgeState.Passable) continue;

                    var target = node.Out[i].To;
                    if (!result.Reachable.Add(target)) continue;

                    var next = graph.Find(target);
                    if (next != null) pending.Enqueue(next);
                }
            }
        }

        private static void TracePath(DialogueGraph graph, IVariableStorage vars, SimulationResult result)
        {
            var current = graph.Find(graph.Start);
            var visited = new HashSet<string>();

            for (var step = 0; step < MaxPathLength && current != null; step++)
            {
                result.DeterministicPath.Add(current.Id);

                if (!visited.Add(current.Id))
                {
                    result.StoppedAt = current.Id;
                    result.StopReason = "Ciclo detectado.";
                    return;
                }

                if (current.Type == NodeType.Choice)
                {
                    var visibles = CountVisibleOptions(current, result);
                    if (visibles != 1)
                    {
                        result.StoppedAt = current.Id;
                        result.StopReason = visibles == 0
                            ? "Sin opciones disponibles."
                            : $"Decisión del jugador entre {visibles} opciones.";
                        return;
                    }
                }

                var next = FirstPassable(current, result);
                if (next == null)
                {
                    result.StoppedAt = current.Id;
                    result.StopReason = current.Out.Count == 0
                        ? "Fin de la conversación."
                        : "Ninguna salida cumple su condición.";
                    return;
                }

                current = graph.Find(next);
            }

            result.StopReason = "Se alcanzó el límite de pasos.";
        }

        private static int CountVisibleOptions(GraphNode node, SimulationResult result)
        {
            var count = 0;
            for (var i = 0; i < node.Out.Count; i++)
                if (node.Out[i].IsOption && result.StateOf(node.Id, i) == EdgeState.Passable)
                    count++;
            return count;
        }

        private static string FirstPassable(GraphNode node, SimulationResult result)
        {
            for (var i = 0; i < node.Out.Count; i++)
                if (result.StateOf(node.Id, i) == EdgeState.Passable)
                    return node.Out[i].To;
            return null;
        }
    }
}