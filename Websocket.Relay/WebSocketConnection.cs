using System;
using System.IO;
using System.Threading.Tasks;
using MaxLib.WebServer.WebSocket;

namespace Websocket.Relay
{
    public class WebSocketConnection : EventConnection
    {
        public ChannelGroup Group { get; }

        public DateTime? LastSent { get; private set; }

        public WebSocketConnection(ChannelGroup group, Stream networkStream, EventFactory factory)
            : base(networkStream, factory)
        {
            Group = group;
            Closed += (_, _) =>
            {
                group.RemoveConnection(this);
            };
            group.AddConnection(this);
        }

        protected override Task ReceiveClose(CloseReason? reason, string? info)
        {
            return Task.CompletedTask;
        }

        protected override Task ReceivedFrame(EventBase @event)
        {
            _ = Task.Run(async () => 
            {
                switch (@event)
                {
                    case Events.Relay relay:
                        if (relay.Token is null || relay.Token != Group.Token)
                            break; // discard this message
                        var now = DateTime.UtcNow;
                        if (LastSent is not null && now - LastSent.Value < TimeSpan.FromMilliseconds(50))
                            break;
                        LastSent = now;
                        await Group.Send(relay).ConfigureAwait(false);
                        break;
                }
            });
            return Task.CompletedTask;
        }

        public async Task Send<T>(T @event)
            where T : EventBase
        {
            await SendFrame(@event).ConfigureAwait(false);
        }
    }
}
