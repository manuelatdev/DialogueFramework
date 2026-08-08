using UnityEditor;
using UnityEngine;
using Neurohard.Playwright.Io;
using Neurohard.Playwright.Unity;

namespace Neurohard.Playwright.Editor
{
    [CustomEditor(typeof(DialogueGraphAsset))]
    public sealed class DialogueGraphAssetEditor : UnityEditor.Editor
    {
        private ValidationReport _report;
        private string _error;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Validar grafo")) Validate();

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
                EditorGUILayout.HelpBox(issue.ToString(),
                    issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning);
        }

        private void Validate()
        {
            _error = null;
            _report = null;

            var asset = (DialogueGraphAsset)target;
            asset.Invalidate();

            try { _report = GraphValidator.Validate(asset.Graph); }
            catch (GraphFormatException ex) { _error = ex.Message; }
            catch (System.Exception ex) { _error = ex.Message; }
        }
    }
}