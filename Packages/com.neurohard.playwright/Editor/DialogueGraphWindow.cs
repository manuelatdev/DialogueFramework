using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Neurohard.Playwright.Unity;

namespace Neurohard.Playwright.Editor
{
    public sealed class DialogueGraphWindow : EditorWindow
    {
        // Serializado para sobrevivir a los domain reloads.
        [SerializeField] private DialogueGraphAsset _asset;

        private DialogueGraphView _view;
        private SimulationPanel _panel;
        private ListView _issues;
        private Label _status;

        [MenuItem("Window/Neurohard/Visor de grafos")]
        public static void ShowWindow() => GetWindow<DialogueGraphWindow>();

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
            if (asset != null && asset != _asset)
            {
                if (!ConfirmDiscard("Abrir otro grafo descartará los cambios.")) return;
                _asset = asset;
                hasUnsavedChanges = false;
            }

            if (_view == null) BuildUi();
            Reload();
        }

        private void CreateGUI()
        {
            saveChangesMessage = "Este grafo tiene cambios sin guardar. ¿Quieres guardarlos?";
            BuildUi();

            // Tras un domain reload el grafo en memoria se pierde: es [NonSerialized].
            if (hasUnsavedChanges)
            {
                hasUnsavedChanges = false;
                Debug.LogWarning(
                    "[Playwright] Se recompilaron los scripts con cambios sin guardar en el grafo. " +
                    "Los cambios en memoria se han perdido y se ha recargado desde disco.");
            }

            if (_asset != null) Reload();
        }

        private void BuildUi()
        {
            if (_view != null) return;

            var toolbar = new Toolbar();
            toolbar.Add(new ToolbarButton(Reload) { text = "Recargar" });
            toolbar.Add(new ToolbarButton(() => _view?.FrameAll()) { text = "Encuadrar" });
            toolbar.Add(new ToolbarButton(ManualSave) { text = "Guardar" });

            _status = new Label(" ");
            _status.style.marginLeft = 10;
            _status.style.unityTextAlign = TextAnchor.MiddleLeft;
            toolbar.Add(_status);

            rootVisualElement.Add(toolbar);

            BuildIssuesPanel(rootVisualElement);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;

            _view = new DialogueGraphView();
            _view.style.flexGrow = 1;
            _view.Modified += MarkDirty;
            body.Add(_view);

            _panel = new SimulationPanel();
            _panel.Changed += RunSimulation;
            body.Add(_panel);

            rootVisualElement.Add(body);

            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.S && (evt.commandKey || evt.ctrlKey))
                {
                    ManualSave();
                    evt.StopPropagation();
                }
            });
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
            if (!ConfirmDiscard("Recargar descartará los cambios en memoria.")) return;

            hasUnsavedChanges = false;
            UpdateTitle();

            if (_asset == null)
            {
                _status.text = "Selecciona un DialogueGraphAsset.";
                _view.Load(null);
                ShowIssues(null);
                return;
            }

            if (!EditorApplication.isPlaying)
                _asset.Invalidate();

            if (!_asset.TryGetGraph(out var graph, out var error))
            {
                _view.Load(null);
                ShowIssues(null);
                _status.text = error;
                return;
            }

            _view.Load(graph);
            _panel.Rebuild(graph);

            var report = GraphValidator.Validate(graph);
            ShowIssues(report);

            _status.text = report.IsValid
                ? $"{_asset.name} · {graph.Nodes.Count} nodos · sin errores"
                : $"{_asset.name} · {graph.Nodes.Count} nodos · {report.Issues.Count} incidencias";

            RunSimulation();
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
            if (_view == null || _asset == null) return;

            if (EditorApplication.isPlaying) { _view.ApplySimulation(null); return; }
            if (!_asset.TryGetGraph(out var graph, out _)) return;

            var result = GraphSimulator.Simulate(graph, _panel.Context);
            _view.ApplySimulation(result);
            _panel.ShowSummary(result);
        }

        // --- guardado ---------------------------------------------------------

        private void MarkDirty()
        {
            if (hasUnsavedChanges) return;
            hasUnsavedChanges = true;
            UpdateTitle();
        }

        private void ManualSave()
        {
            if (hasUnsavedChanges) SaveChanges();
        }

        public override void SaveChanges()
        {
            if (_asset == null) return;

            try
            {
                _asset.Save();
                _status.text = $"{_asset.name} guardado.";
                base.SaveChanges();          // pone hasUnsavedChanges a false
                UpdateTitle();
            }
            catch (System.Exception ex)
            {
                _status.text = $"No se pudo guardar: {ex.Message}";
                Debug.LogError(ex);
                // No llamamos a base: los cambios siguen pendientes.
            }
        }

        private void UpdateTitle()
        {
            var nombre = _asset != null ? _asset.name : "Grafo de diálogo";
            titleContent = new GUIContent(hasUnsavedChanges ? nombre + " *" : nombre);
        }

        private bool ConfirmDiscard(string consecuencia)
            => !hasUnsavedChanges || EditorUtility.DisplayDialog(
                "Cambios sin guardar",
                $"«{(_asset != null ? _asset.name : "El grafo")}» tiene cambios sin guardar. {consecuencia}",
                "Descartar", "Cancelar");

        // --- ciclo de vida ----------------------------------------------------

        private void OnSelectionChange()
        {
            if (!(Selection.activeObject is DialogueGraphAsset asset) || asset == _asset) return;
            if (!ConfirmDiscard("Cambiar de grafo descartará los cambios.")) return;

            _asset = asset;
            hasUnsavedChanges = false;
            Reload();
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