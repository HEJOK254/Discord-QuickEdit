using Discord;
using Serilog;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations;

namespace QuickEdit.Config;

internal sealed class ConfigManager
{
	/// <summary>
	/// Set up configuration services
	/// </summary>
	/// <param name="builder">The HostApplicationBuilder used for the program</param>
	/// <returns>True is success, False if failure</returns>
	internal static bool LoadConfiguration(HostApplicationBuilder builder)
	{
		try
		{
			// Binding
			var discordConfig = builder.Configuration.GetRequiredSection(DiscordConfig.ConfigurationSectionName)
				.Get<DiscordConfig>()!;

			// Validation
			ValidateConfig(discordConfig);

			// Service registration (DI)
			builder.Services.AddSingleton(discordConfig);
			return true;
		}
		catch (ValidationException e)
		{
			Log.Fatal("Config parse error: {e}", e.Message);
			Environment.ExitCode = 1;
			return false;
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to get config or create config service: {e}", e);
			Environment.ExitCode = 1;
			return false;
		}
	}

	private static void ValidateConfig(object config)
	{
		var validationContext = new ValidationContext(config);
		Validator.ValidateObject(config, validationContext, validateAllProperties: true);
	}
}

public sealed class DiscordConfig
{
	public const string ConfigurationSectionName = "DiscordConfig";

	// TODO: Move the Token to user secrets at some point

	[Required]
	[RegularExpression(@"^([MN][\w-]{23,25})\.([\w-]{6})\.([\w-]{27,39})$", ErrorMessage = "Invalid token format")]
	public required string Token { get; set; }
	public ActivityType StatusType { get; set; }
	public string? Status { get; set; }
	public bool Debug { get; set; }
}
