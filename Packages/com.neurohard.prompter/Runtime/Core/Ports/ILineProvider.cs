namespace Neurohard.Prompter
{
    /// <summary>Convierte un LineId en texto final: idioma, interpolación, voz.</summary>
    public interface ILineProvider
    {
        ResolvedLine Resolve(DialogueLine line);
    }
}