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

        public Standard(RandomStrings _strings)
        {
            strings = _strings;
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
            if (Context.Guild.Id != 132720341058453504)
                return;

            IGuildUser user = null;
            string message = ""; // store the message later to be intelligent about when to yell at them for having the role or not

            if (Context.Message.MentionedUserIds.Count() == 1)
            {
                if (Context.Message.MentionedUserIds.FirstOrDefault() == ((SocketGuild)Context.Guild).CurrentUser.Id)
                    user = (IGuildUser)Context.Message.Author;
                else
                {
                    message = "Hey, you sure you want to throw snowballs at your supplier";
                }
            }
            if (Context.Message.MentionedUserIds.Count() > 1)
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

                if (cooldown.ContainsKey(Context.Guild.Id))
                {
                    cooldown[Context.Guild.Id] = new Dictionary<ulong, DateTime>();


                }


                if (!user.RoleIds.ToList().Contains(394129853043048448))
                {
                    try
                    {
                        await user.AddRoleAsync(Context.Guild.GetRole(394129853043048448));
                    }
                    catch (Exception ex)
                    {
                        await ReplyAsync($"{user.Mention} is too fast and dodged your snowball!\nPoke Googie2149 about this! `{ex.Message}`");
                        return;
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
            if (Context.Message.MentionedUserIds.Count() > 1)
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
