using Serilog;

namespace QuickEdit.Logger;

public class SerilogConfiguration
{

	public static void ConfigureLogger()
	{
		var intermediateOutputPath =
			Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..", @"obj\Debug\net8.0\Logs"));

		Directory.CreateDirectory(intermediateOutputPath);

		var logPath = Path.Combine(intermediateOutputPath, $"consoleapplog-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");

		var loggerConfig = new LoggerConfiguration()
			.Enrich.WithMachineName()
			.WriteTo.Console()
			.WriteTo.File(logPath, rollingInterval: RollingInterval.Day);

		if (Program.config != null && Program.config.debug)
			loggerConfig.MinimumLevel.Debug();
		else
			loggerConfig.MinimumLevel.Information();

		Log.Logger = loggerConfig.CreateLogger();
	}
}