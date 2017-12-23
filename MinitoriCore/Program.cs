﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using MinitoriCore.Modules.UptimeTracker;

namespace MinitoriCore
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private Config config;
        private CommandHandler handler;
        private RandomStrings strings;
        //private UptimeService uptime;
        private ServiceProvider map;
        //private IServiceProvider services;
        //private readonly IDependencyMap map = new DependencyMap();
        //private readonly CommandService commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });

        private async Task RunAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });
            client.Log += Log;

            config = Config.Load();
            //uptime = new UptimeService();
            strings = new RandomStrings();

            //var map = new DependencyMap();
            map = new ServiceCollection().AddSingleton(client).AddSingleton(config).AddSingleton(strings)/*.AddSingleton(uptime)*/.BuildServiceProvider();

            //await ConfigureServicesAsync(map);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            client.GuildAvailable += Client_GuildAvailable;

            //await uptime.Install(map);
            
            client.UserJoined += Client_UserJoined;

            handler = new CommandHandler();
            await handler.Install(map);

            //await client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(File.OpenRead("Minitori.png")));

            await Task.Delay(-1);
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            if (guild.Id != 110373943822540800)
                return;

            //if (uptime.CheckInstalled())
            //    return;

            //uptime.Install(map);
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            await Task.Delay(500);

            if (user.Guild.Id == 110373943822540800 && user.IsBot)
                await user.AddRoleAsync(user.Guild.GetRole(318748748010487808));
        }

        private Task Log(LogMessage msg)
        {
            //Console.WriteLine(msg.ToString());

            //Color
            ConsoleColor color;
            switch (msg.Severity)
            {
                case LogSeverity.Error: color = ConsoleColor.Red; break;
                case LogSeverity.Warning: color = ConsoleColor.Yellow; break;
                case LogSeverity.Info: color = ConsoleColor.White; break;
                case LogSeverity.Verbose: color = ConsoleColor.Gray; break;
                case LogSeverity.Debug: default: color = ConsoleColor.DarkGray; break;
            }

            //Exception
            string exMessage;
            Exception ex = msg.Exception;
            if (ex != null)
            {
                while (ex is AggregateException && ex.InnerException != null)
                    ex = ex.InnerException;
                exMessage = $"{ex.Message}";
                if (exMessage != "Reconnect failed: HTTP/1.1 503 Service Unavailable")
                    exMessage += $"\n{ex.StackTrace}";
            }
            else
                exMessage = null;

            //Source
            string sourceName = msg.Source?.ToString();

            //Text
            string text;
            if (msg.Message == null)
            {
                text = exMessage ?? "";
                exMessage = null;
            }
            else
                text = msg.Message;

            //if (text.Contains("GUILD_UPDATE: ") && text.Contains("UTC"))
            //    return Task.CompletedTask;
            //else if (text.StartsWith("CHANNEL_UPDATE: "))
            //    return Task.CompletedTask;

            if (sourceName == "Command")
                color = ConsoleColor.Cyan;
            else if (sourceName == "<<Message")
                color = ConsoleColor.Green;
            else if (sourceName == ">>Message")
                return Task.CompletedTask;

            //Build message
            StringBuilder builder = new StringBuilder(text.Length + (sourceName?.Length ?? 0) + (exMessage?.Length ?? 0) + 5);
            if (sourceName != null)
            {
                builder.Append('[');
                builder.Append(sourceName);
                builder.Append("] ");
            }
            builder.Append($"[{DateTime.Now.ToString("d")} {DateTime.Now.ToString("T")}] ");
            for (int i = 0; i < text.Length; i++)
            {
                //Strip control chars
                char c = text[i];
                if (c == '\n' || !char.IsControl(c) || c != (char)8226)
                    builder.Append(c);
            }
            if (exMessage != null)
            {
                builder.Append(": ");
                builder.Append(exMessage);
            }

            text = builder.ToString();
            //if (msg.Severity <= LogSeverity.Info)
            //{
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            //}
#if DEBUG
            System.Diagnostics.Debug.WriteLine(text);
#endif



            return Task.CompletedTask;
        }
    }
}
