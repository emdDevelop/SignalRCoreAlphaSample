## Basic chat for ASP.NET Core SignalR alpha

## Server 

```C#
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Sample
{
    public class Chat : Hub
    {
        public Task Send(string message)
        {
            return Clients.All.InvokeAsync("Send", message);
        }
    }
}
```

```C#
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sample
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseSignalR(routes =>
            {
                routes.MapHub<Chat>("chat");
            });
        }
    }
}

```

## Client

### Javascript

```javascript
let connection = new signalR.HubConnection('/chat');

connection.on('send', data => {
    console.log(data);
});

connection.start()
    .then(() => connection.invoke('send', 'Hello'));
```

### C#

```C#
var connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/chat")
                .WithConsoleLogger()
                .Build();

connection.On<string>("Send", data =>
{
    Console.WriteLine($"Received: {data}");
});

await connection.StartAsync();

await connection.InvokeAsync("Send", "Hello");

await connection.DisposeAsync();
```
