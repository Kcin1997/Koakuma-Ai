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

namespace MinitoriCore.Modules.Standard
{
    public class Standard : ModuleBase
    {
        private RandomStrings strings;
        private Dictionary<ulong, Dictionary<ulong, DateTime>> cooldown = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        // <Guild ID, <User ID, Time last used>>

        private EventStorage events;

        public Standard(RandomStrings _strings, EventStorage _events)
        {
            strings = _strings;
            events = _events;
        }

        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            await ReplyAsync($"Blah to you too, {Context.User.Mention}.");
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

            if (Context.Guild.Id != 132720341058453504)
                return;

            IGuildUser user = null;
            string message = ""; // store the message later to be intelligent about when to yell at them for having the role or not

            if (Context.Message.MentionedUserIds.Count() == 1)
            {
                if (Context.Message.MentionedUserIds.FirstOrDefault() != ((SocketGuild)Context.Guild).CurrentUser.Id)
                    user = (IGuildUser)Context.Message.Author;
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

            if (((IGuildUser)Context.User).RoleIds.ToList().Contains(394129853043048448))
            {
                if (message != "")
                {
                    await ReplyAsync(message);
                    return;
                }

                if (!cooldown.ContainsKey(Context.Guild.Id))
                    cooldown[Context.Guild.Id] = new Dictionary<ulong, DateTime>();

                if (cooldown[Context.Guild.Id].ContainsKey(Context.User.Id) && cooldown[Context.Guild.Id][Context.User.Id] > DateTime.UtcNow.AddMinutes(-2.5))
                {
                    TimeSpan t = cooldown[Context.Guild.Id][Context.User.Id] - DateTime.UtcNow.AddMinutes(-2.5);
                    await ReplyAsync($"You're still making another snowball! You'll be ready in {t.Minutes:0}:{t.Seconds:00}");
                    return;
                }

                cooldown[Context.Guild.Id][Context.User.Id] = DateTime.UtcNow;

                if (!events.stats.ContainsKey(Context.Guild.Id))
                    events.stats[Context.Guild.Id] = new Dictionary<ulong, SnowballStats>();

                if (events.stats[Context.Guild.Id][Context.User.Id] == null)
                    events.stats[Context.Guild.Id][Context.User.Id] = new SnowballStats();

                if (events.stats[Context.Guild.Id][user.Id] == null)
                    events.stats[Context.Guild.Id][user.Id] = new SnowballStats();

                if (!user.RoleIds.ToList().Contains(394129853043048448))
                {
                    // Add the role. 100% chance to hit
                    try
                    {
                        await user.AddRoleAsync(Context.Guild.GetRole(394129853043048448));
                    }
                    catch (Exception ex)
                    {
                        events.stats[Context.Guild.Id][Context.User.Id].Misses++;
                        events.stats[Context.Guild.Id][user.Id].Dodged++;
                        await ReplyAsync($"The snowball sailed right through {user.Mention}! Wait, what?\nPoke Googie2149 about this! `{ex.Message}`");
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

                        cooldown[Context.Guild.Id][user.Id] = DateTime.UtcNow.AddMinutes(-10);

                        await ReplyAsync($"{Context.User.Mention} threw a snowball at {user.Mention}, but {user.Mention} caught it!");
                    }
                }
            }
            else
            {
                await ReplyAsync("You don't have any snowballs to throw!");
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
