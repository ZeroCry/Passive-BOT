﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using PassiveBOT.Configuration;
using PassiveBOT.Handlers;
using PassiveBOT.Handlers.Services;
using PassiveBOT.Handlers.Services.Interactive;
using Color = System.Drawing.Color;
using EventHandler = PassiveBOT.Handlers.EventHandler;

namespace PassiveBOT
{
    public class Program
    {
        public static DiscordSocketClient Client;

        private CommandHandler _handler;

        public static void Main(string[] args)
        {
            try
            {
                new Program().Start().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //Console.ReadKey();
            }
        }

        public async Task Start()
        {
            Console.Title = $"PassiveBOT";
            Console.WriteLine(
                "██████╗  █████╗ ███████╗███████╗██╗██╗   ██╗███████╗██████╗  ██████╗ ████████╗\n" +
                "██╔══██╗██╔══██╗██╔════╝██╔════╝██║██║   ██║██╔════╝██╔══██╗██╔═══██╗╚══██╔══╝\n" +
                "██████╔╝███████║███████╗███████╗██║██║   ██║█████╗  ██████╔╝██║   ██║   ██║   \n" +
                "██╔═══╝ ██╔══██║╚════██║╚════██║██║╚██╗ ██╔╝██╔══╝  ██╔══██╗██║   ██║   ██║   \n" +
                "██║     ██║  ██║███████║███████║██║ ╚████╔╝ ███████╗██████╔╝╚██████╔╝   ██║   \n" +
                "╚═╝     ╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═══╝  ╚══════╝╚═════╝  ╚═════╝    ╚═╝   \n" +
                "/--------------------------------------------------------------------------\\ \n" +
                "| Designed by PassiveModding - PassiveNation.com  ||   Status: Connected   | \n" +
                "\\--------------------------------------------------------------------------/ \n");

            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/config/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/config/"));
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/server/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/server/"));
            //if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "setup/config/home.json")))
            //    File.Create(Path.Combine(AppContext.BaseDirectory, "setup/config/home.json")).Dispose();
            Config.CheckExistence();
            Homeserver.CheckExistence();
            Tokens.CheckExistence();
            var debug = Config.Load().Debug.ToUpper();
            var token = Config.Load().Token;

            var ll = LogSeverity.Info;
            switch (debug)
            {
                case "Y":
                    ll = LogSeverity.Debug;
                    break;
                case "N":
                    ll = LogSeverity.Info;
                    break;
                default:
                    await LogHandler.In1Run($"Error Loading Debug Config, Set to default (Entry = {debug})");
                    break;
            }


            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = ll,
                MessageCacheSize = 50
            });

            try
            {
                await Client.LoginAsync(TokenType.Bot, token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                await LogHandler.In1Run("Token was rejected by Discord (Invalid Token or Connection Error)\n" +
                                        $"{e}");
            }

            var serviceProvider = ConfigureServices();
            _handler = new CommandHandler(serviceProvider);
            var _ = new EventHandler(serviceProvider);
            await _handler.ConfigureAsync();

            //checks if the user wants to log debug info or not
            if (ll == LogSeverity.Debug)
                Client.Log += LogDebug;
            else
                Client.Log += LogMessageInfo;

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(new InteractiveService(Client))
                .AddSingleton(new TimerService(Client))
                .AddSingleton(new CommandService(
                    new CommandServiceConfig {CaseSensitiveCommands = false, ThrowOnError = false}));
            return services.BuildServiceProvider();
        }

        public static Task LogMessageInfo(LogMessage message)
        {
            var messagestr = message.ToString();
            if (message.ToString().StartsWith("Unknown OpCode (8)") ||
                message.ToString().Contains("VOICE_STATE_UPDATE"))
                return Task.CompletedTask;
            var msg = messagestr.Substring(21, messagestr.Length - 21);
            LogHandler.In2("PassiveBOT", '?', $"{msg}", Color.Chartreuse);
            return Task.CompletedTask;
        }

        public static Task LogDebug(LogMessage msg)
        {
            LogHandler.Debug(msg.ToString());
            return Task.CompletedTask;
        }
    }
}