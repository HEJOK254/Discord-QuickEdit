﻿using Discord;
using Discord.Interactions;
using FFMpegCore;
using Serilog;
using SixLabors.ImageSharp;

namespace QuickEdit.Commands.Modules;

[Group("file", "File stuff")]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
public class Converter : InteractionModuleBase
{
    private static readonly Dictionary<string, Func<string, string, int, Task>> conversionMap = new()
    {
        // Video formats
        { ".mp4", async (input, output, fps) => await ConvertVideo(input, output) },
        { ".avi", async (input, output, fps) => await ConvertVideo(input, output) },
        { ".mov", async (input, output, fps) => await ConvertVideo(input, output) },
        { ".mkv", async (input, output, fps) => await ConvertVideo(input, output) },
        { ".gif", async (input, output, fps) => await ConvertVideoToGif(input, output, fps) },
        
        // Image formats
        { ".png", async (input, output, fps) => await ConvertImage(input, output) },
        { ".jpg", async (input, output, fps) => await ConvertImage(input, output) },
        { ".webp", async (input, output, fps) => await ConvertImage(input, output) },
        { ".bmp", async (input, output, fps) => await ConvertImage(input, output) },
        { ".tiff", async (input, output, fps) => await ConvertImage(input, output) },
    };

    [SlashCommand("convert", "Convert a file format to another one")]
    public async Task ConvertAsync(
        [Summary(description: "The video or image to convert")] Discord.Attachment attachment,
        [Choice("gif", ".gif"), Choice("mp4", ".mp4"), Choice("avi", ".avi"), Choice("mov", ".mov"), Choice("mkv", ".mkv"), Choice("png", ".png"), Choice("jpg", ".jpg"), Choice("webp", ".webp"), Choice("bmp", ".bmp"), Choice("tiff", ".tiff")] string outputFormat,
        [Summary("fps", description: "If using Gif, what fps should it have")] int fps = 30,
        [Summary(description: "A message to send with the converted file")] string message = "",
        [Summary(description: "If the file should be sent as a temporary message, that's only visible to you")] bool ephemeral = false)
    {
        string tempDirPath = Path.GetTempPath();
        string inputFilePath = Path.GetTempFileName();
        string outputFilePath = Path.Combine(tempDirPath, inputFilePath + outputFormat);

        // Acknowledge the command
        await DeferAsync(ephemeral);

        if (!Directory.Exists(tempDirPath))
        {
            Directory.CreateDirectory(tempDirPath);
            Log.Information("TMP directory not found. Created it automatically");
        }

        await DownloadFileAsync(attachment.Url, inputFilePath);

        try
        {
            if (conversionMap.TryGetValue(outputFormat, out var converter))
            {
                if (Path.GetExtension(inputFilePath) != outputFormat)
                {
                    await converter(inputFilePath, outputFilePath, fps);
                }
                else
                {
                    await FollowupAsync($"Silly, you are converting from {Path.GetExtension(inputFilePath)} to {outputFormat}.", ephemeral: ephemeral);
                    return;
                }
            }
            else
            {
                await FollowupAsync("Unsupported conversion type.", ephemeral: ephemeral);
                return;
            }

            await FollowupWithFileAsync(outputFilePath, $"output{outputFormat}", message, ephemeral: ephemeral);
        }
        catch (Exception ex)
        {
            Log.Error($"An error occurred during conversion: {ex.Message}");
            await FollowupAsync("An error occurred during the conversion process.", ephemeral: ephemeral);
        }
        finally
        {
           File.Delete(inputFilePath);
           File.Delete(outputFilePath);
        }
    }

    private static async Task ConvertVideo(string inputFilePath, string outputFilePath)
    {
        var ffmpegArgs = FFMpegArguments
            .FromFileInput(inputFilePath)
            .OutputToFile(outputFilePath, true, options => options
                .WithCustomArgument("-preset slow"));

        await ffmpegArgs.ProcessAsynchronously();
    }

    private static async Task ConvertVideoToGif(string inputFilePath, string outputFilePath, int fps)
    {
        string filterArguments = $"fps={fps},scale=500:-1:flags=lanczos";

        var ffmpegArgs = FFMpegArguments
            .FromFileInput(inputFilePath)
            .OutputToFile(outputFilePath, true, options => options
                .WithCustomArgument($"-filter_complex \"{filterArguments}\"")
                .WithCustomArgument("-b:v 3000k") // Increased bitrate
                .WithCustomArgument("-pix_fmt rgb24") // Set pixel format for GIF
                .WithCustomArgument("-preset slow")); // Use slow preset for better quality

        await ffmpegArgs.ProcessAsynchronously();
    }

    private static async Task ConvertImage(string inputFilePath, string outputFilePath)
    {
        using var image = SixLabors.ImageSharp.Image.Load(inputFilePath);
        string extension = Path.GetExtension(outputFilePath).ToLowerInvariant();
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                await Task.Run(() => image.SaveAsJpeg(outputFilePath));
                break;
            case ".png":
                await Task.Run(() => image.SaveAsPng(outputFilePath));
                break;
            case ".bmp":
                await Task.Run(() => image.SaveAsBmp(outputFilePath));
                break;
            case ".webp":
                await Task.Run(() => image.SaveAsWebp(outputFilePath));
                break;
            case ".tiff":
                await Task.Run(() => image.SaveAsTiff(outputFilePath));
                break;
            default:
                throw new NotSupportedException($"The format '{extension}' is not supported.");
        }
    }

    private static async Task DownloadFileAsync(string uri, string path)
    {
        using var client = new HttpClient();
        using var s = await client.GetStreamAsync(uri);
        using var fs = new FileStream(path, FileMode.OpenOrCreate);
        await s.CopyToAsync(fs);
        fs.Close();
    }
}
