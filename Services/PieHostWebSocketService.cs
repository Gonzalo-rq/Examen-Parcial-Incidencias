using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace IncidenciasApp.Services;

public interface IPieHostWebSocketService
{
    Task PublicarIncidenciaActualizadaAsync(int id, string estado);
}

public class PieHostWebSocketService : IPieHostWebSocketService
{
    private readonly string _webSocketUrl;
    private readonly ILogger<PieHostWebSocketService> _logger;

    public PieHostWebSocketService(IConfiguration configuration, ILogger<PieHostWebSocketService> logger)
    {
        _logger = logger;
        var clusterId = configuration["PieHost:ClusterId"] ?? "free.blr2";
        var apiKey = configuration["PieHost:ApiKey"] ?? "f2X1bR8OHdxfU5blSBwspk25q3otbhOTtfbHAWMh";
        var channelId = configuration["PieHost:ChannelId"] ?? "1";

        _webSocketUrl = $"wss://{clusterId}.piesocket.com/v3/{channelId}?api_key={apiKey}&notify_self=1";
    }

    public async Task PublicarIncidenciaActualizadaAsync(int id, string estado)
    {
        try
        {
            var eventPayload = JsonSerializer.Serialize(new
            {
                @event = "IncidenciaActualizada",
                data = new
                {
                    Id = id,
                    Estado = estado,
                    Timestamp = DateTime.UtcNow
                }
            });

            using var ws = new ClientWebSocket();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await ws.ConnectAsync(new Uri(_webSocketUrl), cts.Token);
            var buffer = Encoding.UTF8.GetBytes(eventPayload);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);

            _logger.LogInformation("[PIEHOST WEBSOCKET] Evento IncidenciaActualizada publicado con éxito: Id={Id}, Estado={Estado}", id, estado);

            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Event Published", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PIEHOST WEBSOCKET ERROR] Error al publicar evento IncidenciaActualizada para Id={Id}", id);
        }
    }
}
