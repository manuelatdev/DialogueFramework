using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Neurohard.Prompter;


namespace Neurohard.Playwright.Tests
{
    public class IntegrationTests
    {
        private sealed class Capture : IDialoguePresenter
        {
            public List<string> Texts { get; } = new List<string>();
            public Task ShowLineAsync(ResolvedLine l, CancellationToken ct) { Texts.Add(l.Text); return Task.CompletedTask; }
            public Task ShowOptionsAsync(IReadOnlyList<ResolvedOption> o, ResolvedLine p, CancellationToken ct) => Task.CompletedTask;
            public void SkipCurrentPresentation() { }
            public Task ClearAsync(CancellationToken ct) => Task.CompletedTask;
        }

        private sealed class PickById : IDialogueInput
        {
            private readonly string _id;
            public PickById(string id) => _id = id;
            public Task WaitForAdvanceAsync(CancellationToken ct) => Task.CompletedTask;
            public Task<string> WaitForSelectionAsync(IReadOnlyList<ResolvedOption> o, CancellationToken ct)
                => Task.FromResult(_id);
        }

        [Test]
        public async Task PrompterReproduceUnGrafoSinSaberQueLoEs()
        {
            var vars = new InMemoryVariableStorage();
            vars.Set("oro", 60);

            var presenter = new Capture();
            var comandos = new List<string>();

var prompter = new DialoguePlayer(new DialoguePlayerOptions {
                Presenters = { presenter },
                Input = new PickById("pagar"),
                Variables = vars,
                Commands = new DelegateDispatcher(c => comandos.Add(c.Name))
            });

            var result = await prompter.Play(
                new GraphDialogueSource(GraphSourceTests.BuildGraph()));

            Assert.AreEqual(DialogueOutcome.Completed, result.Outcome);
            CollectionAssert.Contains(presenter.Texts, "Está bien, pasa.");
            CollectionAssert.Contains(comandos, "abrir_puerta");
            Assert.AreEqual(1, result.Choices.Count);
            Assert.AreEqual("pagar", result.Choices[0].OptionId);
        }

        private sealed class DelegateDispatcher : ICommandDispatcher
        {
            private readonly System.Action<DialogueStep.Command> _handler;
            public DelegateDispatcher(System.Action<DialogueStep.Command> h) => _handler = h;
            public bool CanHandle(string name) => true;
            public Task DispatchAsync(DialogueStep.Command c, CancellationToken ct) { _handler(c); return Task.CompletedTask; }
        }
    }
}