using System.Collections.Generic;

namespace Neurohard.Playwright.Io
{
    /// <summary>
    /// Historial de deshacer basado en instantáneas del JSON completo.
    /// Fuerza bruta a propósito: con grafos de decenas de nodos el coste es
    /// despreciable y la corrección es trivial de razonar.
    /// </summary>
    public sealed class GraphHistory
    {
        private const int MaxDepth = 64;

        private readonly List<string> _undo = new List<string>();
        private readonly List<string> _redo = new List<string>();

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        /// <summary>Registra el estado ANTES de aplicar una operación.</summary>
        public void Record(DialogueGraph graph)
        {
            if (graph == null) return;

            _undo.Add(GraphWriter.ToJson(graph));
            _redo.Clear();

            if (_undo.Count > MaxDepth) _undo.RemoveAt(0);
        }

        /// <summary>Devuelve el JSON anterior, o null si no hay nada que deshacer.</summary>
        public string Undo(DialogueGraph current)
        {
            if (!CanUndo || current == null) return null;

            _redo.Add(GraphWriter.ToJson(current));
            var snapshot = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            return snapshot;
        }

        public string Redo(DialogueGraph current)
        {
            if (!CanRedo || current == null) return null;

            _undo.Add(GraphWriter.ToJson(current));
            var snapshot = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            return snapshot;
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }

        /// <summary>Descarta la última instantánea: la operación no cambió nada.</summary>
        public void Discard()
        {
            if (_undo.Count > 0) _undo.RemoveAt(_undo.Count - 1);
        }
    }
}