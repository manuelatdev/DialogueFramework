using UnityEngine;
using Neurohard.Playwright.Io;

namespace Neurohard.Playwright.Unity
{
    /// <summary>
    /// Envoltorio delgado sobre un TextAsset JSON. El texto es la fuente de la
    /// verdad; este asset solo permite arrastrarlo en el inspector y cachea el
    /// grafo parseado.
    /// </summary>
    [CreateAssetMenu(menuName = "Neurohard/Dialogue Graph", fileName = "NuevoGrafo")]
    public sealed class DialogueGraphAsset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Archivo JSON con los datos del grafo. Si está vacío, se generará uno al guardar.")]
        private TextAsset json;

        [System.NonSerialized] private DialogueGraph _cached;

        public TextAsset Json
        {
            get => json;
            set
            {
                if (json == value) return;
                json = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Parsea al primer uso. Lanza si no hay TextAsset o si el JSON es inválido.
        /// Para el editor, usa TryGetGraph.
        /// </summary>
        public DialogueGraph Graph
        {
            get
            {
                if (_cached != null) return _cached;

                if (json == null)
                    throw new System.InvalidOperationException(
                        $"El asset '{name}' no tiene un TextAsset asignado.");

                _cached = GraphReader.FromJson(json.text);
                return _cached;
            }
        }

        /// <summary>
        /// Versión no lanzante para el editor: un JSON roto no debe reventar el
        /// bucle de dibujado del inspector en cada repintado.
        /// </summary>
        public bool TryGetGraph(out DialogueGraph graph, out string error)
        {
            graph = null;
            error = null;

            try
            {
                graph = Graph;
                return true;
            }
            catch (System.Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Descarta la caché. Fuerza una relectura del JSON al próximo uso.</summary>
        public void Invalidate() => _cached = null;

        /// <summary>true si hay un grafo en memoria, con o sin cambios sin guardar.</summary>
        public bool HasGraphInMemory => _cached != null;

        public GraphDialogueSource CreateSource()
        {
            var graph = Graph;

#if UNITY_EDITOR
            var report = GraphValidator.Validate(graph);
            if (report.HasErrors)
                Debug.LogError($"[{name}] El grafo tiene errores y puede fallar a mitad de conversación:\n{report}");
#endif

            var source = new GraphDialogueSource(graph);
#if UNITY_EDITOR
            _lastSource = source;
#endif
            return source;
        }

#if UNITY_EDITOR
        [System.NonSerialized] private GraphDialogueSource _lastSource;

        /// <summary>Última fuente creada desde este asset. Solo para depuración.</summary>
        public GraphDialogueSource ActiveSource => _lastSource;

        /// <summary>
        /// Copia del TextAsset con el que se validó por última vez. Permite distinguir
        /// un cambio real de referencia de las revalidaciones espurias de Unity.
        /// </summary>
        [SerializeField, HideInInspector] private TextAsset _lastValidatedJson;

        private void OnValidate()
        {
            // OnValidate se dispara al recompilar, al entrar en Play Mode y al hacer
            // undo, no solo al arrastrar un archivo. Invalidar sin comprobar destruiría
            // los cambios sin guardar que viven únicamente en la caché.
            if (_lastValidatedJson == json) return;

            _lastValidatedJson = json;
            Invalidate();
        }

        /// <summary>Serializa el grafo en memoria y lo escribe al TextAsset en disco.</summary>
        public void Save()
        {
            if (_cached == null)
                throw new System.InvalidOperationException(
                    $"El asset '{name}' no tiene grafo en memoria que guardar.");

            if (json == null) CreateJsonAsset();

            var path = UnityEditor.AssetDatabase.GetAssetPath(json);
            System.IO.File.WriteAllText(path, GraphWriter.ToJson(_cached));
            UnityEditor.AssetDatabase.ImportAsset(path);
        }

        /// <summary>Crea un .json vacío junto al ScriptableObject y lo asigna.</summary>
        private void CreateJsonAsset()
        {
            var soPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(soPath))
                throw new System.InvalidOperationException(
                    "No se puede guardar: el asset no existe en disco.");

            var path = System.IO.Path.ChangeExtension(soPath, ".json");
            System.IO.File.WriteAllText(path, GraphWriter.ToJson(new DialogueGraph()));
            UnityEditor.AssetDatabase.ImportAsset(path);

            json = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            _lastValidatedJson = json;          // evita que OnValidate tire la caché
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}