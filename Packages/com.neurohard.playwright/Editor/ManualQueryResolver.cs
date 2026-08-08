using System.Collections.Generic;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Editor
{
    /// <summary>Resolver manejado a mano desde el panel del visor.</summary>
    internal sealed class ManualQueryResolver : IQueryResolver
    {
        private readonly Dictionary<string, object> _answers = new Dictionary<string, object>();

        public bool CanResolve(string queryName) => true;

        public object Resolve(string queryName, IReadOnlyList<string> arguments)
            => _answers.TryGetValue(QueryInventory.KeyOf(queryName, arguments), out var value)
                ? value
                : false;

        public void Set(string key, object value) => _answers[key] = value;

        public bool GetBool(string key)
            => _answers.TryGetValue(key, out var v) && v is bool b && b;

        public string GetRaw(string key)
            => _answers.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        public void Clear() => _answers.Clear();

        public bool Has(string key) => _answers.ContainsKey(key);
    }
}