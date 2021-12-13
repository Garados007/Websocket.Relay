using System;
using MaxLib.WebServer;
using MaxLib.WebServer.Services;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Threading.Tasks;

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
            WebServerLog.LogPreAdded += WebServerLog_LogPreAdded;

            var server = new Server(new WebServerSettings(80, 5000));
            server.AddWebService(new HttpRequestParser());
            server.AddWebService(new HttpHeaderSpecialAction());
            server.AddWebService(new Http404Service());
            server.AddWebService(new HttpResponseCreator());
            server.AddWebService(new HttpSender());

            server.Start();

            await Task.Delay(-1);

            server.Stop();
        }
		
        private static readonly MessageTemplate serilogMessageTemplate =
            new Serilog.Parsing.MessageTemplateParser().Parse(
                "{infoType}: {info}"
            );

        private static void WebServerLog_LogPreAdded(ServerLogArgs e)
        {
            e.Discard = true;
            Log.Write(new LogEvent(
                e.LogItem.Date,
                e.LogItem.Type switch
                {
                    ServerLogType.Debug => LogEventLevel.Verbose,
                    ServerLogType.Information => LogEventLevel.Debug,
                    ServerLogType.Error => LogEventLevel.Error,
                    ServerLogType.FatalError => LogEventLevel.Fatal,
                    _ => LogEventLevel.Information,
                },
                null,
                serilogMessageTemplate,
                new[]
                {
                        new LogEventProperty("infoType", new ScalarValue(e.LogItem.InfoType)),
                        new LogEventProperty("info", new ScalarValue(e.LogItem.Information))
                }
            ));
        }
    }
}
