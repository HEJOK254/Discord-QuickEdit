using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

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

