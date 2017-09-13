using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Sockets;

namespace SampleConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = new HubConnectionBuilder()
                            .WithUrl("http://localhost:5000/chat")
                            .WithMessagePackProtocol()
                            .WithConsoleLogger()
                            .WithTransport(TransportType.WebSockets)
                            .Build();

            await connection.StartAsync();
            var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            connection.Closed += e =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            };

            connection.On<string>("Send", data =>
            {
                Console.WriteLine($"Server: {data}");
            });

            var cancelledTask = Task.Delay(-1, cts.Token);

            while (!cts.IsCancellationRequested)
            {
                Console.Write("Client: ");
                var task = await Task.WhenAny(cancelledTask, Task.Run(() => Console.ReadLine()));
                if (task is Task<string> readline)
                {
                    var line = await readline;

                    await connection.InvokeAsync("Send", line);
                }
                else
                {
                    break;
                }
            }

            await connection.DisposeAsync();

            cts.Dispose();
        }
    }
}
