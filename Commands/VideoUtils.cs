using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Pipes;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace QuickEdit.Commands;
public class VideoUtils
{
	public static async Task TrimVideoAsync(SocketSlashCommand command)
	{
		// Get arguments
		string? trimStartString = command.Data.Options.FirstOrDefault(x => x.Name == "start")?.Value as string;
		string? trimEndString = command.Data.Options.FirstOrDefault(x => x.Name == "end")?.Value as string;
		string message = command.Data.Options.FirstOrDefault(x => x.Name == "message")?.Value as string ?? string.Empty;
		bool ephemeral = command.Data.Options.FirstOrDefault(x => x.Name == "ephemeral")?.Value as bool? ?? false;
		var attachment = command.Data.Options.FirstOrDefault(x => x.Name == "video")?.Value as Discord.Attachment;

		string videoInputPath = "./tmp/input.mp4";      // Normally, I would use Path.GetTempFileName(), but FFMpegCore doesn't seem to
		string videoOutputPath = "./tmp/output.mp4";	// like the .tmp file extension (or anything other than .mp4) as far as i know

		// Achknowledge the command
		await command.DeferAsync(ephemeral);

		// The video can't be trimmed if both start and end times are null / 0
		if (trimStartString == null && trimEndString == null) {
			await command.FollowupAsync("You must provide a start or end time to trim the video.", ephemeral: true);
			return;
		}

		// The attachment should never be null, as it's a required option
		if (attachment == null)
		{
			await command.FollowupAsync("An error occurred while trying to process the video. Please try again.", ephemeral: true);
			await Program.LogAsync("VideoUtils", "Attachment was null in TrimVideoAsync", LogSeverity.Error);
			return;
		}

		// Reject incorrect formats
		if (attachment.ContentType != "video/mp4")
		{
			await command.FollowupAsync("Invalid video format. Please provide an MP4 file.", ephemeral: true);
			return;
		}

		TimeSpan? trimStart = await GetTrimTimeAsync(trimStartString, command);
		TimeSpan? trimEnd = await GetTrimTimeAsync(trimEndString, command);

		// The GetTrimTime method returns null on error
		if (trimStart == null || trimEnd == null) return;

		// Check if the directory, where the video is supposed to be exists
		if (!Directory.Exists("./tmp"))
		{
			Directory.CreateDirectory("./tmp");
		}

		await DownloadVideoAsync(attachment.Url, videoInputPath);

		// Replace the end time with the video's duration if it's 0, greater than the video's duration, or smaller than the start time
		if (trimEnd <= trimStart)
		{
			var mediaInfo = await FFProbe.AnalyseAsync(videoInputPath);
			trimEnd = mediaInfo.Duration;
		}

		// Process and send video
		await FFMpeg.SubVideoAsync(videoInputPath, videoOutputPath, (TimeSpan)trimStart, (TimeSpan)trimEnd); // Need to convert the TimeSpans since the value is nullable
		await command.FollowupWithFileAsync(videoOutputPath, attachment.Filename, message, ephemeral: ephemeral);

		// Clean up
		File.Delete(videoInputPath);
		File.Delete(videoOutputPath);
	}

	private static async Task DownloadVideoAsync(string uri, string path) {
		using var client = new HttpClient();
		using var s = await client.GetStreamAsync(uri);
		using var fs = new FileStream(path, FileMode.OpenOrCreate);
		await s.CopyToAsync(fs);
		fs.Close();
	}

	private static async Task<TimeSpan?> GetTrimTimeAsync(string? timeString, SocketSlashCommand command) {
		if (timeString == null)
		{
			// This will later be replaced with the video's duration
			return TimeSpan.Zero;
		}

		try
		{
			return TimeSpanFromHMS(timeString);
		}
		catch
		{
			await command.FollowupAsync("Invalid time format. Please provide a valid time format (XXh XXm XXs XXms).", ephemeral: true);
			await Program.LogAsync("VideoUtils", $"Invalid time format in TrimVideoAsync (received: {timeString})", LogSeverity.Verbose);
			return null;
		}
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
		string pattern = @"((?<milliseconds>\d+)ms)?\s*((?<hours>\d+)h)?\s*((?<minutes>\d+)m|min)?\s*((?<seconds>\d+)s)?";

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