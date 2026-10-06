// Path: Skyscreen.App/Services/UdpServerDiscoveryClient.cs

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Skyscreen.Core.Discovery;

namespace Skyscreen.App.Services;

/// <summary>
/// Söker efter Skyscreen.Server via UDP broadcast på det lokala nätverket.
/// </summary>
public sealed class UdpServerDiscoveryClient
    : IServerDiscoveryClient
{
    private static readonly TimeSpan DiscoveryTimeout =
        TimeSpan.FromSeconds(2);

    private readonly IServerDiscoveryMessageSerializer _serializer;

    /// <summary>
    /// Skapar discovery-klienten.
    /// </summary>
    public UdpServerDiscoveryClient(
        IServerDiscoveryMessageSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        _serializer = serializer;
    }

    /// <inheritdoc />
    public async Task<Uri?> DiscoverAsync(
        CancellationToken cancellationToken)
    {
        string requestId =
            Guid.NewGuid().ToString("N");

        ServerDiscoveryRequest request = new()
        {
            RequestId = requestId
        };

        string requestData =
            _serializer.SerializeRequest(
                request);

        byte[] requestBytes =
            Encoding.UTF8.GetBytes(
                requestData);

        using UdpClient udpClient = new();

        udpClient.EnableBroadcast = true;

        IPEndPoint broadcastEndpoint =
            new(
                IPAddress.Broadcast,
                SkyscreenDiscoveryProtocol.DiscoveryPort);

        await udpClient.SendAsync(
            requestBytes,
            broadcastEndpoint,
            cancellationToken);

        using CancellationTokenSource timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

        timeoutCancellation.CancelAfter(
            DiscoveryTimeout);

        while (!timeoutCancellation.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult received =
                    await udpClient.ReceiveAsync(
                        timeoutCancellation.Token);

                string responseData =
                    Encoding.UTF8.GetString(
                        received.Buffer);

                ServerDiscoveryResponse response =
                    _serializer.DeserializeResponse(
                        responseData);

                if (!string.Equals(
                        response.RequestId,
                        requestId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                IPAddress serverAddress =
                    received.RemoteEndPoint.Address;

                UriBuilder endpointBuilder = new()
                {
                    Scheme = "ws",
                    Host = serverAddress.ToString(),
                    Port = response.WebSocketPort,
                    Path = response.WebSocketPath
                };

                return endpointBuilder.Uri;
            }
            catch (OperationCanceledException)
                when (timeoutCancellation.IsCancellationRequested)
            {
                return null;
            }
            catch (JsonException)
            {
                // Ett främmande eller trasigt UDP-paket på samma port
                // ska inte avbryta det pågående discovery-försöket.
            }
            catch (SocketException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }

        return null;
    }
}