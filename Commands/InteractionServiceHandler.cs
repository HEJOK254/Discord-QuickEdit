using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace QuickEdit.Commands;
public class InteractionServiceHandler
{
	private static readonly DiscordSocketClient _client = Program.client;
	private static InteractionService? _interactionService;
	private static readonly InteractionServiceConfig _interactionServiceConfig = new() { UseCompiledLambda = true, DefaultRunMode = RunMode.Async };

	/// <summary>
	/// Initialize the InteractionService
	/// </summary>
	/// <returns>True if success, false if failure</returns>
	public static async Task InitAsync()
	{
		try
		{
			_interactionService = new InteractionService(_client.Rest, _interactionServiceConfig);
			await RegisterModulesAsync();
		}
		catch
		{
			await Program.LogAsync("InteractionServiceHandler", "Error initializing InteractionService", LogSeverity.Critical);
			throw;
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
			await Program.LogAsync("InteractionServiceManager.RegisterModulesAsync()", "InteractionService not initialized yet", LogSeverity.Error);
			throw new Exception("InteractionService not initialized while trying to register commands");
		}

		try
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				await _interactionService.AddModulesAsync(assembly, null);
			}

			await _interactionService.RegisterCommandsGloballyAsync();
			_client.InteractionCreated += OnInteractionCreatedAsync;
			await Program.LogAsync("InteractionServiceManager", "Modules registered successfully", LogSeverity.Info);
		}
		catch (Exception e)
		{
			await Program.LogAsync("InteractionServiceManager", $"Error registering modules. ({e})", LogSeverity.Critical);
			throw;
		}
	}

	public static async Task OnInteractionCreatedAsync(SocketInteraction interaction)
	{
		// The service might not have been initialized yet
		if (_interactionService == null)
		{
			await Program.LogAsync("InteractionServiceManager.OnInteractionCreatedAsync()", "InteractionService not initialized yet", LogSeverity.Error);
			return;
		}

		try
		{
			var ctx = new SocketInteractionContext(_client, interaction);
			var res = await _interactionService.ExecuteCommandAsync(ctx, null);

			if (res.IsSuccess is false)
			{
				await Program.LogAsync("InteractionServiceManager", $"Error handling interaction: {res}", LogSeverity.Error);
				await ctx.Channel.SendMessageAsync(res.ToString());
			}
		}
		catch (Exception e)
		{
			await Program.LogAsync("InteractionServiceManager", $"Error handling interaction. {e.Message}", LogSeverity.Error);

			if (interaction.Type is InteractionType.ApplicationCommand)
			{
				await interaction.GetOriginalResponseAsync().ContinueWith(async msg => await msg.Result.DeleteAsync());
			}

			throw;
		}
	}
}
