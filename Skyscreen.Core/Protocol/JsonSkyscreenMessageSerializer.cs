// Path: Skyscreen.Core/Protocol/JsonSkyscreenMessageSerializer.cs

using System.Text.Json;

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Serialiserar och deserialiserar Skyscreens logiska
/// protokollmeddelanden som JSON.
/// </summary>
public sealed class JsonSkyscreenMessageSerializer
    : ISkyscreenMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public string Serialize(SkyscreenMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return JsonSerializer.Serialize(
            message,
            message.GetType(),
            SerializerOptions);
    }

    /// <inheritdoc />
    public SkyscreenMessage Deserialize(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException(
                "Meddelandedata får inte vara tom.",
                nameof(data));
        }

        using JsonDocument document = JsonDocument.Parse(data);

        if (!document.RootElement.TryGetProperty(
                "messageType",
                out JsonElement messageTypeElement))
        {
            throw new JsonException(
                "Meddelandet saknar obligatoriskt fält 'messageType'.");
        }

        string? messageType = messageTypeElement.GetString();

        Type messageClrType = messageType switch
        {
            "ConnectClient" => typeof(ConnectClientMessage),
            "SubscribePanel" => typeof(SubscribePanelMessage),
            "UnsubscribePanel" => typeof(UnsubscribePanelMessage),
            "PanelInput" => typeof(PanelInputMessage),
            "Heartbeat" => typeof(HeartbeatMessage),
            "ServerStatus" => typeof(ServerStatusMessage),

            _ => throw new JsonException(
                $"Okänd Skyscreen-meddelandetyp: '{messageType}'.")
        };

        object? result = JsonSerializer.Deserialize(
            data,
            messageClrType,
            SerializerOptions);

        return result as SkyscreenMessage
            ?? throw new JsonException(
                $"Kunde inte deserialisera meddelandetypen '{messageType}'.");
    }
}