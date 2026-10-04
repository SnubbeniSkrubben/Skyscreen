// Path: Skyscreen.App/Services/SkyscreenClientService.cs

using Skyscreen.App.Transport;
using Skyscreen.Core.Protocol;

namespace Skyscreen.App.Services;

/// <summary>
/// Hanterar Skyscreen.Apps logiska anslutning till Skyscreen.Server.
///
/// Tjänsten använder ett transportoberoende anslutningslager och
/// registrerar klienten med ConnectClient när transportanslutningen
/// har etablerats.
/// </summary>
public sealed class SkyscreenClientService : ISkyscreenClientService
{
    private readonly IClientConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IClientConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Skapar klientservicen.
    /// </summary>
    public SkyscreenClientService(
        IClientConnectionFactory connectionFactory,
        IClientIdentityProvider clientIdentityProvider)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(clientIdentityProvider);

        _connectionFactory = connectionFactory;
        ClientId = clientIdentityProvider.GetOrCreateClientId();
    }

    /// <inheritdoc />
    public bool IsConnected =>
        _connection?.IsConnected == true;

    /// <inheritdoc />
    public string ClientId { get; }

    /// <inheritdoc />
    public async Task ConnectAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        await _connectionLock.WaitAsync(cancellationToken);

        try
        {
            if (_connection?.IsConnected == true)
            {
                return;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            IClientConnection connection =
                await _connectionFactory.ConnectAsync(
                    endpoint,
                    cancellationToken);

            try
            {
                await connection.SendAsync(
                    new ConnectClientMessage
                    {
                        ClientId = ClientId
                    },
                    cancellationToken);

                _connection = connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync()
    {
        await _connectionLock.WaitAsync();

        try
        {
            if (_connection is null)
            {
                return;
            }

            await _connection.DisposeAsync();
            _connection = null;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await DisconnectAsync();

        _connectionLock.Dispose();
    }
}