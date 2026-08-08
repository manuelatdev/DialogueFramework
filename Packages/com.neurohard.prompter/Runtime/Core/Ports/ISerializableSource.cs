namespace Neurohard.Prompter
{
    /// <summary>Opcional. El runner devuelve el token tal cual, sin interpretarlo.</summary>
    public interface ISerializableSource
    {
        string CaptureState();
        void RestoreState(string state);
    }
}