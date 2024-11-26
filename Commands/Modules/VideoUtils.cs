using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using FFMpegCore;
using Serilog;

namespace QuickEdit.Commands.Modules;
[Group("video", "Video utilities")]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
public class VideoUtils : InteractionModuleBase
{
	[SlashCommand("trim", "Trim a video")]
	public async Task TrimVideoAsync(
		[Summary(description: "The video to trim")] Discord.Attachment video,
		[Summary("start", "What time should the video start? [XXh XXm XXs XXms]")] string trimStartString = "",
		[Summary("end", "What time should the video end? [XXh XXm XXs XXms]")] string trimEndString = "",
		[Summary(description: "A message to send with the video when it's trimmed")] string message = "",
		[Summary(description: "If the video should be sent as a temporary message, that's only visible to you")] bool ephemeral = false)
	{
		string videoInputPath = Path.GetTempFileName();
		string videoOutputPath = Path.Combine(Path.GetTempPath(), videoInputPath + ".mp4");

		// Achknowledge the command
		await DeferAsync(ephemeral);

		// Reject incorrect video formats
		if (video.ContentType != "video/mp4" && video.ContentType != "video/quicktime")
		{
			await FollowupAsync("Invalid video format. Please provide an MP4 file.", ephemeral: true);
			return;
		}

		// There is a similar check for the TimeSpan library below, but this it to avoid 
		if (string.IsNullOrEmpty(trimStartString) && string.IsNullOrEmpty(trimEndString))
		{
			await FollowupAsync("You must provide a start or end time to trim the video.", ephemeral: true);
			return;
		}

		// Get TimeSpans
		TimeSpan trimStart = TimeSpan.Zero;
		TimeSpan trimEnd = TimeSpan.Zero;
		try
		{
			// Avoid invalid format exceptions
			if (!string.IsNullOrEmpty(trimStartString)) trimStart = TimeSpanFromHMS(trimStartString);
			if (!string.IsNullOrEmpty(trimEndString)) trimEnd = TimeSpanFromHMS(trimEndString);
		}
		catch (ArgumentException)
		{
			await FollowupAsync("Invalid time format. Please provide a valid time format (XXh XXm XXs XXms).", ephemeral: true);
			return;
		}
		// Make sure the times are not negative | https://stackoverflow.com/a/1018659/17003609 (comment)
		trimStart = trimStart.Duration();
		trimEnd = trimEnd.Duration();
		// The video can't be trimmed if both start and end times are 0
		if (trimStart == TimeSpan.Zero && trimEnd == TimeSpan.Zero)
		{
			await FollowupAsync("You must provide a start or end time to trim the video.", ephemeral: true);
			return;
		}
		await DownloadVideoAsync(video.Url, videoInputPath);
		var mediaInfo = await FFProbe.AnalyseAsync(videoInputPath);
		CheckTimes(mediaInfo.Duration, ref trimStart, ref trimEnd);
		// Process and send video
		var ffmpegArgs = FFMpegArguments
			.FromFileInput(videoInputPath)
			.OutputToFile(videoOutputPath, true, options => options
				.WithCustomArgument("-preset slow") // Use slow preset for better quality
				.WithCustomArgument($"-t {trimEnd - trimStart}")); // Duration

		await ffmpegArgs.ProcessAsynchronously();
		await FollowupWithFileAsync(videoOutputPath, video.Filename, message, ephemeral: ephemeral);
		// Clean up
		File.Delete(videoInputPath);
		File.Delete(videoOutputPath);
	}

	/// <summary>
	/// Check and set the start and end times to follow restrictions:
	/// <list type="bullet">
	///		<item>Set <paramref name="trimEnd"/> to <paramref name="duration"/> if smaller or equal to <paramref name="trimStart"/></item>
	///		<item>Clamp <paramref name="trimEnd"/> to the <paramref name="duration"/></item>
	///		<item>Set <paramref name="trimStart"/> to <c>0</c> if it's greater or equal to the video's <paramref name="duration"/></item>
	/// </list>
	/// </summary>
	/// <param name="duration">Duration of the video</param>
	/// <param name="trimStart">Start of the trim</param>
	/// <param name="trimEnd">End of the trim</param>
	private static void CheckTimes(TimeSpan duration, ref TimeSpan trimStart, ref TimeSpan trimEnd)
	{
		// Set trimEnd to duration if smaller or equal to trimStart
		if (trimEnd <= trimStart)
		{
			trimEnd = duration;
		}

		// Clamp the end time to the video's duration
		trimEnd = new[] { duration, trimEnd }.Min(); // https://stackoverflow.com/a/1985326/17003609

		// Set trimStart to 0 if it's greater or equal to the video's duration
		if (trimStart >= duration)
		{
			trimStart = TimeSpan.Zero;
		}
	}

	private static async Task DownloadVideoAsync(string uri, string path)
	{
		using var client = new HttpClient();
		using var s = await client.GetStreamAsync(uri);
		using var fs = new FileStream(path, FileMode.OpenOrCreate);
		await s.CopyToAsync(fs);
		fs.Close();
	}

	/// <summary>
	/// Parses a string in the format 'XXh XXm XXs XXms' into a TimeSpan object
	/// </summary>
	/// <param name="input">Input string to parse, in format [XXh XXm XXs]</param>
	/// <returns>The parsed TimeSpan</returns>
	/// <exception cref="ArgumentException">Thrown when the input string is in an invalid format</exception>
	public static TimeSpan TimeSpanFromHMS(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			throw new ArgumentException("Input string is not in a valid format");
		}

		// Define the regular expression pattern to match hours, minutes, and seconds
		string pattern = @"((?<hours>\d+)h)?\s*((?<minutes>\d+)m|min)?\s*((?<seconds>\d+)s)?\s*((?<milliseconds>\d+)ms)?";

		// Match the input string with the pattern
		var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);

		// Check if at least one component (hours, minutes, or seconds) is present
		if (!match.Groups["hours"].Success && !match.Groups["minutes"].Success && !match.Groups["seconds"].Success && !match.Groups["milliseconds"].Success)
		{
			throw new ArgumentException("Input string is not in a valid format");
		}

		// Extract the matched groups
		int hours = 0;
		if (match.Groups["hours"].Success) int.TryParse(match.Groups["hours"].Value, out hours);
		int minutes = 0;
		if (match.Groups["minutes"].Success) int.TryParse(match.Groups["minutes"].Value, out minutes);
		int seconds = 0;
		if (match.Groups["seconds"].Success) int.TryParse(match.Groups["seconds"].Value, out seconds);
		int milliseconds = 0;
		if (match.Groups["milliseconds"].Success) int.TryParse(match.Groups["milliseconds"].Value, out milliseconds);

		// Create and return the TimeSpan object
		return new TimeSpan(days: 0, hours, minutes, seconds, milliseconds);
	}
}
