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
            Log.Fatal("Config file not found at: {Path}", path);
            return null;
		}

		try
		{
			return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
		}
		catch
		{
            Log.Fatal("Failed to parse config file.");
            return null;
		}
	}
}
