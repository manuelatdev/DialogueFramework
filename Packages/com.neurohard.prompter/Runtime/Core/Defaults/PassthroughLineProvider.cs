namespace Neurohard.Prompter
{
    /// <summary>El LineId ES el texto. Suficiente hasta que localices.</summary>
    public sealed class PassthroughLineProvider : ILineProvider
    {
        public ResolvedLine Resolve(DialogueLine line)
            => new ResolvedLine(line.Id, line.Id.Value, line.SpeakerId, line.Tags);
    }
}