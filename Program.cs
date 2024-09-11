using Discord;
using Discord.WebSocket;
using FFMpegCore;
using FFMpegCore.Helpers;
using QuickEdit.Commands;
using QuickEdit.Logger;
using Serilog;

namespace QuickEdit;

internal class Program
{
	public static DiscordSocketClient? client;
	public static Config? config;
	public static readonly DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.None };

	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		SerilogConfiguration.ConfigureLogger();

		// Handle exit
		AppDomain.CurrentDomain.ProcessExit += (s, e) =>
			{
				Log.Debug("Stopping program gracefully");
				try
				{
					if (client == null) return;
					Log.Debug("Stopping bot");
					client.LogoutAsync().Wait();
					client.StopAsync().Wait();
					Log.Debug("Stopped bot");
				}
				catch (Exception ex)
				{
					Log.Error("Error while trying to stop bot\n{e}", ex);
				}
				finally
				{
					Log.CloseAndFlush();
				}
			};

		ShowStartMessage();

		try
		{
			config = Config.GetConfig();
			SerilogConfiguration.LoggingLevel.MinimumLevel = config.debug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;
		}
		catch
		{
			Log.Fatal("Failed to GetConfig()");
			Environment.ExitCode = 1; // Exit without triggering DeepSource lol
			return;
		}

		client = new DiscordSocketClient(socketConfig);

		client.Log += AutoLog.LogMessage;
		client.Ready += OnReadyAsync;

		await client.LoginAsync(TokenType.Bot, config.token);
		await client.StartAsync();

		// Custom activities use a different method
		if (config.statusType == ActivityType.CustomStatus)
		{
			await client.SetCustomStatusAsync(config.status);
		}
		else
		{
			await client.SetGameAsync(config.status, null, config.statusType);
		}

		await Task.Delay(-1);
	}

	private static void ShowStartMessage()
	{
		// https://stackoverflow.com/questions/1600962/displaying-the-build-date
		var buildVer = typeof(Program).Assembly.GetName().Version?.ToString() ?? "??";
		var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc); // Use a different method maybe

		Console.WriteLine($"\u001b[36m ---- QuickEdit ver. {buildVer} - Build Date: {compileTime.ToUniversalTime()} UTC - By HEJOK254 ---- \u001b[0m");
	}

	private async Task OnReadyAsync()
	{
		try
		{
			await InteractionServiceHandler.InitAsync();
		}
		catch
		{
			Log.Fatal("Program is exiting due to an error in InteractionServiceHandler.");
			// The program cannot continue without the InteractionService, so terminate it. Nothing important should be running at this point.
			Environment.Exit(1); // skipcq: CS-W1005
		}
	}
}
