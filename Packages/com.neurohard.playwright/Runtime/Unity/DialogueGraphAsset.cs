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
        [SerializeField] private TextAsset json;

        [System.NonSerialized] private DialogueGraph _cached;

        public TextAsset Json
        {
            get => json;
            set { json = value; _cached = null; }
        }

        /// <summary>Parsea al primer uso. Lanza GraphFormatException si el JSON es inválido.</summary>
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

        /// <summary>Descarta la caché. Útil tras editar el JSON en caliente.</summary>
        public void Invalidate() => _cached = null;

#if UNITY_EDITOR
        [System.NonSerialized] private GraphDialogueSource _lastSource;
        /// <summary>Última fuente creada desde este asset. Solo para depuración.</summary>
        public GraphDialogueSource ActiveSource => _lastSource;
#endif
        public GraphDialogueSource CreateSource()
        {
            var source = new GraphDialogueSource(Graph);
#if UNITY_EDITOR
            _lastSource = source;
#endif
            return source;
        }
    }
}