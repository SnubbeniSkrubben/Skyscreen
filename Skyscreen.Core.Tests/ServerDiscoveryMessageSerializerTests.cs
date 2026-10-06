// Path: Skyscreen.Core.Tests/ServerDiscoveryMessageSerializerTests.cs

using System.Text.Json;
using Skyscreen.Core.Discovery;

namespace Skyscreen.Core.Tests;

public sealed class ServerDiscoveryMessageSerializerTests
{
    private readonly IServerDiscoveryMessageSerializer _serializer =
        new JsonServerDiscoveryMessageSerializer();

    [Fact]
    public void Request_roundtrip_bevarar_data()
    {
        ServerDiscoveryRequest original = new()
        {
            RequestId = "request-123"
        };

        string json =
            _serializer.SerializeRequest(original);

        ServerDiscoveryRequest result =
            _serializer.DeserializeRequest(json);

        Assert.Equal(
            SkyscreenDiscoveryProtocol.CurrentProtocolVersion,
            result.ProtocolVersion);

        Assert.Equal(
            SkyscreenDiscoveryProtocol.RequestMessageType,
            result.MessageType);

        Assert.Equal(
            original.RequestId,
            result.RequestId);
    }

    [Fact]
    public void Response_roundtrip_bevarar_data()
    {
        ServerDiscoveryResponse original = new()
        {
            RequestId = "request-123",
            WebSocketPort = 5080,
            WebSocketPath = "/ws"
        };

        string json =
            _serializer.SerializeResponse(original);

        ServerDiscoveryResponse result =
            _serializer.DeserializeResponse(json);

        Assert.Equal(
            SkyscreenDiscoveryProtocol.CurrentProtocolVersion,
            result.ProtocolVersion);

        Assert.Equal(
            SkyscreenDiscoveryProtocol.ResponseMessageType,
            result.MessageType);

        Assert.Equal(
            original.RequestId,
            result.RequestId);

        Assert.Equal(
            original.WebSocketPort,
            result.WebSocketPort);

        Assert.Equal(
            original.WebSocketPath,
            result.WebSocketPath);
    }

    [Fact]
    public void Request_med_fel_protokollversion_avvisas()
    {
        string json =
            """
            {
              "protocolVersion": 999,
              "messageType": "ServerDiscoveryRequest",
              "requestId": "request-123"
            }
            """;

        Assert.Throws<JsonException>(
            () => _serializer.DeserializeRequest(json));
    }

    [Fact]
    public void Response_med_ogiltig_websocket_path_avvisas()
    {
        string json =
            """
            {
              "protocolVersion": 1,
              "messageType": "ServerDiscoveryResponse",
              "requestId": "request-123",
              "webSocketPort": 5080,
              "webSocketPath": "ws"
            }
            """;

        Assert.Throws<JsonException>(
            () => _serializer.DeserializeResponse(json));
    }
}