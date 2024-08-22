using Discord;
using Newtonsoft.Json;
using Serilog;

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
			Log.Fatal($"Config file not found at: {Path.GetFullPath(path)}");
			return null;
		}

		try
		{
			return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
		}
		catch (Exception e)
		{
			Log.Fatal($"Failed to parse config file: {e.Message}");
			return null;
		}
	}
}
