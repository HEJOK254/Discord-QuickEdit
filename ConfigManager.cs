using Discord;
using Newtonsoft.Json;

namespace QuickEdit;
public class Config
{
	public required string token;
	public ActivityType statusType;
	public string status = string.Empty;
	public bool debug = false;

	public static Config? GetConfig()
	{
		string path = "./config.json";
		if (!File.Exists(path))
		{
			Program.LogAsync("Config", $"Config file not found at: {path}", LogSeverity.Critical);
			return null;
		}

		try
		{
			return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
		}
		catch
		{
			Program.LogAsync("Config", "Failed to parse config file.", LogSeverity.Critical);
			return null;
		}
	}
}
