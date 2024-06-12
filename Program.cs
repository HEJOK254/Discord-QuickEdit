using Discord;
using Discord.WebSocket;
using QuickEdit;
using QuickEdit.Commands;
using QuickEdit.Config;

class Program
{
	public static DiscordSocketClient client;
	public static Config config = Config.GetConfig();

	public static Task Main(string[] args) => new Program().MainAsync();

	public async Task MainAsync()
	{
		client = new DiscordSocketClient();

		client.Log += Log;
		client.Ready += OnReady;

		await client.LoginAsync(TokenType.Bot, config.token);
		await client.StartAsync();

		// Custom activities use a different method
		if (config.statusType == ActivityType.CustomStatus) {
			await client.SetCustomStatusAsync(config.status);
		} else {
			await client.SetGameAsync(config.status, null, config.statusType);
		}

		await Task.Delay(-1);
	}

	private async Task OnReady()
	{
		await new CommandManager().Init();
	}

	public Task Log(LogMessage message)
	{
		string msg = $"[{DateTime.Now.ToString("HH.mm.ss")}] {message.Source}: {message.Message}";
		Console.WriteLine(msg + " " + message.Exception);
		return Task.CompletedTask;
	}

	public static Task Log(string source, string message, LogSeverity severity = LogSeverity.Info)
	{
		string msg = $"[{DateTime.Now.ToString("HH.mm.ss")}] {source}: {message}";

		// Change color based on severity
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

			default:
				// All other severities should have the default color of the console
				Console.ResetColor();
				Console.WriteLine(msg);
				break;
		}
		return Task.CompletedTask;
	}
}
