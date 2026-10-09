// Path: Skyscreen.Core.Tests/JsonVideoStreamHandshakeSerializerTests.cs

using System.Text.Json;
using Skyscreen.Core.Protocol;

namespace Skyscreen.Core.Tests;

/// <summary>
/// Tester för JSON-serialisering av video-stream-handshaken.
/// </summary>
public sealed class JsonVideoStreamHandshakeSerializerTests
{
    [Fact]
    public void Roundtrip_bevarar_all_handshake_data()
    {
        JsonVideoStreamHandshakeSerializer serializer = new();

        VideoStreamHandshake original = new()
        {
            ProtocolVersion = 1,
            ClientId = "tablet-1",
            SubscriptionId = "subscription-1",
            ModuleId = "fa18c",
            PanelId = "left-ddi"
        };

        string json =
            serializer.Serialize(original);

        VideoStreamHandshake result =
            serializer.Deserialize(json);

        Assert.Equal(
            original.ProtocolVersion,
            result.ProtocolVersion);

        Assert.Equal(
            original.ClientId,
            result.ClientId);

        Assert.Equal(
            original.SubscriptionId,
            result.SubscriptionId);

        Assert.Equal(
            original.ModuleId,
            result.ModuleId);

        Assert.Equal(
            original.PanelId,
            result.PanelId);
    }

    [Fact]
    public void Serialize_anvander_camelCase()
    {
        JsonVideoStreamHandshakeSerializer serializer = new();

        VideoStreamHandshake handshake = new()
        {
            ClientId = "tablet-1",
            SubscriptionId = "subscription-1",
            ModuleId = "fa18c",
            PanelId = "left-ddi"
        };

        string json =
            serializer.Serialize(handshake);

        using JsonDocument document =
            JsonDocument.Parse(json);

        JsonElement root =
            document.RootElement;

        Assert.True(
            root.TryGetProperty(
                "protocolVersion",
                out _));

        Assert.True(
            root.TryGetProperty(
                "clientId",
                out _));

        Assert.True(
            root.TryGetProperty(
                "subscriptionId",
                out _));

        Assert.True(
            root.TryGetProperty(
                "moduleId",
                out _));

        Assert.True(
            root.TryGetProperty(
                "panelId",
                out _));
    }

    [Fact]
    public void Deserialize_accepterar_skiftlage_oberoende_egenskapsnamn()
    {
        JsonVideoStreamHandshakeSerializer serializer = new();

        const string json = """
        {
          "ProtocolVersion": 1,
          "CLIENTID": "tablet-1",
          "SubscriptionId": "subscription-1",
          "MODULEID": "fa18c",
          "PanelId": "left-ddi"
        }
        """;

        VideoStreamHandshake result =
            serializer.Deserialize(json);

        Assert.Equal(
            1,
            result.ProtocolVersion);

        Assert.Equal(
            "tablet-1",
            result.ClientId);

        Assert.Equal(
            "subscription-1",
            result.SubscriptionId);

        Assert.Equal(
            "fa18c",
            result.ModuleId);

        Assert.Equal(
            "left-ddi",
            result.PanelId);
    }

    [Fact]
    public void Tom_data_ger_fel()
    {
        JsonVideoStreamHandshakeSerializer serializer = new();

        Assert.Throws<ArgumentException>(
            () => serializer.Deserialize(
                string.Empty));
    }

    [Fact]
    public void Ogiltig_json_ger_fel()
    {
        JsonVideoStreamHandshakeSerializer serializer = new();

        const string json = """
        {
          "protocolVersion": 1,
          "clientId":
        }
        """;

        Assert.Throws<JsonException>(
            () => serializer.Deserialize(
                json));
    }
}