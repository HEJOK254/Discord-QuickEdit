using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace QuickEdit.Commands;
internal sealed class InteractionServiceHandler(DiscordSocketClient client, InteractionService interactionService, InteractionServiceConfig interactionServiceConfig, IHostApplicationLifetime appLifetime) : IHostedService
{
	private readonly DiscordSocketClient _client = client;
	private InteractionService _interactionService = interactionService;
	private readonly InteractionServiceConfig _interactionServiceConfig = interactionServiceConfig;
	private readonly IHostApplicationLifetime _appLifetime = appLifetime;
	private static readonly SemaphoreSlim _initSemaphore = new(1);
	private static bool isReady = false;

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_client.Ready += InitAsync;
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_interactionService?.Dispose();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Initialize the InteractionService
	/// </summary>
	private async Task InitAsync()
	{
		await _initSemaphore.WaitAsync();

		// Prevent re-initialization
		if (isReady)
			return;

		try
		{
			_interactionService = new InteractionService(_client.Rest, _interactionServiceConfig);
			await RegisterModulesAsync();

			// Can't simply get the result of the ExecuteCommandAsync, because of RunMode.Async
			// So the event has to be used to handle the result
			_interactionService.SlashCommandExecuted += OnSlashCommandExecutedAsync;
			isReady = true;
		}
		catch (Exception e)
		{
			Log.Fatal("Error initializing InteractionService: {e}", e);
			Environment.ExitCode = 1; // TODO: Maybe implement different exit codes in the future
			_appLifetime.StopApplication();
		}
		finally
		{
			_initSemaphore.Release();
		}
	}

	/// <summary>
	/// Register modules / commands
	/// </summary>
	private async Task RegisterModulesAsync()
	{
		// The service might not have been initialized yet
		if (_interactionService == null)
		{
			Log.Error("Failed to register modules: InteractionService not initialized.");
			throw new InvalidOperationException("InteractionService not initialized while trying to register commands");
		}

		try
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				await _interactionService.AddModulesAsync(assembly, null);
			}

			await _interactionService.RegisterCommandsGloballyAsync();
			_client!.InteractionCreated += OnInteractionCreatedAsync;
			Log.Information("Modules registered successfully");
		}
		catch (Exception e)
		{
			Log.Fatal("Error registering modules:\n{}", e);
			throw;
		}
	}

	private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
	{
		// The service might not have been initialized yet
		if (_interactionService == null)
		{
			Log.Error("Error handling interaction: InteractionService not initialized.");
			return;
		}

		try
		{
			var ctx = new SocketInteractionContext(_client, interaction);
			await _interactionService.ExecuteCommandAsync(ctx, null);
			// Result is handled in OnSlashCommandExecutedAsync, since the RunMode is RunMode.Async.
			// See https://docs.discordnet.dev/guides/int_framework/post-execution.html for more info.
		}
		catch (Exception e)
		{
			Log.Error("Error handling interaction:\n{e}", e);

			if (interaction.Type is InteractionType.ApplicationCommand)
			{
				await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
			}

			throw;
		}
	}

	private static async Task OnSlashCommandExecutedAsync(SlashCommandInfo commandInfo, IInteractionContext interactionContext, IResult result)
	{
		// Only trying to handle errors lol
		if (result.IsSuccess)
			return;

		try
		{
			Log.Error("Error handling interaction: {result.Error}", result);
			await interactionContext.Interaction.FollowupAsync("An error occurred while executing the command.", ephemeral: true);
		}
		catch (Exception e)
		{
			Log.Error("Error handling interaction exception bruh:\n{e}", e);
			throw;
		}
	}
}
