using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using QuickEdit.Commands;
using QuickEdit.Logger;
using Serilog;

namespace QuickEdit;

internal sealed class Bot(DiscordSocketClient client, Config.DiscordConfig discordConfig, IInteractionServiceHandler interactionServiceHandler, IHostApplicationLifetime appLifetime) : IHostedService
{
	private readonly DiscordSocketClient _client = client;
	private readonly Config.DiscordConfig _discordConfig = discordConfig;
	private readonly IInteractionServiceHandler _interactionServiceHandler = interactionServiceHandler;
	private readonly IHostApplicationLifetime _appLifetime = appLifetime;

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_client.Log += AutoLog.LogMessage;
		_client.Ready += OnReadyAsync;

		// Try-catch ValidateToken since LoginAsync doesn't throw exceptions, they just catch them
		// So there's no way to know if the token is invalid without checking it first (or is there?)
		// Also this is separate from the other try-catch to make sure it's the token that's invalid
		try
		{
			TokenUtils.ValidateToken(TokenType.Bot, _discordConfig.Token);
		}
		catch (ArgumentException e)
		{
			Log.Fatal("{e}", e.Message);
			Environment.ExitCode = 1;
			_appLifetime.StopApplication();
			return; // The app would normally continue for a short amount of time before stopping
		}

		// Most of the exceptions are caught by the library :( [maybe not idk i'm writing this at 2am]
		// This means that the program will log stuff in an ugly way and NOT stop the program on fatal errors
		try
		{
			// The token is already validated, so there's no need to validate it again
			await _client.LoginAsync(TokenType.Bot, _discordConfig.Token, validateToken: false);
			await _client.StartAsync();
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to start the bot: {e}", e);
			Environment.ExitCode = 1;
			_appLifetime.StopApplication();
			return;
		}

		// Custom activities use a different method
		if (_discordConfig.StatusType == ActivityType.CustomStatus)
		{
			await _client.SetCustomStatusAsync(_discordConfig.Status);
		}
		else
		{
			await _client.SetGameAsync(_discordConfig.Status, null, _discordConfig.StatusType);
		}
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await _client.LogoutAsync();
		await _client.StopAsync();
	}

	private async Task OnReadyAsync()
	{
		try
		{
			await _interactionServiceHandler.InitAsync();
		}
		catch
		{
			Log.Fatal("Program is exiting due to an error in InteractionServiceHandler.");
			// The program cannot continue without the InteractionService, so terminate it. Nothing important should be running at this point.
			Environment.ExitCode = 1;
			_appLifetime.StopApplication();
		}
	}
}
