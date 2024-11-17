﻿using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using FFMpegCore;
using FFMpegCore.Exceptions;
using FFMpegCore.Helpers;
using QuickEdit.Commands;
using QuickEdit.Logger;
using QuickEdit.Config;
using Serilog;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuickEdit;

internal class Program
{
	public static Task Main(string[] args) => new Program().MainAsync(args);

	public async Task MainAsync(string[] args)
	{
		// Configure Serilog first
		SerilogConfiguration.ConfigureLogger();
		ShowStartMessage();

		// Generic Host setup
		HostApplicationBuilderSettings hostSettings = new()
		{
			Args = args,
			Configuration = new ConfigurationManager(),
			ContentRootPath = AppDomain.CurrentDomain.BaseDirectory
		};

		try
		{
			hostSettings.Configuration.AddJsonFile("config.json");
			hostSettings.Configuration.AddCommandLine(args);
		}
		catch (FileNotFoundException)
		{
			// Can't log the file name using FileNotFoundException.FileName as it's just null
			Log.Fatal("Couldn't find file 'config.json' in path: {path}", AppDomain.CurrentDomain.BaseDirectory);
			return;
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to add config providers:{e}", e);
			return;
		}

		HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder(hostSettings);
		if (!ConfigManager.LoadConfiguration(hostBuilder)) return;
		if (!ConfigureServices(hostBuilder.Services)) return;

		using var host = hostBuilder.Build();

		// Change log level after getting Config
		host.Services.GetRequiredService<SerilogConfiguration>().SetLoggingLevelFromConfig();

		if (!CheckFFMpegExists()) return;

		await host.RunAsync();
	}

	/// <summary>
	/// Configures Dependency Injection Services
	/// </summary>
	/// <param name="services">The service collection from the builder</param>
	/// <returns>True is success, False if failure</returns>
	private static bool ConfigureServices(IServiceCollection services)
	{
		try
		{
			var socketConfig = new DiscordSocketConfig()
			{
				GatewayIntents = GatewayIntents.None
			};

			var interactionServiceConfig = new InteractionServiceConfig()
			{
				UseCompiledLambda = true,
				DefaultRunMode = RunMode.Async
			};

			services.AddSerilog();
			services.AddTransient<SerilogConfiguration>();
			services.AddSingleton(socketConfig);
			services.AddSingleton(interactionServiceConfig);
			services.AddSingleton<DiscordSocketClient>();
			services.AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>(), interactionServiceConfig));
			services.AddHostedService<InteractionServiceHandler>();
			services.AddHostedService<Bot>();
			return true;
		}
		catch (Exception e)
		{
			Log.Fatal("Failed to configure services: {e}", e);
			Environment.ExitCode = 1;
			return false;
		}
	}

	private static bool CheckFFMpegExists()
	{
		try
		{
			FFMpegHelper.VerifyFFMpegExists(GlobalFFOptions.Current);
			Log.Debug("Found FFMpeg");
			return true;
		}
		catch (FFMpegException)
		{
			Log.Fatal("FFMpeg not found.");
			Environment.ExitCode = 1;
			return false;
		}
		catch (Exception e)
		{
			// It seems that there might be a bug in FFMpegCore, causing VerifyFFMpegExists() to
			// fail before it can throw the correct exception, which causes a different exception. 
			Log.Fatal("FFMpeg verification resulted in a failure: {Message}", e);
			Environment.ExitCode = 1;
			return false;
		}
	}

	private static void ShowStartMessage()
	{
		// https://stackoverflow.com/questions/1600962/displaying-the-build-date
		var buildVer = typeof(Program).Assembly.GetName().Version?.ToString() ?? "??";
		var compileTime = new DateTime(Builtin.CompileTime, DateTimeKind.Utc); // Use a different method maybe

		Console.WriteLine($"\u001b[36m ---- QuickEdit ver. {buildVer} - Build Date: {compileTime.ToUniversalTime()} UTC - By HEJOK254 ---- \u001b[0m");
	}
}