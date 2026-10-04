// Path: Skyscreen.Core.Tests/JsonSkyscreenMessageSerializerTests.cs

using Skyscreen.Core.Protocol;

namespace Skyscreen.Core.Tests;

/// <summary>
/// Tester för JSON-serialisering av Skyscreens protokollmeddelanden.
/// </summary>
public sealed class JsonSkyscreenMessageSerializerTests
{
    [Fact]
    public void ConnectClient_roundtrip_bevarar_typ_och_data()
    {
        JsonSkyscreenMessageSerializer serializer = new();

        ConnectClientMessage original = new()
        {
            ClientId = "tablet-1",
            ClientName = "Vänster platta"
        };

        string json = serializer.Serialize(original);

        SkyscreenMessage result = serializer.Deserialize(json);

        ConnectClientMessage typedResult =
            Assert.IsType<ConnectClientMessage>(result);

        Assert.Equal(original.ProtocolVersion, typedResult.ProtocolVersion);
        Assert.Equal(original.MessageType, typedResult.MessageType);
        Assert.Equal(original.ClientId, typedResult.ClientId);
        Assert.Equal(original.ClientName, typedResult.ClientName);
    }

    [Fact]
    public void SubscribePanel_roundtrip_bevarar_prenumerationsdata()
    {
        JsonSkyscreenMessageSerializer serializer = new();

        SubscribePanelMessage original = new()
        {
            SubscriptionId = "subscription-1",
            ClientId = "tablet-1",
            ModuleId = "fa18c",
            PanelId = "left-ddi",
            ReceiveVideo = true,
            EnableInput = true
        };

        string json = serializer.Serialize(original);

        SkyscreenMessage result = serializer.Deserialize(json);

        SubscribePanelMessage typedResult =
            Assert.IsType<SubscribePanelMessage>(result);

        Assert.Equal(original.SubscriptionId, typedResult.SubscriptionId);
        Assert.Equal(original.ClientId, typedResult.ClientId);
        Assert.Equal(original.ModuleId, typedResult.ModuleId);
        Assert.Equal(original.PanelId, typedResult.PanelId);
        Assert.Equal(original.ReceiveVideo, typedResult.ReceiveVideo);
        Assert.Equal(original.EnableInput, typedResult.EnableInput);
    }

    [Fact]
    public void Heartbeat_roundtrip_bevarar_typ_och_protokollversion()
    {
        JsonSkyscreenMessageSerializer serializer = new();

        HeartbeatMessage original = new();

        string json = serializer.Serialize(original);

        SkyscreenMessage result = serializer.Deserialize(json);

        HeartbeatMessage typedResult =
            Assert.IsType<HeartbeatMessage>(result);

        Assert.Equal(original.ProtocolVersion, typedResult.ProtocolVersion);
        Assert.Equal(original.MessageType, typedResult.MessageType);
    }

    [Fact]
    public void Okand_messageType_ger_fel()
    {
        JsonSkyscreenMessageSerializer serializer = new();

        const string json = """
        {
          "protocolVersion": 1,
          "messageType": "UnknownMessage"
        }
        """;

        Assert.Throws<System.Text.Json.JsonException>(
            () => serializer.Deserialize(json));
    }
}