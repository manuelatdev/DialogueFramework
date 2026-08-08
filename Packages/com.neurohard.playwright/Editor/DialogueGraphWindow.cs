using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using Neurohard.Playwright.Io;
using Neurohard.Playwright.Unity;

namespace Neurohard.Playwright.Editor
{
    public sealed class DialogueGraphWindow : EditorWindow
    {
        private DialogueGraphAsset _asset;
        private DialogueGraphView _view;
        private Label _status;

        [MenuItem("Window/Neurohard/Visor de grafos")]
        public static void ShowWindow() => GetWindow<DialogueGraphWindow>().Initialize(null);

        [OnOpenAsset]
        public static bool OnOpenAsset(int entityId, int line)
        {
            if (!(EditorUtility.EntityIdToObject(entityId) is DialogueGraphAsset asset))
                return false;

            GetWindow<DialogueGraphWindow>().Initialize(asset);
            return true;
        }

        private void Initialize(DialogueGraphAsset asset)
        {
            titleContent = new GUIContent("Grafo de diálogo");
            if (asset != null) _asset = asset;
            BuildUi();
            Reload();
        }

        private void CreateGUI() => BuildUi();

        private void BuildUi()
        {
            if (_view != null) return;

            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;

            var recargar = new Button(Reload) { text = "Recargar" };
            toolbar.Add(recargar);
            toolbar.Add(new Button(() => _view.FrameAll()) { text = "Encuadrar" });


            _status = new Label(" ");
            _status.style.marginLeft = 10;
            _status.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(_status);

            rootVisualElement.Add(toolbar);

            _view = new DialogueGraphView();
            rootVisualElement.Add(_view);
        }

        private void Reload()
        {
            if (_view == null) return;

            if (_asset == null)
            {
                _status.text = "Selecciona un DialogueGraphAsset y pulsa Recargar.";
                _view.Load(null);
                return;
            }

            if (!EditorApplication.isPlaying)
                _asset.Invalidate();

            _asset.Invalidate();

            try
            {
                var graph = _asset.Graph;
                _view.Load(graph);

                var report = GraphValidator.Validate(graph);
                _status.text = report.IsValid
                    ? $"{_asset.name} · {graph.Nodes.Count} nodos · sin errores"
                    : $"{_asset.name} · {graph.Nodes.Count} nodos · {report.Issues.Count} incidencias (ver inspector)";
            }
            catch (GraphFormatException ex)
            {
                _view.Load(null);
                _status.text = ex.Message;
            }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is DialogueGraphAsset asset && asset != _asset)
            {
                _asset = asset;
                Reload();
            }
        }

        private void OnEnable() => EditorApplication.update += PollActiveNode;
        private void OnDisable() => EditorApplication.update -= PollActiveNode;

        private void PollActiveNode()
        {
            if (_view == null) return;
            var id = EditorApplication.isPlaying ? _asset?.ActiveSource?.CurrentNodeId : null;
            if (EditorApplication.isPlaying && Time.frameCount % 60 == 0)
                Debug.Log($"asset={_asset?.name ?? "null"} source={_asset?.ActiveSource != null} node={id ?? "null"}");
            _view.SetActiveNode(id);
        }
    }
}