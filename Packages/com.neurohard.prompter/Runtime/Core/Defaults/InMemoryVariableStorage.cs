using System.Collections.Generic;

namespace Neurohard.Prompter
{
    public sealed class InMemoryVariableStorage : IVariableStorage
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public bool TryGet<T>(string name, out T value)
        {
            if (_values.TryGetValue(name, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        public void Set<T>(string name, T value) => _values[name] = value;
        public bool Has(string name) => _values.ContainsKey(name);
        public void Remove(string name) => _values.Remove(name);
    }
}