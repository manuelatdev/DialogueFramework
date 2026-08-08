using NUnit.Framework;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Tests
{
    public class SimulatorTests
    {
        private static IVariableStorage Vars(int oro)
        {
            var v = new InMemoryVariableStorage();
            v.Set("oro", oro);
            return v;
        }

        [Test]
        public void ConOro_LaAristaCondicionalEsTransitable()
        {
            var result = GraphSimulator.Simulate(GraphSourceTests.BuildGraph(), Vars(60));
            Assert.AreEqual(EdgeState.Passable, result.StateOf("menu", 1));
        }

        [Test]
        public void SinOro_LaAristaQuedaBloqueada()
        {
            var result = GraphSimulator.Simulate(GraphSourceTests.BuildGraph(), Vars(10));
            Assert.AreEqual(EdgeState.Blocked, result.StateOf("menu", 1));
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