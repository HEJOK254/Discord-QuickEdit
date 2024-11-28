using System.ComponentModel.DataAnnotations;
using Discord;

namespace QuickEdit.Config;

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
