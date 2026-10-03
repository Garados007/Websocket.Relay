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
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
            WebServerLog.SetLoggerFactory(new Serilog.Extensions.Logging.SerilogLoggerFactory(Log.Logger));

            using var server = RelayServer.Create(8005);

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
        }
    }
}
