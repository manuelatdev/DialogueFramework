using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Neurohard.Prompter.Tests
{
    public class LinearScriptTests
    {
        private static async Task<DialogueStep[]> DrainAsync(IDialogueSource source, int max = 20)
        {
            await source.StartAsync(new DialogueContext(new InMemoryVariableStorage()), CancellationToken.None);

            var steps = new System.Collections.Generic.List<DialogueStep>();
            for (var i = 0; i < max; i++)
            {
                var step = await source.AdvanceAsync(CancellationToken.None);
                steps.Add(step);
                if (step is DialogueStep.Complete) break;
            }
            return steps.ToArray();
        }

        [Test]
        public async Task SeparaHablanteYTexto()
        {
            var steps = await DrainAsync(DialogueSource.FromLines("Alba: ¿Y esto qué es?"));

            var line = ((DialogueStep.Line)steps[0]).Value;
            Assert.AreEqual("Alba", line.SpeakerId);
            Assert.AreEqual("¿Y esto qué es?", line.Id.Value);
        }

        [Test]
        public async Task DosPuntosDentroDeLaFrase_NoSeTomanComoHablante()
        {
            var steps = await DrainAsync(DialogueSource.FromLines("Y entonces dijo: vete"));

            var line = ((DialogueStep.Line)steps[0]).Value;
            Assert.IsEmpty(line.SpeakerId);
            Assert.AreEqual("Y entonces dijo: vete", line.Id.Value);
        }

        [Test]
        public async Task ParseaComandosConArgumentos()
        {
            var steps = await DrainAsync(DialogueSource.FromLines(">> dar_objeto espada 2"));

            var cmd = (DialogueStep.Command)steps[0];
            Assert.AreEqual("dar_objeto", cmd.Name);
            Assert.AreEqual(new[] { "espada", "2" }, cmd.Arguments);
        }

        [Test]
        public async Task TerminaSiempreEnComplete()
        {
            var steps = await DrainAsync(DialogueSource.FromLines("Una.", "", "Dos."));

            Assert.AreEqual(3, steps.Length);          // la vacía se descarta
            Assert.IsInstanceOf<DialogueStep.Complete>(steps[2]);
        }

        [Test]
        public async Task StartRebobina()
        {
            var source = DialogueSource.FromLines("Una.", "Dos.");
            await DrainAsync(source);
            var segundaVuelta = await DrainAsync(source);

            Assert.AreEqual(3, segundaVuelta.Length);
        }

        [Test]
        public async Task CapturaYRestauraPosicion()
        {
            var source = DialogueSource.FromLines("Una.", "Dos.", "Tres.");
            await source.StartAsync(new DialogueContext(new InMemoryVariableStorage()), CancellationToken.None);
            await source.AdvanceAsync(CancellationToken.None);

            var state = ((ISerializableSource)source).CaptureState();
            await source.AdvanceAsync(CancellationToken.None);
            ((ISerializableSource)source).RestoreState(state);

            var line = ((DialogueStep.Line)await source.AdvanceAsync(CancellationToken.None)).Value;
            Assert.AreEqual("Dos.", line.Id.Value);
        }
    }
}