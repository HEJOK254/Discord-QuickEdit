using Serilog;

namespace QuickEdit.Logger;

public class SerilogConfiguration
{
	public static void ConfigureLogger()
	{
		var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

		Directory.CreateDirectory(logDirectory);

		var logPath = Path.Combine(logDirectory, "quickedit-.log");

		var loggerConfig = new LoggerConfiguration()
			.WriteTo.Console()
			.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);

		if (Program.config != null && Program.config.debug)
			loggerConfig.MinimumLevel.Debug();
		else
			loggerConfig.MinimumLevel.Information();

		Log.Logger = loggerConfig.CreateLogger();
	}
}