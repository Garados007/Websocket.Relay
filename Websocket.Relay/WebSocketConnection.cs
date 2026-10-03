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

        protected override async Task ReceiveClose(CloseReason? reason, string? info)
        {
            // Without a close reply the ping loop never ends and the group is never cleaned up.
            try
            {
                await Close(reason ?? CloseReason.NormalClose).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Serilog.Log.Debug(ex, "Cannot reply to close frame");
            }
        }

        protected override Task ReceivedFrame(EventBase @event)
        {
            // Checked before leaving the receive loop, so the rate limit cannot race between messages.
            if (@event is not Events.Relay relay
                || relay.Token is null
                || relay.Token != Group.Token)
                return Task.CompletedTask; // discard this message
            var now = DateTime.UtcNow;
            if (LastSent is not null && now - LastSent.Value < TimeSpan.FromMilliseconds(50))
                return Task.CompletedTask;
            LastSent = now;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Group.Send(new Events.Relay { Value = relay.Value }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Debug(ex, "Cannot relay message");
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
