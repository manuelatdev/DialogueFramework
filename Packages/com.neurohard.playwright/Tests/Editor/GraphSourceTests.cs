using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Tests
{
    public class GraphSourceTests
    {
        /// <summary>encuentro → menu(paz | soborno si oro>=50) → fin</summary>
        internal static DialogueGraph BuildGraph()
        {
            var g = new DialogueGraph { Start = "encuentro" };

            var encuentro = g.Add(new GraphNode {
                Id = "encuentro", Type = NodeType.Line,
                Line = new GraphLine { Text = "¿Quién anda ahí?", Speaker = "alba" }
            });
            encuentro.Out.Add(new GraphEdge { To = "menu" });

            var menu = g.Add(new GraphNode { Id = "menu", Type = NodeType.Choice });
            menu.Out.Add(new GraphEdge {
                To = "fin", OptionId = "paz",
                Line = new GraphLine { Text = "Vengo en son de paz." }
            });
            menu.Out.Add(new GraphEdge {
                To = "soborno", OptionId = "pagar",
                Line = new GraphLine { Text = "Te doy 50 de oro." },
                When = new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, 50),
                Then = { new Effect.Assign("oro", AssignOp.Subtract, 50),
                         new Effect.Command("abrir_puerta", new[] { "norte" }) }
            });

            var soborno = g.Add(new GraphNode {
                Id = "soborno", Type = NodeType.Line,
                Line = new GraphLine { Text = "Está bien, pasa.", Speaker = "alba" }
            });
            soborno.Out.Add(new GraphEdge { To = "fin" });

            g.Add(new GraphNode { Id = "fin", Type = NodeType.Hub, Fallthrough = FallthroughMode.End });
            return g;
        }

        private static async Task<(GraphDialogueSource, IVariableStorage)> StartAsync(int oro)
        {
            var vars = new InMemoryVariableStorage();
            vars.Set("oro", oro);
            var source = new GraphDialogueSource(BuildGraph());
            await source.StartAsync(new DialogueContext(vars), CancellationToken.None);
            return (source, vars);
        }

        [Test]
        public async Task ConOroSuficiente_LaOpcionEstaDisponible()
        {
            var (source, _) = await StartAsync(60);

            await source.AdvanceAsync(CancellationToken.None);            // la línea
            var options = (DialogueStep.Options)await source.AdvanceAsync(CancellationToken.None);

            Assert.AreEqual(2, options.Choices.Count);
            Assert.IsTrue(options.Choices[1].IsAvailable);
        }

        [Test]
        public async Task SinOro_LaOpcionLlegaDeshabilitadaPeroVisible()
        {
            var (source, _) = await StartAsync(10);

            await source.AdvanceAsync(CancellationToken.None);
            var options = (DialogueStep.Options)await source.AdvanceAsync(CancellationToken.None);

            Assert.AreEqual(2, options.Choices.Count);
            Assert.IsFalse(options.Choices[1].IsAvailable);
        }

        [Test]
        public async Task ElegirLaOpcion_AplicaElEfectoYEmiteElComando()
        {
            var (source, vars) = await StartAsync(60);

            await source.AdvanceAsync(CancellationToken.None);
            await source.AdvanceAsync(CancellationToken.None);
            await source.SelectOptionAsync("pagar", CancellationToken.None);

            var step = await source.AdvanceAsync(CancellationToken.None);

            var command = (DialogueStep.Command)step;
            Assert.AreEqual("abrir_puerta", command.Name);
            Assert.IsTrue(vars.TryGet<int>("oro", out var oro));
            Assert.AreEqual(10, oro);                                     // 60 - 50
        }

        [Test]
        public async Task TrasElComando_ContinuaConLaLineaDelDestino()
        {
            var (source, _) = await StartAsync(60);

            await source.AdvanceAsync(CancellationToken.None);
            await source.AdvanceAsync(CancellationToken.None);
            await source.SelectOptionAsync("pagar", CancellationToken.None);
            await source.AdvanceAsync(CancellationToken.None);            // comando

            var line = ((DialogueStep.Line)await source.AdvanceAsync(CancellationToken.None)).Value;
            Assert.AreEqual("Está bien, pasa.", line.Id.Value);
        }

        [Test]
        public async Task ElHubSinSalidas_TerminaLaConversacion()
        {
            var (source, _) = await StartAsync(10);

            await source.AdvanceAsync(CancellationToken.None);
            await source.AdvanceAsync(CancellationToken.None);
            await source.SelectOptionAsync("paz", CancellationToken.None);

            Assert.IsInstanceOf<DialogueStep.Complete>(
                await source.AdvanceAsync(CancellationToken.None));
        }

        [Test]
        public async Task ParametroStart_ArrancaEnOtroNodo()
        {
            var source = new GraphDialogueSource(BuildGraph());
            await source.StartAsync(
                new DialogueContext(new InMemoryVariableStorage(),
                                    new Dictionary<string, string> { ["start"] = "soborno" }),
                CancellationToken.None);

            var line = ((DialogueStep.Line)await source.AdvanceAsync(CancellationToken.None)).Value;
            Assert.AreEqual("Está bien, pasa.", line.Id.Value);
        }
    }
}