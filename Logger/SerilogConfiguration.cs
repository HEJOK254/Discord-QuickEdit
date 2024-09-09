using System.Diagnostics;
using Serilog;
using Serilog.Core;

namespace QuickEdit.Logger;

public class SerilogConfiguration
{
	// Use Debug by default, as it should get overwritten after the config is parsed and can help with Config issues
	public static LoggingLevelSwitch LoggingLevel { get; set; } = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
	public static void ConfigureLogger()
	{
		var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

		Directory.CreateDirectory(logDirectory);

		var logPath = Path.Combine(logDirectory, "quickedit-.log");

		var loggerConfig = new LoggerConfiguration()
			.MinimumLevel.ControlledBy(LoggingLevel)
			.WriteTo.Console()
			.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);

		Log.Logger = loggerConfig.CreateLogger();
	}
}