using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuickEdit.LoggerConfig;

public class SerilogConfiguration
{
    public static void ConfigureLogger()
    {
        var intermediateOutputPath =
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..", @"obj\Debug\net8.0\Logs"));

        Directory.CreateDirectory(intermediateOutputPath);

        var logPath = Path.Combine(intermediateOutputPath, $"consoleapplog-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}