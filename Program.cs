using Discord;
using Discord.WebSocket;
using FFMpegCore;
using FFMpegCore.Helpers;
using QuickEdit.Commands;

namespace QuickEdit;
class Program
{
	public static DiscordSocketClient? client;
	public static Config? config = Config.GetConfig();
	public static readonly DiscordSocketConfig socketConfig = new() { GatewayIntents = GatewayIntents.None };

	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		// If the config is null, we can't continue as the bot won't have a token to login with
		if (config == null) return;
		FFMpegHelper.VerifyFFMpegExists(GlobalFFOptions.Current);

		client = new DiscordSocketClient(socketConfig);

		client.Log += LogAsync;
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

	private async Task OnReadyAsync()
	{
		try
		{
			await InteractionServiceHandler.InitAsync();
		}
		catch
		{
			await LogAsync("Program", "Exiting", LogSeverity.Info);
			// The program cannot continue without the InteractionService, so terminate it. Nothing important should be running at this point.
			Environment.Exit(1); // skipcq: CS-W1005
		}
	}

	public Task LogAsync(LogMessage message)
	{
		string msg = $"[{DateTime.UtcNow.ToString("HH.mm.ss")}] {message.Source}: {message.Message}";
		Console.WriteLine(msg + " " + message.Exception);
		return Task.CompletedTask;
	}

	public static Task LogAsync(string source, string message, LogSeverity severity = LogSeverity.Info)
	{
		string msg = $"[{DateTime.UtcNow.ToString("HH.mm.ss")}] {source}: {message}";

		// Change color / display based on severity
		switch (severity)
		{
			case LogSeverity.Warning:
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine(msg);
				Console.ResetColor();
				break;
			case LogSeverity.Critical:
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.WriteLine(msg);
				Console.ResetColor();
				break;

			case LogSeverity.Error:
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(msg);
				Console.ResetColor();
				break;

			case LogSeverity.Verbose:
				// Verbose logs are only displayed if the debug flag is set to true in the config
				if (config == null || !config.debug) break;

				Console.ForegroundColor = ConsoleColor.DarkGray;
				Console.WriteLine(msg);
				Console.ResetColor();
				break;

			default:
				// All other severities should have the default color of the console
				Console.ResetColor();
				Console.WriteLine(msg);
				break;
		}
		return Task.CompletedTask;
	}
}
