using System.Text.Json.Nodes;

namespace WhatsApp.Core.Tests.Helpers;

internal static class JsonTestHelpers
{
    public static void AssertJsonEqual(string expectedJson, JsonNode actual)
    {
        var expected = JsonNode.Parse(expectedJson) ?? throw new InvalidOperationException("Expected JSON was null.");
        Assert.True(JsonNode.DeepEquals(expected, actual), $"Expected:\n{expected}\nActual:\n{actual}");
    }

    public static string NormalizeJson(JsonNode node) => node.ToJsonString();
}
