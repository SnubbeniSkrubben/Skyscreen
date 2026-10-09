// Path: Skyscreen.Core/Protocol/JsonVideoStreamHandshakeSerializer.cs

using System.Text.Json;

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Serialiserar och deserialiserar handshaken som används
/// när en separat videoanslutning etableras.
///
/// JSON-konventionerna är samma som för Skyscreens
/// övriga protokollmeddelanden.
/// </summary>
public sealed class JsonVideoStreamHandshakeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            PropertyNameCaseInsensitive =
                true
        };

    /// <summary>
    /// Serialiserar en video-stream-handshake till JSON.
    /// </summary>
    public string Serialize(
        VideoStreamHandshake handshake)
    {
        ArgumentNullException.ThrowIfNull(
            handshake);

        return JsonSerializer.Serialize(
            handshake,
            SerializerOptions);
    }

    /// <summary>
    /// Deserialiserar JSON till en video-stream-handshake.
    /// </summary>
    public VideoStreamHandshake Deserialize(
        string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new ArgumentException(
                "Handshake-data får inte vara tom.",
                nameof(data));
        }

        VideoStreamHandshake? handshake =
            JsonSerializer.Deserialize<VideoStreamHandshake>(
                data,
                SerializerOptions);

        return handshake
            ?? throw new JsonException(
                "Kunde inte deserialisera video-stream-handshaken.");
    }
}