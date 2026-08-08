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

            var prompter = new Prompter(new PrompterOptions {
                Presenters = { presenter }
            });

            var result = await prompter.Play(DialogueSource.FromLines(
                "Alba: ¿Y esto qué es?",
                "Nilo: Ni idea. Pero brilla."
            ));

            Assert.AreEqual(DialogueOutcome.Completed, result.Outcome);
            Assert.AreEqual(2, presenter.LinesShown.Count);
            Assert.AreEqual("Alba", presenter.LinesShown[0].SpeakerId);
        }
    }
}