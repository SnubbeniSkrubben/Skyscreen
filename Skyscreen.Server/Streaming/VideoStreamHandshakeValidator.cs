// Path: Skyscreen.Server/Streaming/VideoStreamHandshakeValidator.cs

using Skyscreen.Core.Models;
using Skyscreen.Core.Protocol;
using Skyscreen.Server.Services;

namespace Skyscreen.Server.Streaming;

/// <summary>
/// Validerar att en video-stream-handshake motsvarar
/// en redan registrerad och aktiv panelprenumeration.
/// </summary>
public sealed class VideoStreamHandshakeValidator
{
    private readonly ClientSessionManager _clientSessionManager;

    public VideoStreamHandshakeValidator(
        ClientSessionManager clientSessionManager)
    {
        ArgumentNullException.ThrowIfNull(
            clientSessionManager);

        _clientSessionManager =
            clientSessionManager;
    }

    /// <summary>
    /// Validerar en inkommande video-stream-handshake
    /// mot serverns aktuella klient- och prenumerationsstate.
    /// </summary>
    public VideoStreamHandshakeValidationResult Validate(
        VideoStreamHandshake handshake)
    {
        ArgumentNullException.ThrowIfNull(handshake);

        if (handshake.ProtocolVersion !=
            SkyscreenMessage.CurrentProtocolVersion)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                $"Protokollversion {handshake.ProtocolVersion} stöds inte.");
        }

        if (string.IsNullOrWhiteSpace(
            handshake.ClientId))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "ClientId saknas.");
        }

        if (string.IsNullOrWhiteSpace(
            handshake.SubscriptionId))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "SubscriptionId saknas.");
        }

        if (string.IsNullOrWhiteSpace(
            handshake.ModuleId))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "ModuleId saknas.");
        }

        if (string.IsNullOrWhiteSpace(
            handshake.PanelId))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "PanelId saknas.");
        }

        string clientId =
            handshake.ClientId.Trim();

        string subscriptionId =
            handshake.SubscriptionId.Trim();

        string moduleId =
            handshake.ModuleId.Trim();

        string panelId =
            handshake.PanelId.Trim();

        ClientSession? session =
            _clientSessionManager.FindByClientId(
                clientId);

        if (session is null)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Ingen registrerad klientsession hittades.");
        }

        if (!session.IsConnected)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Klientsessionen är inte ansluten.");
        }

        PanelSubscription? subscription =
            session.Subscriptions.FirstOrDefault(
                item =>
                    string.Equals(
                        item.SubscriptionId,
                        subscriptionId,
                        StringComparison.OrdinalIgnoreCase));

        if (subscription is null)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Panelprenumerationen hittades inte.");
        }

        if (!subscription.IsActive)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Panelprenumerationen är inte aktiv.");
        }

        if (!subscription.ReceiveVideo)
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Panelprenumerationen tillåter inte video.");
        }

        if (!string.Equals(
                subscription.ClientId,
                clientId,
                StringComparison.OrdinalIgnoreCase))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Prenumerationens ClientId matchar inte handshaken.");
        }

        if (!string.Equals(
                subscription.ModuleId,
                moduleId,
                StringComparison.OrdinalIgnoreCase))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Prenumerationens ModuleId matchar inte handshaken.");
        }

        if (!string.Equals(
                subscription.PanelId,
                panelId,
                StringComparison.OrdinalIgnoreCase))
        {
            return VideoStreamHandshakeValidationResult.Failure(
                "Prenumerationens PanelId matchar inte handshaken.");
        }

        return VideoStreamHandshakeValidationResult.Success(
            subscription);
    }
}