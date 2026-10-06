// Path: Skyscreen.Core/Discovery/JsonServerDiscoveryMessageSerializer.cs

using System.Text.Json;

namespace Skyscreen.Core.Discovery;

/// <summary>
/// Serialiserar och deserialiserar Skyscreens UDP-baserade
/// discovery-meddelanden som JSON.
/// </summary>
public sealed class JsonServerDiscoveryMessageSerializer
    : IServerDiscoveryMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public string SerializeRequest(
        ServerDiscoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return JsonSerializer.Serialize(
            request,
            SerializerOptions);
    }

    /// <inheritdoc />
    public ServerDiscoveryRequest DeserializeRequest(
        string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException(
                "Discovery-data får inte vara tom.",
                nameof(data));
        }

        ServerDiscoveryRequest? request =
            JsonSerializer.Deserialize<ServerDiscoveryRequest>(
                data,
                SerializerOptions);

        if (request is null)
        {
            throw new JsonException(
                "Discovery-anropet kunde inte deserialiseras.");
        }

        if (request.ProtocolVersion !=
            SkyscreenDiscoveryProtocol.CurrentProtocolVersion)
        {
            throw new JsonException(
                $"Discovery-protokollversion {request.ProtocolVersion} stöds inte.");
        }

        if (!string.Equals(
                request.MessageType,
                SkyscreenDiscoveryProtocol.RequestMessageType,
                StringComparison.Ordinal))
        {
            throw new JsonException(
                $"Ogiltig discovery-meddelandetyp: '{request.MessageType}'.");
        }

        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            throw new JsonException(
                "Discovery-anropet saknar RequestId.");
        }

        return request;
    }

    /// <inheritdoc />
    public string SerializeResponse(
        ServerDiscoveryResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return JsonSerializer.Serialize(
            response,
            SerializerOptions);
    }

    /// <inheritdoc />
    public ServerDiscoveryResponse DeserializeResponse(
        string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException(
                "Discovery-data får inte vara tom.",
                nameof(data));
        }

        ServerDiscoveryResponse? response =
            JsonSerializer.Deserialize<ServerDiscoveryResponse>(
                data,
                SerializerOptions);

        if (response is null)
        {
            throw new JsonException(
                "Discovery-svaret kunde inte deserialiseras.");
        }

        if (response.ProtocolVersion !=
            SkyscreenDiscoveryProtocol.CurrentProtocolVersion)
        {
            throw new JsonException(
                $"Discovery-protokollversion {response.ProtocolVersion} stöds inte.");
        }

        if (!string.Equals(
                response.MessageType,
                SkyscreenDiscoveryProtocol.ResponseMessageType,
                StringComparison.Ordinal))
        {
            throw new JsonException(
                $"Ogiltig discovery-meddelandetyp: '{response.MessageType}'.");
        }

        if (string.IsNullOrWhiteSpace(response.RequestId))
        {
            throw new JsonException(
                "Discovery-svaret saknar RequestId.");
        }

        if (response.WebSocketPort is < 1 or > 65535)
        {
            throw new JsonException(
                "Discovery-svaret innehåller en ogiltig WebSocket-port.");
        }

        if (string.IsNullOrWhiteSpace(response.WebSocketPath) ||
            !response.WebSocketPath.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            throw new JsonException(
                "Discovery-svaret innehåller en ogiltig WebSocket-sökväg.");
        }

        return response;
    }
}