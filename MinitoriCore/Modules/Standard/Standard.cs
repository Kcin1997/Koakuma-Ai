using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace MinitoriCore.Modules.Standard
{
    public class Standard : ModuleBase
    {
        private RandomStrings strings;

        private EventStorage events;

        public Standard(RandomStrings _strings, EventStorage _events)
        {
            strings = _strings;
            events = _events;
        }

        private RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private int RandomInteger(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) *
                (scale / (double)uint.MaxValue));
        }

        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            await ReplyAsync($"Blah to you too, {Context.User.Mention}.");
        }

        [Command("quit")]
        [Priority(1000)]
        public async Task ShutDown()
        {
            if (Context.User.Id != 102528327251656704)
            {
                await ReplyAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            events.Save();

            Task.Run(async () =>
            {
                await ReplyAsync("rip");
                //await Task.Delay(500);
                await ((DiscordSocketClient)Context.Client).LogoutAsync();
                Environment.Exit(0);
            });
        }

        // Non-testing 132106771975110656
        // Full mute 132106637614776320
        // Unverified 318748748010487808

        // No emoji 241256979840892939
        // No embed 178823209217556480

        [Command("mute")]
        [Summary("Place a non-testing mute on a bot")]
        public async Task NormalMute([Remainder]string remainder = "")
        {
            if (Context.Guild.Id != 110373943822540800)
                return;

            if (((IGuildUser)Context.User).RoleIds.ToList().Contains(407326634819977217) || 
                ((IGuildUser)Context.User).RoleIds.ToList().Contains(113379036524212224))
            {
                var args = remainder.Split(' ').Where(x => x.Length > 0).ToList();
                string reason = "";
                var users = new List<ulong>();
                //((SocketGuild)Context.Guild).GetUser

                int i = 0;
                foreach (var s in new List<string>(args))
                {
                    var id = s.TrimStart('<').TrimStart('@').TrimEnd('>');
                    ulong temp;
                    if (ulong.TryParse(id, out temp))
                    {
                        users.Add(temp);
                        args.RemoveAt(i);
                        i++;
                    }
                    else
                        break;
                }

                if (users.Count() == 0)
                {
                    await ReplyAsync("You need to mention something for this to work!");
                    return;
                }

                reason = $"[ Mute by {Context.User.Username}#{Context.User.Discriminator} ]{string.Join(" ", args)}";

                StringBuilder output = new StringBuilder();
                output.AppendLine("Non-testing muted the following bots:");

                int mutedUsers = 0;
                var role = Context.Guild.GetRole(132106771975110656);

                foreach (var u in users)
                {
                    var user = await Context.Guild.GetUserAsync(u);

                    if (user == null)
                        continue;

                    if (!user.IsBot)
                        continue;

                    mutedUsers++;

                    // TODO: Add a catch
                    await user.AddRoleAsync(role, new RequestOptions() { AuditLogReason = reason });
                    output.Append($"**{user.Username}#{user.Discriminator}**, ");
                }

                if (mutedUsers == 0)
                {
                    await ReplyAsync("None of those are bots!");
                    return;
                }

                await ReplyAsync(output.ToString().Trim().TrimEnd(','));
            }
        }

        [Command("unsnow")]
        [Summary("Leave the snowball fight")]
        public async Task UnSnowball()
        {
            var snowballRoles = Context.Guild.Roles.Count(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            if (snowballRoles == 0)
            {
                await ReplyAsync("I don't see a role named `Snowball`, make one and try again.");
                return;
            }
            else if (snowballRoles > 1)
            {
                await ReplyAsync("There are too many roles named `Snowball`, rename some and try again.");
                return;
            }

            var role = Context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));
        }

        [Command("snowball")]
        [Summary("Throw snowballs at people! Build an army!")]
        public async Task Snowball([Remainder]string remainder = "")
        {
            if (Context.Guild == null)
            {
                await ReplyAsync("You can't use this in DMs!");
                return;
            }
            
            var snowballRoles = Context.Guild.Roles.Count(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            if (snowballRoles == 0)
            {
                await ReplyAsync("I don't see a role named `Snowball`, make one and try again.");
                return;
            }
            else if (snowballRoles > 1)
            {
                await ReplyAsync("There are too many roles named `Snowball`, rename some and try again.");
                return;
            }

            var role = Context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            IGuildUser user = null;
            string message = ""; // store the message later to be intelligent about when to yell at them for having the role or not

            if (Context.Message.MentionedUserIds.Count() == 1)
            {
                if (Context.Message.MentionedUserIds.FirstOrDefault() != ((SocketGuild)Context.Guild).CurrentUser.Id)
                    user = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.FirstOrDefault());
                else
                {
                    message = "Hey, you sure you want to throw snowballs at your supplier";
                }
            }
            else if (Context.Message.MentionedUserIds.Count() > 1)
            {
                foreach (var u in Context.Message.MentionedUserIds)
                {
                    if (u == ((SocketGuild)Context.Guild).CurrentUser.Id)
                        continue;

                    user = await Context.Guild.GetUserAsync(u);

                    break;
                }

                if (user == null)
                {
                    message = "Nope, still can't throw snowballs at me";
                }
            }
            else
            {
                await ReplyAsync("You need to pick someone to throw a snowball at!");
                return;
            }
            
            if (((IGuildUser)Context.User).RoleIds.ToList().Contains(role.Id))
            {
                if (message != "")
                {
                    await ReplyAsync(message);
                    return;
                }

                if (!events.cooldown.ContainsKey(Context.Guild.Id))
                    events.cooldown[Context.Guild.Id] = new Dictionary<ulong, DateTime>();

                if (events.cooldown[Context.Guild.Id].ContainsKey(Context.User.Id) && events.cooldown[Context.Guild.Id][Context.User.Id] > DateTime.UtcNow.AddSeconds(-15))
                {
                    TimeSpan t = events.cooldown[Context.Guild.Id][Context.User.Id] - DateTime.UtcNow.AddSeconds(-15);
                    await ReplyAsync($"You're still making another snowball! You'll be ready in {t.Seconds:00} seconds.");
                    return;
                }

                events.cooldown[Context.Guild.Id][Context.User.Id] = DateTime.UtcNow;

                if (!events.stats.ContainsKey(Context.Guild.Id))
                    events.stats[Context.Guild.Id] = new Dictionary<ulong, SnowballStats>();

                if (!events.stats[Context.Guild.Id].ContainsKey(Context.User.Id))
                    events.stats[Context.Guild.Id][Context.User.Id] = new SnowballStats();

                if (!events.stats[Context.Guild.Id].ContainsKey(user.Id))
                    events.stats[Context.Guild.Id][user.Id] = new SnowballStats();

                if (user.Id == Context.User.Id)
                {
                    events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                    events.cooldown[Context.Guild.Id][Context.User.Id] = DateTime.UtcNow.AddSeconds(40);
                    await ReplyAsync($"{Context.User.Mention} attempted to throw a snowball at {Context.User.Mention}, but all they managed to do is fall over and lose their snowball.");
                    return;
                }

                if (!user.RoleIds.ToList().Contains(role.Id))
                {
                    // Add the role. 100% chance to hit
                    try
                    {
                        await user.AddRoleAsync(role);
                    }
                    catch (Exception ex)
                    {
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Dodged++;
                        await ReplyAsync($"The snowball sailed right through {user.Username}! Wait, what?\ni probably don't have the manage roles permission! `{ex.Message}`");
                        return;
                    }

                    events.stats[Context.Guild.Id][Context.User.Id].Hits++;
                    events.stats[Context.Guild.Id][user.Id].Downed++;
                    await ReplyAsync($"{Context.User.Mention} threw a snowball at {user.Mention}!");
                    return;
                }
                else
                {
                    Random rand = new Random((int)(Context.User.Id + user.Id + Convert.ToUInt64(DateTime.Now.Ticks)));

                    var chance = rand.Next(1, 101);

                    if (chance >= 1 && chance <= 65)
                    {
                        // hit
                        events.stats[Context.Guild.Id][Context.User.Id].Hits++;
                        events.stats[Context.Guild.Id][user.Id].Downed++;

                        await ReplyAsync($"{Context.User.Mention} threw a snowball at {user.Mention}!");
                    }
                    else if (chance >= 66 && chance <= 90)
                    {
                        // miss
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Dodged++;

                        await ReplyAsync($"{Context.User.Mention} threw a snowball at {user.Mention}, but it missed!");
                    }
                    else if (chance >= 91 && chance <= 100)
                    {
                        // caught
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Caught++;

                        events.cooldown[Context.Guild.Id][user.Id] = DateTime.UtcNow.AddMinutes(-10);

                        await ReplyAsync($"{Context.User.Mention} threw a snowball at {user.Mention}, but {user.Mention} caught it!");
                    }
                }
            }
            else
            {
                await ReplyAsync("No one has thrown any snowballs your way yet, so you don't have the Snowball role yet.");
                return;
            }
        }

        [Command("throw")]
        [Summary("Beat people with objects!")]
        public async Task Throw([Remainder]string remainder = "")
        {
            IGuildUser user = null;

            if (Context.Message.MentionedUserIds.Count() == 1)
            {
                if (Context.Message.MentionedUserIds.FirstOrDefault() == ((SocketGuild)Context.Guild).CurrentUser.Id)
                    user = (IGuildUser)Context.Message.Author;
                else
                    user = await Context.Guild.GetUserAsync(Context.Message.MentionedUserIds.FirstOrDefault());
            }
            else if (Context.Message.MentionedUserIds.Count() > 1)
            {
                foreach (var u in Context.Message.MentionedUserIds)
                {
                    if (u == ((SocketGuild)Context.Guild).CurrentUser.Id)
                        continue;

                    user = await Context.Guild.GetUserAsync(u);

                    break;
                }

                if (user == null)
                    user = (IGuildUser)Context.Message.Author;
            }
            else
                user = (IGuildUser)Context.User;

            int count = strings.RandomInteger(0, 100);
            string objects = "a horrible error that should never happen";

            if (count < 60)
                objects = strings.objects[strings.RandomInteger(0, strings.objects.Length)];
            else if (count > 60 && count < 85)
                objects = $"{strings.objects[strings.RandomInteger(0, strings.objects.Length)]} " +
                    $"and {strings.objects[strings.RandomInteger(0, strings.objects.Length)]}";
            else if (count > 85)
                objects = $"{strings.objects[strings.RandomInteger(0, strings.objects.Length)]}, " +
                    $"{strings.objects[strings.RandomInteger(0, strings.objects.Length)]}, " +
                    $"and {strings.objects[strings.RandomInteger(0, strings.objects.Length)]}";

            await ReplyAsync($"*throws {objects} at {user.Mention}*");
        }

        [Command("choose")]
        [Alias("choice")]
        [Summary("Let the bot decide for you!")]
        public async Task Choose([Remainder]string remainder = "")
        {
            string[] choices = remainder.Split(';').Where(x => x.Trim().Length > 0).ToArray();
            
            if (choices[0] != "")
                await ReplyAsync($"I choose **{choices[RandomInteger(0, choices.Length)].Trim()}**");
            else
                await ReplyAsync("What do you want me to do with this?");
        }
    }

    

