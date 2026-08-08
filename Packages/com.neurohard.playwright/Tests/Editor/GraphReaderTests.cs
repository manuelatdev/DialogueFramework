using NUnit.Framework;
using Neurohard.Playwright.Io;

namespace Neurohard.Playwright.Tests
{
    public class GraphReaderTests
    {
        private const string Json = @"{
  ""version"": 1,
  ""start"": ""n_encuentro"",
  ""nodes"": [
    { ""id"": ""n_encuentro"", ""type"": ""line"",
      ""line"": { ""text"": ""¿Quién anda ahí?"", ""speaker"": ""alba"" },
      ""editor"": { ""x"": 0, ""y"": 0 },
      ""out"": [ { ""to"": ""n_menu"" } ] },
    { ""id"": ""n_menu"", ""type"": ""choice"",
      ""out"": [
        { ""to"": ""n_fin"", ""id"": ""paz"", ""line"": { ""text"": ""Vengo en paz."" } },
        { ""to"": ""n_fin"", ""id"": ""pagar"", ""line"": { ""text"": ""Toma 50."" },
          ""when"": { ""var"": ""oro"", ""op"": "">="", ""value"": 50 },
          ""then"": [ { ""var"": ""oro"", ""op"": ""-="", ""value"": 50 },
                      { ""command"": ""abrir_puerta"", ""args"": [ ""norte"" ] } ] }
      ] },
    { ""id"": ""n_fin"", ""type"": ""hub"", ""fallthrough"": ""end"", ""out"": [] }
  ]
}";

        [Test]
        public void CargaElGrafoCompleto()
        {
            var g = GraphReader.FromJson(Json);

            Assert.AreEqual(3, g.Nodes.Count);
            Assert.AreEqual("n_encuentro", g.Start);
            Assert.AreEqual("alba", g.Find("n_encuentro").Line.Speaker);
            Assert.AreEqual(FallthroughMode.End, g.Find("n_fin").Fallthrough);

            var pagar = g.Find("n_menu").Out[1];
            Assert.IsInstanceOf<Condition.Compare>(pagar.When);
            Assert.AreEqual(2, pagar.Then.Count);
            Assert.IsInstanceOf<Effect.Command>(pagar.Then[1]);
        }

        [Test]
        public void ElErrorIncluyeElNumeroDeLinea()
        {
            var roto = "{\n  \"version\": 1,\n  \"nodes\": [\n    { \"id\": \"a\" }\n  ]\n}";

            var ex = Assert.Throws<GraphFormatException>(() => GraphReader.FromJson(roto));

            StringAssert.Contains("línea", ex.Message);
            StringAssert.Contains("type", ex.Message);
        }
    }
}