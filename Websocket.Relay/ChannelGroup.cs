using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Websocket.Relay
{
    public class ChannelGroup
    {
        public string Id { get; }

        public string Token { get; }

        public ChannelGroup(string id, string token)
        {
            Id = id;
            Token = token;
        }

        public static ConcurrentDictionary<string, ChannelGroup> Channels { get; }
            = new ConcurrentDictionary<string, ChannelGroup>();
        
        public static ChannelGroup AddGroup()
        {
            var rng = new Random();
            Span<byte> buffer = stackalloc byte[16];
            while (true)
            {
                rng.NextBytes(buffer);
                var id = Convert.ToBase64String(buffer);
                rng.NextBytes(buffer);
                var token = Convert.ToBase64String(buffer);
                var channel = new ChannelGroup(id, token);
                if (!Channels.TryAdd(id, channel))
                    continue;
                return channel;
            }
        }

        private readonly List<WebSocketConnection> connections = new List<WebSocketConnection>();
        private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

        public void AddConnection(WebSocketConnection connection)
        {
            try
            {
                @lock.EnterWriteLock();
                connections.Add(connection);
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }

        public void RemoveConnection(WebSocketConnection connection)
        {
            try
            {
                @lock.EnterWriteLock();
                connections.Remove(connection);
                if (connections.Count == 0)
                {
                    Channels.Remove(Id, out _);
                }
            }
            finally
            {
                @lock.ExitWriteLock();
            }
        }

        public async Task Send(MaxLib.WebServer.WebSocket.EventBase @event)
        {
            Task task;
            try
            {
                @lock.EnterReadLock();
                task = Task.WhenAll(connections.Select(x => x.Send(@event)));
            }
            finally
            {
                @lock.ExitReadLock();
            }
            await task.ConfigureAwait(false);
        }
    }
}