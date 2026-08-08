using UnityEditor;
using UnityEngine;
using Neurohard.Playwright.Unity;

namespace Neurohard.Playwright.Editor
{
    [CustomEditor(typeof(DialogueGraphAsset))]
    public sealed class DialogueGraphAssetEditor : UnityEditor.Editor
    {
        private ValidationReport _report;
        private string _error;

        private void OnEnable() => Validate(false);

        public override void OnInspectorGUI()
        {
            var asset = (DialogueGraphAsset)target;

            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Abrir visor de grafos", GUILayout.Height(30)))
                AssetDatabase.OpenAsset(asset);

            EditorGUILayout.Space();

            // Releer de disco descarta lo que haya en memoria: por eso es explícito.
            if (GUILayout.Button("Revalidar desde disco"))
                Validate(true);

            EditorGUILayout.Space();

            if (_error != null)
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
                return;
            }

            if (_report == null) return;

            if (_report.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("Grafo válido, sin incidencias.", MessageType.Info);
                return;
            }

            foreach (var issue in _report.Issues)
                EditorGUILayout.HelpBox(
                    issue.ToString(),
                    issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning);
        }

        /// <summary>
        /// Valida el grafo. Con relectura, descarta la caché y vuelve a leer el JSON;
        /// sin ella, valida lo que haya en memoria para no pisar ediciones en curso.
        /// </summary>
        private void Validate(bool relerDeDisco)
        {
            _error = null;
            _report = null;

            var asset = (DialogueGraphAsset)target;

            if (asset.Json == null)
            {
                _error = "Este asset no tiene JSON asignado. Ábrelo en el visor para generar uno, " +
                         "o arrastra un TextAsset al campo de arriba.";
                return;
            }

            if (relerDeDisco) asset.Invalidate();

            if (!asset.TryGetGraph(out var graph, out var error))
            {
                _error = error;
                return;
            }

            _report = GraphValidator.Validate(graph);
        }
    }
}