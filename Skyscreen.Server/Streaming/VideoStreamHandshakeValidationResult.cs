// Path: Skyscreen.Server/Streaming/VideoStreamHandshakeValidationResult.cs

using Skyscreen.Core.Models;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Resultat från validering av en video-stream-handshake.
/// </summary>
public sealed record VideoStreamHandshakeValidationResult
{
    /// <summary>
    /// Anger om handshaken är giltig.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Den verifierade panelprenumerationen.
    ///
    /// Är endast satt när IsValid är true.
    /// </summary>
    public PanelSubscription? Subscription { get; init; }

    /// <summary>
    /// Teknisk orsak när valideringen misslyckas.
    ///
    /// Ska användas för serverloggning och diagnostik,
    /// inte som ett säkerhetskänsligt detaljerat svar
    /// till en fjärrklient.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Skapar ett lyckat valideringsresultat.
    /// </summary>
    public static VideoStreamHandshakeValidationResult Success(
        PanelSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        return new VideoStreamHandshakeValidationResult
        {
            IsValid = true,
            Subscription = subscription
        };
    }

    /// <summary>
    /// Skapar ett misslyckat valideringsresultat.
    /// </summary>
    public static VideoStreamHandshakeValidationResult Failure(
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "FailureReason får inte vara tom.",
                nameof(reason));
        }

        return new VideoStreamHandshakeValidationResult
        {
            IsValid = false,
            FailureReason = reason
        };
    }
}