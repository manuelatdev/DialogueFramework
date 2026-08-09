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
        /// <summary>Estado de cada arista, indexado por la propia arista.</summary>
        private readonly Dictionary<GraphEdge, EdgeState> _edges =
            new Dictionary<GraphEdge, EdgeState>();

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

        internal void SetState(GraphEdge edge, EdgeState state) => _edges[edge] = state;

        public EdgeState StateOf(GraphEdge edge)
            => edge != null && _edges.TryGetValue(edge, out var state) ? state : EdgeState.Broken;
    }

    /// <summary>
    /// Evalúa un grafo contra un estado de variables sin reproducirlo.
    /// No aplica efectos: es un análisis estático del "aquí y ahora".
    /// </summary>
    public static class GraphSimulator
    {
        private const int MaxPathLength = 200;

        public static SimulationResult Simulate(DialogueGraph graph, EvaluationContext ctx)
        {
            var result = new SimulationResult();
            if (graph == null) return result;

            EvaluateAllEdges(graph, ctx, result);
            ComputeReachable(graph, result);
            TracePath(graph, result);

            return result;
        }

        private static void EvaluateAllEdges(DialogueGraph graph, EvaluationContext ctx, SimulationResult result)
        {
            foreach (var node in graph.Nodes)
                foreach (var edge in node.Out)
                {
                    if (string.IsNullOrEmpty(edge.To) || graph.Find(edge.To) == null)
                    {
                        result.SetState(edge, EdgeState.Broken);
                        continue;
                    }

                    var passable = edge.When == null || edge.When.Evaluate(ctx);

                    result.SetState(edge,
                        passable ? EdgeState.Passable
                        : edge.HideWhenUnavailable ? EdgeState.Hidden
                        : EdgeState.Blocked);
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
                foreach (var edge in node.Out)
                {
                    if (result.StateOf(edge) != EdgeState.Passable) continue;
                    if (!result.Reachable.Add(edge.To)) continue;

                    var next = graph.Find(edge.To);
                    if (next != null) pending.Enqueue(next);
                }
            }
        }

        private static void TracePath(DialogueGraph graph, SimulationResult result)
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
            foreach (var edge in node.Out)
                if (edge.IsOption && result.StateOf(edge) == EdgeState.Passable)
                    count++;
            return count;
        }

        private static string FirstPassable(GraphNode node, SimulationResult result)
        {
            foreach (var edge in node.Out)
                if (result.StateOf(edge) == EdgeState.Passable)
                    return edge.To;
            return null;
        }
    }
}