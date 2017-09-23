using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;

namespace Sample
{
    public class NumbersService : BackgroundService
    {
        public NumbersService(IHubContext<Numbers> hubContext)
        {
            Clients = hubContext.Clients;
        }

        public IHubClients Clients { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int n = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                await Clients.All.InvokeAsync("Send", n++);

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
