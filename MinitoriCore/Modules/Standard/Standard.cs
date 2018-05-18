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

        [Command("setnick")]
        [Summary("Change my nickname!")]
        [RequireOwner()]
        public async Task SetNickname(string Nick = "")
        {
            await (Context.Guild as SocketGuild).CurrentUser.ModifyAsync(x => x.Nickname = Nick);
            await ReplyAsync(":thumbsup:");
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

        [Command("joined")]
        public async Task GetJoinDates([Remainder]string blah)
        {
            StringBuilder output = new StringBuilder();

            foreach (var u in Context.Message.MentionedUserIds)
            {
                var user = ((SocketGuild)Context.Guild).GetUser(u);
                output.AppendLine($"{user.Username} - `{user.JoinedAt.Value.ToLocalTime().ToString("d")} {user.JoinedAt.Value.ToLocalTime().ToString("T")}`");
            }

            await ReplyAsync(output.ToString());
        }

        [Command("listroles")]
        public async Task ListRoles([Remainder]string role)
        {
            if (role.Length > 0)
            {
                var r = Context.Guild.Roles.FirstOrDefault(x => x.Name == role);
                if (r != null)
                    await ReplyAsync($"```{r.Id} | {r.Name}```");
                else
                    await ReplyAsync($"I can't find a role named `{role}`!");

                return;
            }

            StringBuilder output = new StringBuilder();
            output.Append("```");

            foreach (var r in Context.Guild.Roles)
            {
                output.AppendLine($"{r.Id} | {r.Name}");
            }

            output.Append("```");

            await ReplyAsync(output.ToString());
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

            if (user.Id == 102528327251656704) // Googie2149
                user = (IGuildUser)Context.User;

            if (user.Id == 396277750664527872)
                return; // made me stay up late to break things
            
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

        [Command("snowball stats")]
        [Summary("Get stats for the snowball fight!")]
        [Priority(1000)]
        public async Task Stats([Remainder]string mentions = "")
        {
            //Task.Run(async () =>
            //{
                StringBuilder output = new StringBuilder();
                //output.Append("```");
                
                int spaces = 0;

                if (Context.Message.MentionedUserIds.Count() > 0)
                {
                    spaces = Context.Message.MentionedUserIds.Select(x => ((SocketGuild)Context.Guild).GetUser(x)?.Username).OrderByDescending(x => x?.Length).FirstOrDefault().Length;

                    foreach (var kv in events.stats[Context.Guild.Id].OrderByDescending(x => x.Value.Hits).Where(x => Context.Message.MentionedUserIds.Contains(x.Key)))
                    {
                        var user = (await Context.Guild.GetUserAsync(kv.Key));
                        var name = user == null ? kv.Key.ToString() : user.Username;

                        output.AppendLine($"{name}{new string(' ', spaces - name.Length)} | {kv.Value.Hits.ToString("00")} Hits | {kv.Value.Misses.ToString("00")} Missed | " +
                            $"{kv.Value.Dodged.ToString("00")} Dodged | {kv.Value.Caught.ToString("00")} Caught");
                    }
                }
                else
                {
                    spaces = events.stats[Context.Guild.Id].Select(x => ((SocketGuild)Context.Guild).GetUser(x.Key)?.Username).OrderByDescending(x => x?.Length).FirstOrDefault().Length;

                    foreach (var kv in events.stats[Context.Guild.Id].OrderByDescending(x => x.Value.Hits))
                    {
                        var user = (await Context.Guild.GetUserAsync(kv.Key));
                        var name = user == null ? kv.Key.ToString() : user.Username;

                        output.AppendLine($"{name}{new string(' ', spaces - name.Length)} | {kv.Value.Hits.ToString("000")} Hits | {kv.Value.Misses.ToString("000")} Missed | " +
                            $"{kv.Value.Dodged.ToString("000")} Dodged | {kv.Value.Caught.ToString("000")} Caught");
                    }
                }

                //output.Append("```");

                if (output.Length > 2000)
                {
                    using (var stream = new MemoryStream())
                    {
                        var writer = new StreamWriter(stream);
                        writer.Write(output.ToString());
                        writer.Flush();
                        stream.Position = 0;
                        await Context.Channel.SendFileAsync(stream, "snowball_stats.txt", $"```{string.Join('\n', output.ToString().Split('\n').Take(10))}```");
                    }
                }
                else
                    await ReplyAsync(output.ToString());
            //});
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

            if (user.Id == 102528327251656704) // Googie2149
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
