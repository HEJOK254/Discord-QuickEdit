using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;
using QuickEdit.Commands;
using QuickEdit.Logger;
using Serilog;

using Serilog.Extensions.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuickEdit;

internal class Program
{
	private static readonly Config.DiscordConfig? discordConfig = new();

	public static Task Main(string[] args) => new Program().MainAsync(args);

	public async Task MainAsync(string[] args)
	{
		// Configure Serilog first
		SerilogConfiguration.ConfigureLogger();
		ShowStartMessage();

		// Generic Host setup
		HostApplicationBuilderSettings hostSettings = new()
		{
			Args = args,
			Configuration = new ConfigurationManager(),
			ContentRootPath = AppDomain.CurrentDomain.BaseDirectory
		};

		hostSettings.Configuration.AddJsonFile("config.json");
		hostSettings.Configuration.AddCommandLine(args);

		HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder(hostSettings);
		ConfigureServices(hostBuilder.Services);
		hostBuilder.Configuration.GetRequiredSection(nameof(DiscordConfig))
			.Bind(discordConfig);

		using var host = hostBuilder.Build();

		// Change log level after getting Config
		try
		{
			SerilogConfiguration.LoggingLevel.MinimumLevel =
				discordConfig!.Debug
					? Serilog.Events.LogEventLevel.Debug
					: Serilog.Events.LogEventLevel.Information;
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to set debugging level:\n{e}", e);
			Environment.ExitCode = 1; // Exit without triggering DeepSource lol
			return;
		}

		if (!CheckFFMpegExists()) return;

		await host.RunAsync();
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		var socketConfig = new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.None
		};

		var interactionServiceConfig = new InteractionServiceConfig()
		{
			UseCompiledLambda = true,
			DefaultRunMode = RunMode.Async
		};

		services.AddSerilog();
		services.AddSingleton(discordConfig!);
		services.AddSingleton(socketConfig);
		services.AddSingleton(interactionServiceConfig);
		services.AddSingleton<DiscordSocketClient>();
		services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionServiceConfig));
		services.AddSingleton<InteractionServiceHandler>();
		services.AddHostedService<Bot>();
	}

	private static bool CheckFFMpegExists()
	{
		try
		{
			FFMpegHelper.VerifyFFMpegExists(GlobalFFOptions.Current);
			Log.Debug("Found FFMpeg");
			return true;
		}
		catch (FFMpegException)
		{
			Log.Fatal("FFMpeg not found.");
			Environment.ExitCode = 1;
			return false;
		}
		catch (Exception e)
		{
			// It seems that there might be a bug in FFMpegCore, causing VerifyFFMpegExists() to
			// fail before it can throw the correct exception, which causes a different exception. 
			Log.Fatal("FFMpeg verification resulted in a failure:\n{Message}", e);
			Environment.ExitCode = 1;
			return false;
		}
	}

	private static void ShowStartMessage()
	{
		// https://stackoverflow.com/questions/1600962/displaying-the-build-date
		var buildVer = typeof(Program).Assembly.GetName().Version?.ToString() ?? "??";
		var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc); // Use a different method maybe

		Console.WriteLine($"\u001b[36m ---- QuickEdit ver. {buildVer} - Build Date: {compileTime.ToUniversalTime()} UTC - By HEJOK254 ---- \u001b[0m");
	}
}
