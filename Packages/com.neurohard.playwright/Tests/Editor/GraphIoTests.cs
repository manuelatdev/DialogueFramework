using NUnit.Framework;
using Neurohard.Playwright.Io;

namespace Neurohard.Playwright.Tests
{
    public class GraphIoTests
    {
        [Test]
        public void RoundTrip_EsEstable()
        {
            var original = GraphSourceTests.BuildGraph();

            var json1 = GraphWriter.ToJson(original);
            var reloaded = GraphReader.FromJson(json1);
            var json2 = GraphWriter.ToJson(reloaded);

            Assert.AreEqual(json1, json2);
            Assert.AreEqual(original.Nodes.Count, reloaded.Nodes.Count);
            Assert.AreEqual(original.Start, reloaded.Start);
        }

        [Test]
        public void RoundTrip_ConservaCondicionesYEfectos()
        {
            var reloaded = GraphReader.FromJson(GraphWriter.ToJson(GraphSourceTests.BuildGraph()));
            var pagar = reloaded.Find("menu").Out[1];

            Assert.IsInstanceOf<Condition.Compare>(pagar.When);
            Assert.AreEqual(2, pagar.Then.Count);
            Assert.AreEqual(50, ((Condition.Compare)pagar.When).Value);
        }

        [Test]
        public void DetectaAristaRota()
        {
            var g = new DialogueGraph();
            var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
            a.Out.Add(new GraphEdge { To = "no_existe" });

            var report = GraphValidator.Validate(g);

            Assert.IsTrue(report.HasErrors);
            StringAssert.Contains("no_existe", report.ToString());
        }

        [Test]
        public void DetectaNodoInalcanzable()
        {
            var g = new DialogueGraph { Start = "a" };
            g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
            g.Add(new GraphNode { Id = "huerfano", Type = NodeType.Line, Line = new GraphLine { Text = "Nadie" } });

            var report = GraphValidator.Validate(g);

            Assert.IsFalse(report.HasErrors);
            StringAssert.Contains("inalcanzable", report.ToString());
        }

        [Test]
        public void ElGrafoDeEjemplo_EsValido()
        {
            Assert.IsTrue(GraphValidator.Validate(GraphSourceTests.BuildGraph()).IsValid);
        }

        [Test]
        public void ElJsonGeneradoNoTieneComasHuerfanas()
        {
            var json = GraphWriter.ToJson(GraphSourceTests.BuildGraph());

            StringAssert.DoesNotContain(": ,", json);
            StringAssert.DoesNotContain(",,", json);
            StringAssert.DoesNotContain("{,", json);
            StringAssert.DoesNotContain(",}", json);
            StringAssert.DoesNotContain("[,", json);
            StringAssert.DoesNotContain(",]", json);
        }

        [Test]
        public void RoundTrip_ConservaCondicionesCompuestas()
        {
            var g = new DialogueGraph { Start = "a" };
            var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
            a.Out.Add(new GraphEdge
            {
                To = "a",
                When = new Condition.All(new Condition[] {
            new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, 50),
            new Condition.Not(new Condition.Compare("enemigo", ComparisonOp.Exists, null)),
            new Condition.Any(new Condition[] {
                new Condition.Compare("clase", ComparisonOp.Equal, "mago"),
                new Condition.Compare("nivel", ComparisonOp.Greater, 10)
            })
        })
            });

            var json1 = GraphWriter.ToJson(g);
            var json2 = GraphWriter.ToJson(GraphReader.FromJson(json1));

            Assert.AreEqual(json1, json2);
            Assert.IsInstanceOf<Condition.All>(GraphReader.FromJson(json1).Find("a").Out[0].When);
        }

        [Test]
        public void RoundTrip_ConservaTiposDeValor()
        {
            var g = new DialogueGraph { Start = "a" };
            var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
            a.Out.Add(new GraphEdge
            {
                To = "a",
                Then = {
            new Effect.Assign("nombre", AssignOp.Set, "alba"),
            new Effect.Assign("visto", AssignOp.Set, true),
            new Effect.Assign("vida", AssignOp.Set, 2.5),
            new Effect.Assign("oro", AssignOp.Add, 10)
        }
            });

            var efectos = GraphReader.FromJson(GraphWriter.ToJson(g)).Find("a").Out[0].Then;

            Assert.AreEqual("alba", ((Effect.Assign)efectos[0]).Value);
            Assert.AreEqual(true, ((Effect.Assign)efectos[1]).Value);
            Assert.AreEqual(2.5, ((Effect.Assign)efectos[2]).Value);
            Assert.AreEqual(10, ((Effect.Assign)efectos[3]).Value);
        }

        [Test]
        public void RoundTrip_ConservaLaReferencia()
        {
            var g = new DialogueGraph { Start = "a" };
            var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "Hola" } });
            a.Out.Add(new GraphEdge
            {
                To = "a",
                When = new Condition.Compare("oro", ComparisonOp.GreaterOrEqual, new VariableRef("precio")),
                Then = { new Effect.Assign("oro", AssignOp.Subtract, new VariableRef("precio")) }
            });

            var json1 = GraphWriter.ToJson(g);
            var reloaded = GraphReader.FromJson(json1);

            var when = (Condition.Compare)reloaded.Find("a").Out[0].When;
            Assert.IsInstanceOf<VariableRef>(when.Value);
            Assert.AreEqual("precio", ((VariableRef)when.Value).Name);
            Assert.AreEqual(json1, GraphWriter.ToJson(reloaded));
        }

        [Test]
        public void ElEscritorCubreTodasLasVariantesDeCondicion()
        {
            Condition[] todas = {
        Condition.Always.Instance,
        new Condition.Compare("a", ComparisonOp.Equal, 1),
        new Condition.All(new Condition[] { Condition.Always.Instance }),
        new Condition.Any(new Condition[] { Condition.Always.Instance }),
        new Condition.Not(Condition.Always.Instance),
        new Condition.Query("puede_comprar", new[] { "cuerda" })
    };

            foreach (var c in todas)
            {
                var g = new DialogueGraph { Start = "a" };
                var a = g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "x" } });
                a.Out.Add(new GraphEdge { To = "a", When = c });

                var json = GraphWriter.ToJson(g);
                Assert.AreEqual(json, GraphWriter.ToJson(GraphReader.FromJson(json)),
                    $"El round-trip falla para {c.GetType().Name}");
            }
        }
        [Test]
        public void DeshacerYRehacer_RestauranElEstado()
        {
            var history = new GraphHistory();
            var g = new DialogueGraph { Start = "a" };
            g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "uno" } });

            history.Record(g);                      // estado con 1 nodo
            g.Add(new GraphNode { Id = "b", Type = NodeType.Line, Line = new GraphLine { Text = "dos" } });

            Assert.IsTrue(history.CanUndo);
            var previo = GraphReader.FromJson(history.Undo(g));
            Assert.AreEqual(1, previo.Nodes.Count);

            Assert.IsTrue(history.CanRedo);
            var rehecho = GraphReader.FromJson(history.Redo(previo));
            Assert.AreEqual(2, rehecho.Nodes.Count);
        }

        [Test]
        public void RegistrarAlgoNuevo_DescartaElRedo()
        {
            var history = new GraphHistory();
            var g = new DialogueGraph { Start = "a" };
            g.Add(new GraphNode { Id = "a", Type = NodeType.Line, Line = new GraphLine { Text = "uno" } });

            history.Record(g);
            history.Undo(g);
            Assert.IsTrue(history.CanRedo);

            history.Record(g);
            Assert.IsFalse(history.CanRedo);
        }
    }
}