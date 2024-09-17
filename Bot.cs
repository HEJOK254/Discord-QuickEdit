using System;
using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using QuickEdit.Commands;
using QuickEdit.Logger;
using Serilog;

namespace QuickEdit;

internal sealed class Bot(DiscordSocketClient client, Config.DiscordConfig discordConfig, InteractionServiceHandler interactionServiceHandler, IHostApplicationLifetime appLifetime) : IHostedService
{
	private readonly DiscordSocketClient _client = client;
	private readonly Config.DiscordConfig _discordConfig = discordConfig;
	private readonly InteractionServiceHandler _interactionServiceHandler = interactionServiceHandler;
	private readonly IHostApplicationLifetime _appLifetime = appLifetime;

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_client.Log += AutoLog.LogMessage;
		_client.Ready += OnReadyAsync;

		await _client.LoginAsync(TokenType.Bot, _discordConfig.Token);
		await _client.StartAsync();

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
