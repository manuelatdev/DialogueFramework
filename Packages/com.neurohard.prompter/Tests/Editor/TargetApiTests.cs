using System.Threading.Tasks;
using NUnit.Framework;

namespace Neurohard.Prompter.Tests
{
    public class TargetApiTests
    {
        [Test]
        public async Task ReproduceUnGuionDeDosLineas()
        {
            var presenter = new RecordingPresenter();
            var prompter = new DialoguePlayer(new DialoguePlayerOptions { Presenters = { presenter } });

            var result = await prompter.Play(DialogueSource.FromLines(
                "Alba: ¿Y esto qué es?",
                "Nilo: Ni idea. Pero brilla."
            ));

            Assert.AreEqual(DialogueOutcome.Completed, result.Outcome);
            Assert.AreEqual(2, presenter.LinesShown.Count);
            Assert.AreEqual("Alba", presenter.LinesShown[0].SpeakerId);
            Assert.AreEqual("¿Y esto qué es?", presenter.LinesShown[0].Text);
            Assert.AreEqual(1, presenter.ClearCount);      // solo al final
        }

        [Test]
        public void SinPresentadores_ElErrorEsExplicito()
        {
            var ex = Assert.Throws<System.InvalidOperationException>(
                () => new DialoguePlayer(new DialoguePlayerOptions()));

            StringAssert.Contains("Presenters", ex.Message);
        }

        [Test]
        public async Task UnPresentadorQueFalla_NoTumbaLaSesion()
        {
            var bueno = new RecordingPresenter();
            var prompter = new DialoguePlayer(new DialoguePlayerOptions {
                Presenters = { new ThrowingPresenter(), bueno }
            });

            var result = await prompter.Play(DialogueSource.FromLines("Una.", "Dos."));

            Assert.AreEqual(DialogueOutcome.Completed, result.Outcome);
            Assert.AreEqual(2, bueno.LinesShown.Count);
        }

        private sealed class ThrowingPresenter : IDialoguePresenter
        {
            public Task ShowLineAsync(ResolvedLine l, System.Threading.CancellationToken ct)
                => throw new System.Exception("boom");
            public Task ShowOptionsAsync(System.Collections.Generic.IReadOnlyList<ResolvedOption> o,
                ResolvedLine p, System.Threading.CancellationToken ct) => Task.CompletedTask;
            public void SkipCurrentPresentation() { }
            public Task ClearAsync(System.Threading.CancellationToken ct) => Task.CompletedTask;
        }
    }
}