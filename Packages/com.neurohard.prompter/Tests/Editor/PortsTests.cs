using NUnit.Framework;

namespace Neurohard.Prompter.Tests
{
    public class PortsTests
    {
        [Test]
        public void PassthroughProvider_UsaElIdComoTexto()
        {
            var resolved = new PassthroughLineProvider()
                .Resolve(new DialogueLine("Hola qué tal", "Alba"));

            Assert.AreEqual("Hola qué tal", resolved.Text);
            Assert.AreEqual("Alba", resolved.SpeakerId);
        }

        [Test]
        public void VariableStorage_DevuelveFalseSiElTipoNoCuadra()
        {
            var vars = new InMemoryVariableStorage();
            vars.Set("oro", 100);

            Assert.IsTrue(vars.TryGet<int>("oro", out var oro));
            Assert.AreEqual(100, oro);
            Assert.IsFalse(vars.TryGet<string>("oro", out _));
        }
    }
}