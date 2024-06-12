using Discord;
using Newtonsoft.Json;

namespace QuickEdit.Config
{
	public class Config
	{
		public required string token;
		public ulong logChannel;
		public ulong guildID;
		public ActivityType statusType;
		public string status = string.Empty;
		public bool debug = false;

		public static Config GetConfig()
		{
			string path = "./config.json";
			if (!File.Exists(path))
			{
				Program.Log("Config", $"Config file not found at: {path}", LogSeverity.Critical);
				Environment.Exit(1);
			}

			try {
				return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path))!;
			} catch {
				Program.Log("Config" , "Failed to parse config file.", LogSeverity.Critical);
				Environment.Exit(1);
				return null;
			}
		}
	}
}
