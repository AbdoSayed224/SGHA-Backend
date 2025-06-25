using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

public class AiHub : Hub
{
    private readonly ILogger<AiHub> _logger;

    public AiHub(ILogger<AiHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("✅ Client connected to AiHub: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("❌ Client disconnected from AiHub: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
