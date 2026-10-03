// Path: Skyscreen.Server/Worker.cs

using Skyscreen.Server.Services;

namespace Skyscreen.Server;

/// <summary>
/// Bakgrundstjänst för Skyscreen.Server.
/// Den kommer senare att hantera bland annat
/// klientanslutningar, DCS-kommunikation och panelströmmar.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ClientSessionManager _clientSessionManager;

    public Worker(
        ILogger<Worker> logger,
        ClientSessionManager clientSessionManager)
    {
        _logger = logger;
        _clientSessionManager = clientSessionManager;
    }

    /// <summary>
    /// Huvudloopen för Skyscreen.Server.
    /// Just nu verifierar den bara att servern startar
    /// och att ClientSessionManager kan injiceras korrekt.
    /// </summary>
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Skyscreen.Server startad. Aktiva klienter: {ClientCount}",
            _clientSessionManager.GetAll().Count);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal nedstängning av servern.
        }
    }
}