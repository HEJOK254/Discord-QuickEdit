using QuickEdit.Config;
using Serilog;
using Serilog.Core;

namespace QuickEdit.Logger;

public class SerilogConfiguration(DiscordConfig discordConfig)
{
	// Use Debug by default, as it should get overwritten after the config is parsed and can help with Config issues
	public static LoggingLevelSwitch LoggingLevel { get; set; } = new LoggingLevelSwitch(Serilog.Events.LogEventLevel.Debug);
	private readonly DiscordConfig _discordConfig = discordConfig;

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

	internal bool SetLoggingLevelFromConfig()
	{
		try
		{
			LoggingLevel.MinimumLevel =
				_discordConfig.Debug
					? Serilog.Events.LogEventLevel.Debug
					: Serilog.Events.LogEventLevel.Information;
			return true;
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to set minimum log level: {e}", e);
			Environment.ExitCode = 1;
			return false;
		}
	}
}
