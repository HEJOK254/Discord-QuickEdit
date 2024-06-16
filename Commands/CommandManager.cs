using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickEdit.Commands;
public class CommandManager
{
	private readonly DiscordSocketClient _client = Program.client;

	// Macros for interaction context types
	private static readonly InteractionContextType[] interactionContextAll = new InteractionContextType[] { InteractionContextType.PrivateChannel, InteractionContextType.Guild, InteractionContextType.BotDm };
	private static readonly InteractionContextType[] interactionContextUser = new InteractionContextType[] { InteractionContextType.PrivateChannel, InteractionContextType.Guild };

	#region Command List
	List<SlashCommandBuilder> slashCommandBuilders = new List<SlashCommandBuilder>() {
		new SlashCommandBuilder()
			.WithName("test")
			.WithDescription("Test command.")
			.WithIntegrationTypes(ApplicationIntegrationType.UserInstall)
			.WithContextTypes(interactionContextAll),

		new SlashCommandBuilder()
			.WithName("trim")
			.WithDescription("Trim a video")
			.WithIntegrationTypes(ApplicationIntegrationType.UserInstall)
			.WithContextTypes(interactionContextUser)
			.AddOption(new SlashCommandOptionBuilder()
				.WithName("video")
				.WithDescription("The video to trim")
				.WithType(ApplicationCommandOptionType.Attachment)
				.WithRequired(true))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName("start")
				.WithDescription("What time should the video start? [XXh XXm XXs XXms]")
				.WithType(ApplicationCommandOptionType.String) // TODO: Change to ApplicationCommandOptionType.Time if added one day
				.WithAutocomplete(true)
				.WithMinLength(2) // The time cannot be expressed with less than 2 characters
				.WithRequired(false))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName("end")
				.WithDescription("What time should the video end? [XXh XXm XXs XXms]")
				.WithType(ApplicationCommandOptionType.String) // TODO: Change to ApplicationCommandOptionType.Time if added one day
				.WithMinLength(2) // The time cannot be expressed with less than 2 characters
				.WithRequired(false))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName("message")
				.WithDescription("A message to send with the video when it's trimmed")
				.WithType(ApplicationCommandOptionType.String)
				.WithRequired(false))
			.AddOption(new SlashCommandOptionBuilder()
				.WithName("ephemeral")
				.WithDescription("If the video should be sent as a temporary message, that's only visible to you")
				.WithType(ApplicationCommandOptionType.Boolean)
				.WithRequired(false))
	};
	#endregion

	public async Task InitAsync()
	{
		// Build and register commands
		var builtCommands = BulkBuildCommands(slashCommandBuilders);
		await RegisterCommandsAsync(builtCommands);
			
		_client.SlashCommandExecuted += SlashCommandHandlerAsync;
	}

	private List<SlashCommandProperties> BulkBuildCommands(List<SlashCommandBuilder> commandBuilders) {
		var builtCommands = new List<SlashCommandProperties>();
		foreach (var commandBuilder in commandBuilders) {
			builtCommands.Add(commandBuilder.Build());
		}

		return builtCommands;
	}

	public async Task RegisterCommandsAsync(List<SlashCommandProperties> slashCommands) {
		try {
			await _client.BulkOverwriteGlobalApplicationCommandsAsync(slashCommands.ToArray());
			await Program.LogAsync("CommandManager", "Successfully registered slash commands.");
		}
		catch {
			await Program.LogAsync("CommandManager", "Failed to register slash commands.", LogSeverity.Critical);
			return;
		}
	}

	private async Task SlashCommandHandlerAsync(SocketSlashCommand command)
	{
		switch (command.Data.Name)
		{
			case "test":
				command.RespondAsync("Test command executed!");
				break;

			case "trim":
				VideoUtils.TrimVideoAsync(command);
				break;

			// In case the command is not recognized by the bot
			default:
				await command.RespondAsync("An error occurred with the command you tried to execute", ephemeral: true);
				await Program.LogAsync("CommandManager", "Failed to execute slash command.", LogSeverity.Error);
				break;
		}
	}
}