#region bot list classes
    public class BotListing
    {
        public ulong user_id { get; set; }
        public string name { get; set; }
        public List<ulong> owner_ids { get; set; }
        public string prefix { get; set; }
        public string Error { get; set; }
    }


    public class SearchResults
    {
        public int total_results { get; set; }
        public string analytics_id { get; set; }
        public List<List<Message>> messages { get; set; }
    }

    public class Author
    {
        public string username { get; set; }
        public string discriminator { get; set; }
        public bool bot { get; set; }
        public string id { get; set; }
        public object avatar { get; set; }
    }

    public class Mention
    {
        public string username { get; set; }
        public string discriminator { get; set; }
        public string id { get; set; }
        public string avatar { get; set; }
        public bool? bot { get; set; }
    }

    public class Message
    {
        public List<object> attachments { get; set; }
        public bool tts { get; set; }
        public List<object> embeds { get; set; }
        public string timestamp { get; set; }
        public bool mention_everyone { get; set; }
        public string id { get; set; }
        public bool pinned { get; set; }
        public object edited_timestamp { get; set; }
        public Author author { get; set; }
        public List<string> mention_roles { get; set; }
        public string content { get; set; }
        public string channel_id { get; set; }
        public List<Mention> mentions { get; set; }
        public int type { get; set; }
        public bool? hit { get; set; }
    }
#endregion
}
