using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Editor
{
    /// <summary>Panel lateral con las variables del grafo, editables en vivo.</summary>
    internal sealed class SimulationPanel : VisualElement
    {
        private readonly VisualElement _fields;
        private readonly Label _summary;
        private readonly InMemoryVariableStorage _vars = new InMemoryVariableStorage();
        private readonly Dictionary<string, string> _rawValues = new Dictionary<string, string>();

        private readonly ManualQueryResolver _queries = new ManualQueryResolver();
        private VisualElement _queryFields;

        public EvaluationContext Context => new EvaluationContext(_vars, _queries); public event Action Changed;

        public SimulationPanel()
        {
            style.width = 260;
            style.borderLeftWidth = 1;
            style.borderLeftColor = new Color(0f, 0f, 0f, 0.3f);
            style.paddingLeft = 8;
            style.paddingRight = 8;
            style.paddingTop = 6;

            var title = new Label("Simulación");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 6;
            Add(title);

            _fields = new VisualElement();          // ← variables
            Add(_fields);

            var queryTitle = new Label("Consultas al juego");
            queryTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            queryTitle.style.marginTop = 12;
            queryTitle.style.marginBottom = 4;
            Add(queryTitle);

            _queryFields = new VisualElement();     // ← consultas
            Add(_queryFields);

            _summary = new Label();                 // ← al final
            _summary.style.whiteSpace = WhiteSpace.Normal;
            _summary.style.marginTop = 10;
            _summary.style.opacity = 0.85f;
            Add(_summary);
        }

        /// <summary>Reconstruye los campos a partir de las variables que usa el grafo.</summary>
        public void Rebuild(DialogueGraph graph)
        {
            RebuildVariables(graph);
            RebuildQueries(graph);
        }

        private void RebuildVariables(DialogueGraph graph)
        {
            _fields.Clear();

            var inventory = VariableInventory.Collect(graph);
            if (inventory.Count == 0)
            {
                _fields.Add(new Label("El grafo no usa variables.") { style = { opacity = 0.6f } });
                return;
            }

            foreach (var usage in inventory.Values)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 2;

                var name = new Label(usage.Name);
                name.style.width = 110;
                name.style.overflow = Overflow.Hidden;
                if (usage.IsNeverWritten)
                {
                    name.style.color = new Color(0.95f, 0.75f, 0.35f);
                    name.tooltip = "Se lee pero nunca se escribe en este grafo. " +
                                   "¿Errata, o la fija el juego?";
                }
                row.Add(name);

                var initial = _rawValues.TryGetValue(usage.Name, out var cached) ? cached : "0";
                var field = new TextField { value = initial };
                field.style.flexGrow = 1;

                var variableName = usage.Name;
                field.RegisterValueChangedCallback(evt =>
                {
                    _rawValues[variableName] = evt.newValue;
                    Apply(variableName, evt.newValue);
                    Changed?.Invoke();
                });

                row.Add(field);
                _fields.Add(row);

                _rawValues[usage.Name] = initial;
                Apply(usage.Name, initial);
            }
        }

        public void ShowSummary(SimulationResult result)
        {
            if (result == null) { _summary.text = string.Empty; return; }

            _summary.text =
                $"Camino: {string.Join(" → ", result.DeterministicPath)}\n\n" +
                $"Se detiene en «{result.StoppedAt}»\n{result.StopReason}\n\n" +
                $"Alcanzables: {result.Reachable.Count}";
        }

        /// <summary>Interpreta el texto como bool, número o cadena, en ese orden.</summary>
        private void Apply(string name, string raw)
        {
            if (bool.TryParse(raw, out var b)) { _vars.Set(name, b); return; }
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { _vars.Set(name, i); return; }
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { _vars.Set(name, d); return; }
            _vars.Set(name, raw);
        }

        private void RebuildQueries(DialogueGraph graph)
        {
            _queryFields.Clear();

            var inventory = QueryInventory.Collect(graph);
            if (inventory.Count == 0)
            {
                _queryFields.Add(new Label("El grafo no consulta al juego.") { style = { opacity = 0.6f } });
                return;
            }

            foreach (var usage in inventory.Values)
                foreach (var key in usage.ArgumentSets)
                {
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.style.marginBottom = 2;

                    if (usage.NeedsValue)
                    {
                        var label = new Label(key);
                        label.style.width = 110;
                        label.style.overflow = Overflow.Hidden;
                        label.tooltip = key;
                        row.Add(label);

                        var field = new TextField { value = _queries.GetRaw(key) };
                        field.style.flexGrow = 1;
                        var k = key;
                        field.RegisterValueChangedCallback(evt =>
                        {
                            _queries.Set(k, Parse(evt.newValue));
                            Changed?.Invoke();
                        });
                        row.Add(field);
                    }
                    else
                    {
                        var toggle = new Toggle(key) { value = _queries.GetBool(key) };
                        toggle.style.flexGrow = 1;
                        toggle.tooltip = $"Usada en: {string.Join(", ", usage.UsedIn)}";
                        var k = key;
                        toggle.RegisterValueChangedCallback(evt =>
                        {
                            _queries.Set(k, evt.newValue);
                            Changed?.Invoke();
                        });
                        row.Add(toggle);
                    }

                    _queryFields.Add(row);
                    _queryFields.Add(row);
if (!_queries.Has(key))
    _queries.Set(key, usage.NeedsValue ? (object)"0" : false);                }
        }

        private static object Parse(string raw)
        {
            if (bool.TryParse(raw, out var b)) return b;
            if (int.TryParse(raw, System.Globalization.NumberStyles.Integer,
                             System.Globalization.CultureInfo.InvariantCulture, out var i)) return i;
            if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
            return raw;
        }
    }
}