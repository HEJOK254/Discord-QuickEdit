using Discord;
using Discord.Interactions;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace QuickEdit.Commands.Modules;

[Group("image", "Image stuff")]
[IntegrationType(ApplicationIntegrationType.UserInstall)]
[CommandContextType(InteractionContextType.Guild, InteractionContextType.PrivateChannel)]
public class Jpegify : InteractionModuleBase 
{
    [SlashCommand("jpegify", "Jpegify an Image")]

    public async Task JpegifyAsync(
		[Summary(description: "The image to jpegify")] Discord.Attachment image,
		[Summary("jpegification", "How much compression should be applied 1 - 100")] int jpegification,
		[Summary(description: "A message to send with the image when it's jpegified")] string message = "",
		[Summary(description: "If the image should be sent as a temporary message, that's only visible to you")] bool ephemeral = false)
	{
        jpegification = 101 - Math.Clamp(jpegification, 1, 100);

        string imageInputPath = Path.GetTempFileName();
		string imageOutputPath = Path.Combine(Path.GetTempPath(), imageInputPath + ".jpeg");

        await DeferAsync(ephemeral);
        try 
        {
            Log.Debug("Jpegification: " + jpegification);
            await DownloadImageAsync(image.Url, imageInputPath);

            // Process IMG
            using var img = SixLabors.ImageSharp.Image.Load(imageInputPath);
            var encoder = new JpegEncoder 
            { 
                Quality = jpegification
            };
            await img.SaveAsJpegAsync(imageOutputPath, encoder);

            await FollowupWithFileAsync(imageOutputPath, image.Filename, message, ephemeral: ephemeral);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during Jpegify process");
            await FollowupAsync("An error occured while processing the image.", ephemeral: ephemeral);
        }
        finally
        {
            File.Delete(imageInputPath);
            File.Delete(imageOutputPath);
        }
    }

    private static async Task DownloadImageAsync(string uri, string path)
	{
		using var client = new HttpClient();
		using var s = await client.GetStreamAsync(uri);
		using var fs = new FileStream(path, FileMode.OpenOrCreate);
		await s.CopyToAsync(fs);
		fs.Close();
	}
}