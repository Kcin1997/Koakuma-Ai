using System;
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
using MinitoriCore.Modules.Standard;

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
        private EventStorage events;
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
            events = EventStorage.Load();
            strings = new RandomStrings();

            //var map = new DependencyMap();
            map = new ServiceCollection().AddSingleton(client).AddSingleton(config).AddSingleton(strings).AddSingleton(events)/*.AddSingleton(uptime)*/.BuildServiceProvider();

            //await ConfigureServicesAsync(map);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            client.GuildAvailable += Client_GuildAvailable;
            //client.GuildMemberUpdated += Client_UserUpdated;
            // memes

            //await uptime.Install(map);

            client.UserJoined += Client_UserJoined;
            client.MessageReceived += Client_MessageReceived;

            handler = new CommandHandler();
            await handler.Install(map);

            Task.Run(async () =>
            {
                await Task.Delay(1000 * 60); // wait a minute before downloading to ensure we have access to the server
                await client.DownloadUsersAsync(new IGuild[] { client.GetGuild(110373943822540800) });
                var role = client.GetGuild(110373943822540800).GetRole(110374777914417152);

                while (true)
                {
                    foreach (var u in client.GetGuild(110373943822540800).Users.Where(x => x?.IsBot == true))
                    {
                        if (!u.Roles.Contains(role))
                        {
                            await u.AddRoleAsync(role);
                        }
                    }

                    await Task.Delay(1000 * 60 * 30); // Wait 30 minutes
                }
            });

            //await client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(File.OpenRead("Minitori.png")));

            await Task.Delay(-1);
        }

        private async Task Client_MessageReceived(SocketMessage msg)
        {
            if (((IGuildChannel)msg.Channel).GuildId != 110373943822540800)
                return;
            
            if (msg.Content.ToLower().StartsWith(".iam emotes"))
            {
                await ((IGuildChannel)msg.Channel).Guild.AddBanAsync(msg.Author, 1, "Spam prevention");
            }
        }

        //private async Task Client_UserUpdated(SocketGuildUser before, SocketGuildUser after)
        //{
        //    if (((SocketGuildUser)before).Guild.Id != 110373943822540800)
        //        return;

        //    if (before.Id == 190544080164487168 && ((SocketGuildUser)before).Roles.Count() != ((SocketGuildUser)after).Roles.Count())
        //    {
        //        var testMute = ((SocketGuildUser)after).Guild.GetRole(132106771975110656);
        //        var superMute = ((SocketGuildUser)after).Guild.GetRole(132106637614776320);

        //        if (((SocketGuildUser)after).Roles.Contains(superMute) || ((SocketGuildUser)after).Roles.Contains(testMute))
        //        {
        //            await Task.Delay(200);
        //            await ((SocketGuildUser)after).RemoveRolesAsync(new IRole[] { testMute, superMute });
        //        }
        //    }
        //}

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
            if (user.Guild.Id == 110373943822540800 && user.IsBot)
            {
                await Task.Delay(1500);
                var roles = new IRole[] { user.Guild.GetRole(318748748010487808), user.Guild.GetRole(110374777914417152) };
                await user.AddRolesAsync(roles);
            }
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
