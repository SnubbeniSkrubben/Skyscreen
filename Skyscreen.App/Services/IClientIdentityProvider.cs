// Path: Skyscreen.App/Services/IClientIdentityProvider.cs

namespace Skyscreen.App.Services;

/// <summary>
/// Tillhandahåller Skyscreen-klientens stabila identitet.
///
/// ClientId ska återanvändas mellan anslutningar och appstarter så
/// att servern kan känna igen klienten vid återanslutning.
/// </summary>
public interface IClientIdentityProvider
{
    /// <summary>
    /// Hämtar eller skapar klientens stabila ClientId.
    /// </summary>
    string GetOrCreateClientId();
}