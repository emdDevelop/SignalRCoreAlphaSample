using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Sample
{
    public class Chat : Hub
    {
        public override Task OnConnectedAsync()
        {
            return Clients.All.InvokeAsync("Send", $"{Context.ConnectionId} joined");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return Clients.All.InvokeAsync("Send", $"{Context.ConnectionId} left");
        }

        public Task Send(string message)
        {
            return Clients.All.InvokeAsync("Send", message);
        }
    }
}
