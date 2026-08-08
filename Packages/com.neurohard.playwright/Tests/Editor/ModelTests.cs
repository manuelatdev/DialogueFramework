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

        [Test]
        public void VariableInexistente_CuentaComoCeroEnComparacionNumerica()
        {
            var v = new InMemoryVariableStorage();
            Assert.IsTrue(new Condition.Compare("contador", ComparisonOp.Equal, 0).Evaluate(v));
            Assert.IsFalse(new Condition.Compare("contador", ComparisonOp.GreaterOrEqual, 3).Evaluate(v));
        }

        [Test]
        public void VariableInexistente_EsDistintaDeUnaCadena()
        {
            var v = new InMemoryVariableStorage();
            Assert.IsTrue(new Condition.Compare("estado", ComparisonOp.NotEqual, "hecho").Evaluate(v));
            Assert.IsFalse(new Condition.Compare("estado", ComparisonOp.Equal, "hecho").Evaluate(v));
        }

        [Test]
        public void ExistsSigueDistinguiendoNoDefinidoDeCero()
        {
            var v = new InMemoryVariableStorage();
            Assert.IsFalse(new Condition.Compare("c", ComparisonOp.Exists, null).Evaluate(v));
            v.Set("c", 0);
            Assert.IsTrue(new Condition.Compare("c", ComparisonOp.Exists, null).Evaluate(v));
        }

        [Test]
        public void SumarSobreVariableInexistente_DaElDeltaComoInt()
        {
            var v = new InMemoryVariableStorage();
            new Effect.Assign("oro", AssignOp.Add, 10).Apply(v);
            Assert.IsTrue(v.TryGet<int>("oro", out var oro));
            Assert.AreEqual(10, oro);
        }

        [Test]
        public void SumarDecimales_NoDegradaADoble()
        {
            var v = new InMemoryVariableStorage();
            v.Set("vida", 2.5);
            new Effect.Assign("vida", AssignOp.Add, 0.5).Apply(v);
            Assert.IsTrue(v.TryGet<double>("vida", out var vida));
            Assert.AreEqual(3.0, vida);
        }

        [Test]
public void CompararVariableContraVariable()
{
    var v = new InMemoryVariableStorage();
    v.Set("oro", 10);
    v.Set("precio", 3);

    var c = new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, new VariableRef("precio"));
    Assert.IsTrue(c.Evaluate(v));

    v.Set("precio", 50);
    Assert.IsFalse(c.Evaluate(v));
}

[Test]
public void RestarUnaCantidadReferenciada()
{
    var v = new InMemoryVariableStorage();
    v.Set("oro", 10);
    v.Set("precio", 3);

    new Effect.Assign("oro", AssignOp.Subtract, new VariableRef("precio")).Apply(v);

    Assert.IsTrue(v.TryGet<int>("oro", out var oro));
    Assert.AreEqual(7, oro);
}

[Test]
public void ElInventarioCuentaLaVariableReferenciada()
{
    var g = new DialogueGraph { Start = "a" };
    var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
    a.Out.Add(new GraphEdge {
        To = "a",
        When = new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, new VariableRef("precio"))
    });

    var inventory = VariableInventory.Collect(g);
    Assert.IsTrue(inventory.ContainsKey("precio"));
    Assert.IsFalse(inventory["precio"].IsNeverRead);
}
    }
}