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

	public static Config GetConfig()
	{
		string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
		if (!File.Exists(path))
		{
			Log.Fatal($"Config file not found at: {Path.GetFullPath(path)}");
			Log.Information($"Check if you have a valid config.json file present in the directory of the executable ({AppDomain.CurrentDomain.BaseDirectory})");
			throw new FileNotFoundException();
		}

		try
		{
			var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
			Log.Debug("Loaded config file");
			return config;
		}
		catch (Exception e)
		{
			Log.Fatal($"Failed to parse config file: {e.Message}");
			throw;
		}
	}
}
