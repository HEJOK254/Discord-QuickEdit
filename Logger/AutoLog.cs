using Discord;
using Serilog;
using Serilog.Events;

namespace QuickEdit.Logger;

public class AutoLog
{
	// LogMessage does not run asynchronously, so we can ignore the DeepSource error
	// skipcq: CS-R1073
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
