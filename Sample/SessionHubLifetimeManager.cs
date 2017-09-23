using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Options;

namespace Sample
{
    public class SessionHubLifetimeManager<THub> : DefaultHubLifetimeManager<THub>
    {
        // Sessions expire after a minute of idle time
        private readonly TimeSpan _sessionTimeout;
        private readonly int _bufferSize;
        private ConcurrentDictionary<string, Session> _sessions = new ConcurrentDictionary<string, Session>();

        private readonly ISessionKeyProvider _sessionKeyProvider;

        public SessionHubLifetimeManager(ISessionKeyProvider sessionKeyProvider, IOptions<SessionOptions> options)
        {
            _sessionKeyProvider = sessionKeyProvider;
            _sessionTimeout = options.Value.SessionTimeout;
            _bufferSize = options.Value.MaxBufferSize;
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            await base.OnConnectedAsync(connection);

            var sessionId = _sessionKeyProvider.GetSessionKey(connection);

            // We remove the session if it exists since we're going to reply all of the state
            // up front, we can discard the session
            if (_sessions.TryRemove(sessionId, out var session))
            {
                session.ConnectionId = null;
                session.CancellationTokenSource.Dispose();
                session.CancellationTokenSource = null;
                session.Registration.Dispose();

                // Re-join groups
                if (session.Groups != null)
                {
                    foreach (var g in session.Groups)
                    {
                        await AddGroupAsync(connection.ConnectionId, g);
                    }
                }

                // Replay missed hub messages here
                while (session.Messages.Count > 0)
                {
                    var msg = session.Messages.Dequeue() as InvocationMessage;
                    // await WriteAsync(connection, msg);
                    await InvokeConnectionAsync(connection.ConnectionId, msg.Target, msg.Arguments);
                }
            }
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            await base.OnDisconnectedAsync(connection);

            var sessionId = _sessionKeyProvider.GetSessionKey(connection);
            // var groups = connection.Features.Get<IHubGroupsFeature>();

            // Get or create a new session for this connection
            var session = _sessions.GetOrAdd(sessionId, s => new Session());

            // Store the previous connection id so that any messages addressed to this connection
            // between now and the timeout are stored
            session.ConnectionId = connection.ConnectionId;

            // Store the previous groups so that they are restored on reconnect
            // session.Groups = groups.Groups;

            // Store the previous user id
            session.UserId = connection.User.Identity.Name;

            // Now setup a timeout that will auto remove the session unless a connection comes back
            // within that timeout
            // Now setup a timeout that will auto remove the session unless a connection comes back
            // within that timeout
            session.CancellationTokenSource = new CancellationTokenSource(_sessionTimeout);
            session.Registration = session.CancellationTokenSource.Token.Register(() =>
            {
                _sessions.TryRemove(sessionId, out _);
            });
        }

        public override Task InvokeGroupAsync(string groupName, string methodName, object[] args)
        {
            foreach (var pair in _sessions)
            {
                var session = pair.Value;
                if (session.ConnectionId == null)
                {
                    continue;
                }

                if (session.Groups.Contains(groupName))
                {
                    AddInvocationMessage(session, methodName, args);
                }
            }

            return base.InvokeGroupAsync(groupName, methodName, args);
        }

        private string GetInvocationId()
        {
            return "0";
        }

        public override Task InvokeConnectionAsync(string connectionId, string methodName, object[] args)
        {
            foreach (var pair in _sessions)
            {
                var session = pair.Value;

                if (session.ConnectionId == connectionId)
                {
                    AddInvocationMessage(session, methodName, args);
                }
            }

            return base.InvokeConnectionAsync(connectionId, methodName, args);
        }

        public override Task InvokeUserAsync(string userId, string methodName, object[] args)
        {
            foreach (var pair in _sessions)
            {
                var session = pair.Value;

                if (string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    AddInvocationMessage(session, methodName, args);
                }
            }

            return base.InvokeUserAsync(userId, methodName, args);
        }

        public override Task InvokeAllAsync(string methodName, object[] args)
        {
            foreach (var pair in _sessions)
            {
                var session = pair.Value;

                if (session.ConnectionId == null)
                {
                    continue;
                }

                AddInvocationMessage(session, methodName, args);
            }

            return base.InvokeAllAsync(methodName, args);
        }

        private void AddInvocationMessage(Session session, string methodName, object[] args)
        {
            lock (session.Messages)
            {
                var message = new InvocationMessage(GetInvocationId(), nonBlocking: true, target: methodName, arguments: args);
                session.Messages.Enqueue(message);

                if (session.Messages.Count > _bufferSize)
                {
                    session.Messages.Dequeue();
                }
            }
        }

        private class Session
        {
            public CancellationTokenRegistration Registration { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }

            public string ConnectionId { get; set; }
            public string UserId { get; set; }
            public Queue<HubMessage> Messages { get; } = new Queue<HubMessage>();
            public HashSet<string> Groups { get; set; }
        }
    }
}
