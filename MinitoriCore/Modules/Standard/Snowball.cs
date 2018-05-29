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
using MinitoriCore.Preconditions;

namespace MinitoriCore.Modules.Standard
{
    class Snowball : MinitoriModule
    {
        private EventStorage events;
        private Dictionary<ulong, bool> rotate = new Dictionary<ulong, bool>();

        public Snowball(EventStorage _events)
        {
            events = _events;
        }

        [Command("unsnow")]
        [Summary("Leave the snowball fight")]
        public async Task UnSnowball()
        {
            var snowballRoles = Context.Guild.Roles.Count(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            if (snowballRoles == 0)
            {
                await RespondAsync("I don't see a role named `Snowball`, make one and try again.");
                return;
            }
            else if (snowballRoles > 1)
            {
                await RespondAsync("There are too many roles named `Snowball`, rename some and try again.");
                return;
            }

            var role = Context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));
        }

        [Command("snowball")]
        [Summary("Throw snowballs at people! Build an army!")]
        public async Task ThrowSnowball([Remainder]string remainder = "")
        {
            if (Context.Guild == null)
            {
                await RespondAsync("You can't use this in DMs!");
                return;
            }

            var snowballRoles = Context.Guild.Roles.Count(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            if (snowballRoles == 0)
            {
                await RespondAsync("I don't see a role named `Snowball`, make one and try again.");
                return;
            }
            else if (snowballRoles > 1)
            {
                await RespondAsync("There are too many roles named `Snowball`, rename some and try again.");
                return;
            }

            var role = Context.Guild.Roles.FirstOrDefault(x => string.Equals(x.Name, "SNOWBALL", StringComparison.OrdinalIgnoreCase));

            IGuildUser user = null;
            string message = ""; // store the message later to be intelligent about when to yell at them for having the role or not

            if (Context.Message.MentionedUserIds.Count() == 1)
            {
                if (Context.Message.MentionedUserIds.FirstOrDefault() != ((SocketGuild)Context.Guild).CurrentUser.Id)
                    user = Context.Guild.GetUser(Context.Message.MentionedUserIds.FirstOrDefault());
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

                    user = Context.Guild.GetUser(u);

                    break;
                }

                if (user == null)
                {
                    message = "Nope, still can't throw snowballs at me";
                }
            }
            else
            {
                await RespondAsync("You need to pick someone to throw a snowball at!");
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
                    await RespondAsync(message);
                    return;
                }

                if (!events.cooldown.ContainsKey(Context.Guild.Id))
                    events.cooldown[Context.Guild.Id] = new Dictionary<ulong, DateTime>();

                if (events.cooldown[Context.Guild.Id].ContainsKey(Context.User.Id) && events.cooldown[Context.Guild.Id][Context.User.Id] > DateTime.UtcNow.AddSeconds(-15))
                {
                    TimeSpan t = events.cooldown[Context.Guild.Id][Context.User.Id] - DateTime.UtcNow.AddSeconds(-15);
                    await RespondAsync($"You're still making another snowball! You'll be ready in {t.Seconds:00} seconds.");
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
                    await RespondAsync($"{Context.User.Mention} attempted to throw a snowball at {Context.User.Mention}, but all they managed to do is fall over and lose their snowball.");
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
                        await RespondAsync($"The snowball sailed right through {user.Username}! Wait, what?\ni probably don't have the manage roles permission! `{ex.Message}`");
                        return;
                    }

                    events.stats[Context.Guild.Id][Context.User.Id].Hits++;
                    events.stats[Context.Guild.Id][user.Id].Downed++;
                    await RespondAsync($"{Context.User.Mention} threw a snowball at {user.Mention}!");
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

                        await RespondAsync($"{Context.User.Mention} threw a snowball at {user.Mention}!");
                    }
                    else if (chance >= 66 && chance <= 90)
                    {
                        // miss
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Dodged++;

                        await RespondAsync($"{Context.User.Mention} threw a snowball at {user.Mention}, but it missed!");
                    }
                    else if (chance >= 91 && chance <= 100)
                    {
                        // caught
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Caught++;

                        events.cooldown[Context.Guild.Id][user.Id] = DateTime.UtcNow.AddMinutes(-10);

                        await RespondAsync($"{Context.User.Mention} threw a snowball at {user.Mention}, but {user.Mention} caught it!");
                    }
                }
            }
            else
            {
                await RespondAsync("No one has thrown any snowballs your way yet, so you don't have the Snowball role yet.");
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
                    var user = Context.Guild.GetUser(kv.Key);
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
                    var user = Context.Guild.GetUser(kv.Key);
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
                await RespondAsync(output.ToString());
            //});
        }
    }
}
