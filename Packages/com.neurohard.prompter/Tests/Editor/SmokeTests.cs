using NUnit.Framework;

namespace Estudio.Prompter.Tests
{
    public class SmokeTests
    {
        [Test]
        public void CoreEsAlcanzableDesdeLosTests()
        {
            Assert.AreEqual("0.1.0", PrompterInfo.Version);
        }
    }
}