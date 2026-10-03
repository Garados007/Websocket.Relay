using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MaxLib.WebServer;
using Serilog;
using Serilog.Events;

namespace Websocket.Relay
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            WebServerLog.SetLoggerFactory(new Serilog.Extensions.Logging.SerilogLoggerFactory(Log.Logger));

            long maxMessageSize;
            try
            {
                maxMessageSize = RelayServer.ParseMaxMessageSize(
                    Environment.GetEnvironmentVariable("RELAY_MAX_MESSAGE_SIZE"));
            }
            catch (ArgumentException ex)
            {
                Log.Fatal(ex.Message);
                return 1;
            }

            using var server = RelayServer.Create(8005, maxMessageSize);

            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                if (server.RunToken is not { } token)
                    return;
                // Keep the process alive until RunAsync has stopped the server.
                context.Cancel = true;
                Log.Information("SIGTERM received");
                token.Cancel();
            });

            await server.RunAsync();
            return 0;
        }
    }
}
