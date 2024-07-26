using Discord;
using Serilog.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickEdit.LoggerConfig.LoggerImplementation;

public class AutoLog
{
    public static Task LogMessage(LogMessage message)
    {
        var logLevel = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        Log.Write(logLevel, message.Exception, "{Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
