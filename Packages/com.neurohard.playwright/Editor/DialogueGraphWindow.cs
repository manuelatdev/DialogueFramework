using System.Linq;
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
        private SimulationPanel _panel;
        private ListView _issues;
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

            // --- barra de herramientas ---
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;

            toolbar.Add(new Button(Reload) { text = "Recargar" });
            toolbar.Add(new Button(() => _view.FrameAll()) { text = "Encuadrar" });

            _status = new Label(" ");
            _status.style.marginLeft = 10;
            _status.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(_status);

            rootVisualElement.Add(toolbar);

            // --- incidencias ---
            BuildIssuesPanel(rootVisualElement);

            // --- grafo + panel de simulación ---
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;

            _view = new DialogueGraphView();
            _view.style.flexGrow = 1;
            body.Add(_view);

            _panel = new SimulationPanel();
            _panel.Changed += RunSimulation;
            body.Add(_panel);

            rootVisualElement.Add(body);
        }

        private void BuildIssuesPanel(VisualElement parent)
        {
            _issues = new ListView
            {
                fixedItemHeight = 20,
                selectionType = SelectionType.Single,
                makeItem = () => new Label { style = { paddingLeft = 6, fontSize = 11 } },
                style = { maxHeight = 110, display = DisplayStyle.None }
            };

            _issues.bindItem = (element, index) =>
            {
                var issue = (ValidationIssue)_issues.itemsSource[index];
                var label = (Label)element;
                label.text = issue.ToString();
                label.style.color = issue.Severity == IssueSeverity.Error
                    ? new Color(0.95f, 0.45f, 0.45f)
                    : new Color(0.95f, 0.8f, 0.4f);
            };

            _issues.selectionChanged += selection =>
            {
                foreach (var item in selection)
                    if (item is ValidationIssue issue && !string.IsNullOrEmpty(issue.NodeId))
                        _view.FocusNode(issue.NodeId);
            };

            parent.Add(_issues);
        }

        private void Reload()
        {
            if (_view == null) return;

            if (_asset == null)
            {
                _status.text = "Selecciona un DialogueGraphAsset y pulsa Recargar.";
                _view.Load(null);
                ShowIssues(null);
                return;
            }

            if (!EditorApplication.isPlaying)
                _asset.Invalidate();

            try
            {
                var graph = _asset.Graph;
                _view.Load(graph);
                _panel.Rebuild(graph);

                var report = GraphValidator.Validate(graph);
                ShowIssues(report);

                _status.text = report.IsValid
                    ? $"{_asset.name} · {graph.Nodes.Count} nodos · sin errores"
                    : $"{_asset.name} · {graph.Nodes.Count} nodos · {report.Issues.Count} incidencias";

                RunSimulation();
            }
            catch (GraphFormatException ex)
            {
                _view.Load(null);
                ShowIssues(null);
                _status.text = ex.Message;
            }
        }

        private void ShowIssues(ValidationReport report)
        {
            var items = report?.Issues.ToList();
            _issues.itemsSource = items;
            _issues.style.display = items != null && items.Count > 0
                ? DisplayStyle.Flex : DisplayStyle.None;
            _issues.RefreshItems();
        }

        private void RunSimulation()
        {
            if (EditorApplication.isPlaying) { _view.ApplySimulation(null); return; }

            if (_asset == null || _view == null) return;

            try
            {
                var result = GraphSimulator.Simulate(_asset.Graph, _panel.Context);
                _view.ApplySimulation(result);
                _panel.ShowSummary(result);
            }
            catch (GraphFormatException) { /* el estado ya lo reporta Reload */ }
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is DialogueGraphAsset asset && asset != _asset)
            {
                _asset = asset;
                Reload();
            }
        }

        private void OnEnable()
        {
            EditorApplication.update += PollActiveNode;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.update -= PollActiveNode;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) RunSimulation();
        }
        private void PollActiveNode()
        {
            if (_view == null) return;
            _view.SetActiveNode(EditorApplication.isPlaying ? _asset?.ActiveSource?.CurrentNodeId : null);
        }
    }
}