using NUnit.Framework;

namespace Neurohard.Prompter.Tests
{
    public class ContractTests
    {
        [Test]
        public void UnPasoLine_ExponeSuLinea()
        {
            var step = new DialogueStep.Line(new DialogueLine("saludo_alba", "Alba"));

            Assert.IsInstanceOf<DialogueStep.Line>(step);
            Assert.AreEqual("Alba", ((DialogueStep.Line)step).Value.SpeakerId);
        }

        [Test]
        public void UnPasoOptions_RechazaListasVacias()
        {
            Assert.Throws<System.ArgumentException>(
                () => new DialogueStep.Options(new DialogueOption[0]));
        }

        [Test]
        public void PatternMatching_CubreLasCuatroVariantes()
        {
            DialogueStep[] pasos = {
                new DialogueStep.Line(new DialogueLine("a")),
                new DialogueStep.Options(new[] { new DialogueOption("o1", new DialogueLine("b")) }),
                new DialogueStep.Command("dar_objeto", new[] { "espada" }),
                DialogueStep.Complete.Instance
            };

            foreach (var paso in pasos)
            {
                var nombre = paso switch
                {
                    DialogueStep.Line _     => "line",
                    DialogueStep.Options _  => "options",
                    DialogueStep.Command _  => "command",
                    DialogueStep.Complete _ => "complete",
                    _ => throw new System.InvalidOperationException("Variante no contemplada")
                };
                Assert.IsNotEmpty(nombre);
            }
        }
    }
}