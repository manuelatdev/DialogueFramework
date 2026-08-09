using System;

namespace Neurohard.Playwright.Io
{
    /// <summary>
    /// Operación atómica sobre un grafo. Al cerrarse, compara el resultado con
    /// el estado inicial y descarta la instantánea si nada cambió.
    /// </summary>
    public sealed class GraphTransaction : IDisposable
    {
        private readonly GraphHistory _history;
        private readonly DialogueGraph _graph;
        private readonly string _before;
        private readonly Action _onChanged;
        private bool _closed;

        internal GraphTransaction(GraphHistory history, DialogueGraph graph, Action onChanged)
        {
            _history = history;
            _graph = graph;
            _onChanged = onChanged;
            _before = GraphWriter.ToJson(graph);
        }

        public void Dispose()
        {
            if (_closed) return;
            _closed = true;

            var after = GraphWriter.ToJson(_graph);
            if (after == _before) return;      // no cambió nada: ni instantánea ni dirty

            _history.Commit(_before);
            _onChanged?.Invoke();
        }
    }
}