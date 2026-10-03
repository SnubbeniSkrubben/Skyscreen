// Path: Skyscreen.Core/Protocol/ISkyscreenMessageSerializer.cs

namespace Skyscreen.Core.Protocol;

/// <summary>
/// Abstraktion för serialisering och deserialisering av
/// Skyscreens logiska protokollmeddelanden.
///
/// Serialiseringsformatet ska vara oberoende av om meddelandet
/// transporteras via Wi-Fi, USB eller annan transport.
/// </summary>
public interface ISkyscreenMessageSerializer
{
    /// <summary>
    /// Serialiserar ett logiskt Skyscreen-meddelande till data
    /// som kan skickas över transportlagret.
    /// </summary>
    string Serialize(SkyscreenMessage message);

    /// <summary>
    /// Deserialiserar data till rätt konkret Skyscreen-meddelandetyp.
    /// </summary>
    SkyscreenMessage Deserialize(string data);
}