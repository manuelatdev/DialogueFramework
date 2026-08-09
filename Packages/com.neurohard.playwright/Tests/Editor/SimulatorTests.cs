using NUnit.Framework;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Tests
{
    public class SimulatorTests
    {
        private static EvaluationContext Vars(int oro)
        {
            var v = new InMemoryVariableStorage();
            v.Set("oro", oro);
            return new EvaluationContext(v);
        }

        [Test]
        public void ConOro_LaAristaCondicionalEsTransitable()
        {
            var graph = GraphSourceTests.BuildGraph();
            var result = GraphSimulator.Simulate(graph, Vars(60));

            var pagar = graph.Find("menu").Out[1];
            Assert.AreEqual(EdgeState.Passable, result.StateOf(pagar));
        }

        [Test]
        public void SinOro_LaAristaQuedaBloqueada()
        {
            var graph = GraphSourceTests.BuildGraph();
            var result = GraphSimulator.Simulate(graph, Vars(10));

            var pagar = graph.Find("menu").Out[1];
            Assert.AreEqual(EdgeState.Blocked, result.StateOf(pagar));
        }

        [Test]
        public void ElCaminoSeDetieneEnLaDecision()
        {
            var result = GraphSimulator.Simulate(GraphSourceTests.BuildGraph(), Vars(60));

            Assert.AreEqual("menu", result.StoppedAt);
            StringAssert.Contains("Decisión", result.StopReason);
            CollectionAssert.Contains(result.DeterministicPath, "encuentro");
        }

        [Test]
        public void SinOro_SoloUnaOpcionYElCaminoContinua()
        {
            var result = GraphSimulator.Simulate(GraphSourceTests.BuildGraph(), Vars(10));

            Assert.AreEqual("fin", result.StoppedAt);
            CollectionAssert.Contains(result.DeterministicPath, "fin");
        }

        [Test]
        public void ElInventarioDetectaVariablesNuncaEscritas()
        {
            var inventory = VariableInventory.Collect(GraphSourceTests.BuildGraph());

            Assert.IsTrue(inventory.ContainsKey("oro"));
            Assert.IsFalse(inventory["oro"].IsNeverWritten);   // se resta en la arista
            Assert.IsFalse(inventory["oro"].IsNeverRead);
        }
    }
}