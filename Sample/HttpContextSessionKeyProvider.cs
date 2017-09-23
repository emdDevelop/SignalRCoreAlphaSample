using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Sample
{
    public class HttpContextSessionKeyProvider : ISessionKeyProvider
    {
        public string GetSessionKey(HubConnectionContext hubConnection)
        {
            return hubConnection.GetHttpContext().Request.Query["session"];
        }
    }
}
