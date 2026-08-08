namespace Neurohard.Prompter
{
    public interface IVariableStorage
    {
        bool TryGet<T>(string name, out T value);
        void Set<T>(string name, T value);
        bool Has(string name);
        void Remove(string name);
    }
}