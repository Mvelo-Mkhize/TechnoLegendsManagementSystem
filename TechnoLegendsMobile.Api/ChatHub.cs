using Microsoft.AspNetCore.SignalR;

namespace TechnoLegendsMobile.Api
{

    // Receive-only for now. Messages are saved by ChannelsController, which pushes through this hub.
    public class ChatHub : Hub { }

    public class SubUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) =>
            connection.User?.FindFirst("sub")?.Value;
    }
}
