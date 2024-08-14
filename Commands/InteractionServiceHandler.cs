using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace QuickEdit.Commands;
public class InteractionServiceHandler
{
	private static readonly DiscordSocketClient? _client = Program.client;
	private static InteractionService? _interactionService;
	private static readonly InteractionServiceConfig _interactionServiceConfig = new() { UseCompiledLambda = true, DefaultRunMode = RunMode.Async };
	private static readonly SemaphoreSlim _initSemaphore = new(1);
	private static bool isReady = false;

	/// <summary>
	/// Initialize the InteractionService
	/// </summary>
	public static async Task InitAsync()
	{
		await _initSemaphore.WaitAsync();

		// Prevent reinitialization
		if (isReady)
			return;

		try
		{
			if (_interactionService != null)
			{
				await Program.LogAsync("InteractionServiceHandler", "InteractionServiceHandler.InitAsync() executed after the InteractionService has already been initialized", LogSeverity.Warning);
				return;
			}

			_interactionService = new InteractionService(_client!.Rest, _interactionServiceConfig);
			await RegisterModulesAsync();

			// Can't simply get the result of the ExecuteCommandAsync, because of RunMode.Async
			// So the event has to be used to handle the result
			_interactionService.SlashCommandExecuted += OnSlashCommandExecutedAsync;
			isReady = true;
		}
		catch (Exception e)
		{
			Log.Fatal($"Error initializing InteractionService: {e.Message}");
			throw;
		}
		finally
		{
			_initSemaphore.Release();
		}
	}

	/// <summary>
	/// Register modules / commands
	/// </summary>
	public static async Task RegisterModulesAsync()
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
			Log.Fatal($"Error registering modules: {(Program.config != null && Program.config.debug ? e : e.Message)}");
			throw;
		}
	}

	public static async Task OnInteractionCreatedAsync(SocketInteraction interaction)
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
			Log.Error($"Error handling interaction: {e.Message}");

			if (interaction.Type is InteractionType.ApplicationCommand)
			{
				await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
			}

			throw;
		}
	}

	public static async Task OnSlashCommandExecutedAsync(SlashCommandInfo commandInfo, IInteractionContext interactionContext, IResult result)
	{
		// Only trying to handle errors lol
		if (result.IsSuccess)
			return;

		try
		{
			Log.Error($"Error handling interaction: {result.Error}");
			await interactionContext.Interaction.FollowupAsync("An error occurred while executing the command.", ephemeral: true);
		}
		catch (Exception e)
		{
			Log.Error($"Error handling interaction exception bruh: {e.ToString()}");
			throw;
		}
	}
}
