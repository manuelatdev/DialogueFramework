using NUnit.Framework;
using Neurohard.Prompter;

namespace Neurohard.Playwright.Tests
{
    public class ModelTests
    {
        [Test]
        public void RenombrarNodo_ActualizaLasAristas()
        {
            var g = new DialogueGraph();
            var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line });
            g.Add(new GraphNode { Id = "b", Type = NodeType.Line });
            a.Out.Add(new GraphEdge { To = "b" });

            g.Rename("b", "b2");

            Assert.AreEqual("b2", a.Out[0].To);
            Assert.IsNotNull(g.Find("b2"));
            Assert.IsNull(g.Find("b"));
        }

        [Test]
        public void CondicionCompara_ConVariablesNumericas()
        {
            var vars = new InMemoryVariableStorage();
            vars.Set("oro", 60);

            var c = new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, 50);
            Assert.IsTrue(c.Evaluate(vars));

            vars.Set("oro", 10);
            Assert.IsFalse(c.Evaluate(vars));
        }

        [Test]
        public void EfectoResta_ModificaLaVariable()
        {
            var vars = new InMemoryVariableStorage();
            vars.Set("oro", 100);

            new Effect.Assign("oro", AssignOp.Subtract, 50).Apply(vars);

            Assert.IsTrue(vars.TryGet<int>("oro", out var oro));
            Assert.AreEqual(50, oro);
        }
    }
}