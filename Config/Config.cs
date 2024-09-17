using Discord;
using Newtonsoft.Json;
using Serilog;

namespace QuickEdit.Config;

public sealed class DiscordConfig
{
	public string? Token { get; set; }
	public ActivityType StatusType { get; set; }
	public string? Status { get; set; }
	public bool Debug { get; set; }
}
