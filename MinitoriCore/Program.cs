using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Rest;
using Discord.Commands;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
//using MinitoriCore.Modules.UptimeTracker;
using MinitoriCore.Modules.Standard;
using MinitoriCore.Modules.Splatoon;
using System.Threading;
using System.Runtime.InteropServices.ComTypes;

namespace MinitoriCore
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient socketClient;
        private DiscordRestClient restClient;
        private Config config;
        private CommandHandler handler;
        private RandomStrings strings;
        //private UptimeService uptime;
        private EventStorage events;
        private RankedService rankedService;
        private ServiceProvider map;
        //private IServiceProvider services;
        //private readonly IDependencyMap map = new DependencyMap();
        //private readonly CommandService commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });
        private ulong updateChannel = 0;

        private Dictionary<ulong, int> posCommandUsage = new Dictionary<ulong, int>();

        private async Task RunAsync()
        {
            socketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true
            });
            socketClient.Log += Log;

            restClient = new DiscordRestClient(new DiscordRestConfig
            {
                LogLevel = LogSeverity.Verbose
            });
            restClient.Log += Log;

            if (File.Exists("./update"))
            {
                var temp = File.ReadAllText("./update");
                ulong.TryParse(temp, out updateChannel);
                File.Delete("./update");
                Console.WriteLine($"Found an update file! It contained [{temp}] and we got [{updateChannel}] from it!");
            }

            config = Config.Load();
            //uptime = new UptimeService();
            events = EventStorage.Load();
            rankedService = new RankedService();
            strings = new RandomStrings();

            //var map = new DependencyMap();
            map = new ServiceCollection().AddSingleton(socketClient).AddSingleton(config).AddSingleton(strings).AddSingleton(events).AddSingleton(rankedService)/*.AddSingleton(uptime)*/.BuildServiceProvider();

            //await ConfigureServicesAsync(map);

            await socketClient.LoginAsync(TokenType.Bot, config.Token);
            await socketClient.StartAsync();

            await restClient.LoginAsync(TokenType.Bot, config.Token);

            if (File.Exists("./deadlock"))
            {
                Console.WriteLine("We're recovering from a deadlock.");
                File.Delete("./deadlock");
                foreach (var u in config.OwnerIds)
                {
                    (await restClient.GetUserAsync(u))?
                        .SendMessageAsync($"I recovered from a deadlock.\n`{DateTime.Now.ToShortDateString()}` `{DateTime.Now.ToLongTimeString()}`");
                }
            }

            socketClient.GuildAvailable += Client_GuildAvailable;
            socketClient.Disconnected += SocketClient_Disconnected;
            socketClient.MessageReceived += SocketClient_MessageReceived;
            socketClient.ReactionAdded += SocketClient_ReactionAdded;
            //client.GuildMemberUpdated += Client_UserUpdated;
            // memes

            //await uptime.Install(map);

            socketClient.UserJoined += Client_UserJoined;

            handler = new CommandHandler();
            await handler.Install(map);

            //Task.Run(async () =>
            //{
            //    await Task.Delay(1000 * 60); // wait a minute before downloading to ensure we have access to the server
            //    await client.DownloadUsersAsync(new IGuild[] { client.GetGuild(110373943822540800) });
            //    var role = client.GetGuild(110373943822540800).GetRole(110374777914417152);

            //    while (true)
            //    {
            //        foreach (var u in client.GetGuild(110373943822540800).Users.Where(x => x?.IsBot == true))
            //        {
            //            if (!u.Roles.Contains(role))
            //            {
            //                await u.AddRoleAsync(role);
            //            }
            //        }

            //        await Task.Delay(1000 * 60 * 30); // Wait 30 minutes
            //    }
            //});

            //await client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(File.OpenRead("Minitori.png")));

            await Task.Delay(-1);
        }

        private async Task SocketClient_ReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var guildChannel = channel as IGuildChannel;
            if (guildChannel == null)
                return;
            else if (!config.SelfStarPreventionServers.Contains(guildChannel.GuildId))
                return;

            if (reaction.Emote.Name != "star")
                return;

            IUserMessage msg;

            if (!cache.HasValue)
            {
                msg = (IUserMessage)await channel.GetMessageAsync(cache.Id);
            }
            else
                msg = cache.Value;

            if (msg.Author.Id == reaction.UserId)
            {
                try
                {
                    await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    await channel.SendMessageAsync($"Prevented self-star by {reaction.User.Value.Mention} *(msg: `{msg.Id}`)*");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to remove self-star in {guildChannel.GuildId}");
                }
            }
        }

        private async Task SocketClient_MessageReceived(SocketMessage msg)
        {
            if (msg.Author.Id == socketClient.CurrentUser.Id)
                return;

            var channel = msg.Channel as IGuildChannel;

            if (channel == null)
                return;

            if (channel.Guild.Id != 110373943822540800)
                return;

            // basically every channel that isn't #general or a testing channel in dbots
            if (channel.Id == 468690756899438603 || channel.Id == 110374153562886144 || channel.Id == 631313666851078145 || channel.Id == 520832612739186688 || channel.Id == 715318925281460235)
            {
                var author = msg.Author as SocketGuildUser;

                if (msg.Content.Contains("pos "))
                {
                    await msg.DeleteAsync();

                    if (!posCommandUsage.ContainsKey(author.Id))
                        posCommandUsage[author.Id] = 0;

                    posCommandUsage[author.Id]++;

                    switch (posCommandUsage[author.Id])
                    {
                        case 1:
                        case 2:
                            Task.Run(async () =>
                            {
                                var response = await msg.Channel.SendMessageAsync($"{author.Mention} please use a testing channel to check on the status of your bot.");
                                await Task.Delay(10 * 1000);
                                await response.DeleteAsync();
                            });
                            break;
                        case 3:
                            var nonTestingMute = channel.Guild.GetRole(132106771975110656);
                            await author.AddRoleAsync(nonTestingMute, new RequestOptions { AuditLogReason = "Did not use testing channels to check on the status of their bot." });
                            break;
                    }
                }
                else if (author.Id == 241930933962407936)
                {
                    if (msg.Content == "That bot isn't in this guild." || msg.Content == "That bot isn't part of the queue." 
                        || msg.Content == "That bot isn't in this guild." || msg.Content.Contains("in the verification queue."))
                        await msg.DeleteAsync();
                }
            }

            if (msg.Author != null && msg.Author.IsBot && (msg.Author as SocketGuildUser).JoinedAt > DateTimeOffset.Now.AddSeconds(-15))
            {
                await Task.Delay(250);
                await msg.DeleteAsync();
                var logChannel = await channel.Guild.GetChannelAsync(467192652463341578) as SocketTextChannel;
                EmbedBuilder builder = new EmbedBuilder();
                await logChannel.SendMessageAsync($"`[{DateTimeOffset.Now.ToString("HH:mm:ss")}]` Deleted a join message from the bot **{msg.Author.Username}**{msg.Author.Discriminator} (ID:{msg.Author.Id}):", 
                    embed: builder.WithAuthor(msg.Author).WithCurrentTimestamp().WithTitle("Deleted Message").WithDescription(msg.Content).Build());
                return;
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

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            if (user.Username.ToLower().Contains("platinbots") || user.Username.ToLower().Contains("botsplat") || 
                user.Username.ToLower().Contains("discord.gg/") ||
                (user.Username.ToLower().Contains("twitch") && user.Username.ToLower().Contains("tv") && user.Username.ToLower().Contains("binzy")) ||
                (user.Username.ToLower().Contains("twitter") && user.Username.ToLower().Contains(".com") && user.Username.ToLower().Contains("senseibin"))
                )
            {
                await user.Guild.AddBanAsync(user.Id, reason: "Userbot/Adbot");
                return;
            }

            //if (user.Guild.Id == 110373943822540800 && user.IsBot)
            //{
            //    await Task.Delay(2500);
            //    var roles = new IRole[] { user.Guild.GetRole(318748748010487808), user.Guild.GetRole(110374777914417152) };
            //    await user.AddRolesAsync(roles);
            //}
        }

        private async Task Client_GuildAvailable(SocketGuild guild)
        {
            if (updateChannel != 0 && guild.GetTextChannel(updateChannel) != null)
            {
                await Task.Delay(3000); // wait 3 seconds just to ensure we can actually send it. this might not do anything.
                await guild.GetTextChannel(updateChannel).SendMessageAsync("aaaaaand we're back.");
                updateChannel = 0;
            }
        }

        private async Task SocketClient_Disconnected(Exception ex)
        {
            Console.WriteLine("!!!Disconnected Event Fired!!!");
            // If we disconnect, wait 3 minutes and see if we regained the connection.
            // If we did, great, exit out and continue. If not, check again 3 minutes later
            // just to be safe, and restart to exit a deadlock.
            var task = Task.Run(async () =>
            {
                Console.WriteLine("!!!Disconnected Task Started!!!");
                for (int i = 0; i < 2; i++)
                {
                    Console.WriteLine("!!!Disconnected Timer Started!!!");
                    await Task.Delay(1000 * 60 * 3);
                    Console.WriteLine("!!!Disconnected Timer Finished!!!");
                    
                    if (socketClient.ConnectionState == ConnectionState.Connected)
                    {
                        Console.WriteLine("!!!Connection Regained!!!");
                        break;
                    }
                    else if (i == 1)
                    {
                        Console.WriteLine("!!!Still Disconnected!!!");
                        File.Create("./deadlock");
                        Environment.Exit((int)ExitCodes.ExitCode.DeadlockEscape);
                    }
                }
            });
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
