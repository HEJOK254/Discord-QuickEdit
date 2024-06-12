using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QuickEdit.Commands
{
	public class CommandManager
	{
		private DiscordSocketClient _client = Program.client;

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
		};
		#endregion

		public async Task Init()
		{
			// Build and register commands
			var builtCommands = BulkBuildCommands(slashCommandBuilders);
			await RegisterCommands(builtCommands);
			
			_client.SlashCommandExecuted += SlashCommandHandler;
		}

		private List<SlashCommandProperties> BulkBuildCommands(List<SlashCommandBuilder> commandBuilders) {
			var builtCommands = new List<SlashCommandProperties>();
			foreach (var commandBuilder in commandBuilders) {
				builtCommands.Add(commandBuilder.Build());
			}

			return builtCommands;
		}

		public async Task RegisterCommands(List<SlashCommandProperties> slashCommands) {
			try {
				await _client.BulkOverwriteGlobalApplicationCommandsAsync(slashCommands.ToArray());
				await Program.Log("CommandManager", "Successfully registered slash commands.");
			}
			catch {
				await Program.Log("CommandManager", "Failed to register slash commands.", LogSeverity.Critical);
				return;
			}
		}

		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			switch (command.Data.Name)
			{
				case "test":
					await command.RespondAsync("Test command executed!");
					break;
			}
		}
	}
}
