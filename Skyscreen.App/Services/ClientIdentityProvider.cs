// Path: Skyscreen.App/Services/ClientIdentityProvider.cs

using Microsoft.Maui.Storage;

namespace Skyscreen.App.Services;

/// <summary>
/// Tillhandahåller en stabil klientidentitet för den aktuella
/// Skyscreen-installationen.
///
/// ClientId lagras persistent i appens Preferences och återanvänds
/// därför mellan appstarter och återanslutningar.
/// </summary>
public sealed class ClientIdentityProvider : IClientIdentityProvider
{
    private const string ClientIdPreferenceKey = "Skyscreen.ClientId";

    /// <inheritdoc />
    public string GetOrCreateClientId()
    {
        string? existingClientId =
            Preferences.Default.Get<string?>(
                ClientIdPreferenceKey,
                null);

        if (!string.IsNullOrWhiteSpace(existingClientId))
        {
            return existingClientId;
        }

        string clientId = Guid.NewGuid().ToString("D");

        Preferences.Default.Set(
            ClientIdPreferenceKey,
            clientId);

        return clientId;
    }
}