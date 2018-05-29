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
using System.Drawing;
using MinitoriCore.Preconditions;

namespace MinitoriCore.Modules.Standard
{
    public class Standard : MinitoriModule
    {
        private RandomStrings strings;

        private EventStorage events;
        private Config config;
        private CommandService commands;
        private IServiceProvider services;
        private Dictionary<ulong, bool> rotate = new Dictionary<ulong, bool>();
        private Dictionary<ulong, float> angle = new Dictionary<ulong, float>();

        public Standard(RandomStrings _strings, EventStorage _events, CommandService _commands, IServiceProvider _services, Config _config)
        {
            strings = _strings;
            events = _events;
            commands = _commands;
            services = _services;
            config = _config;
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

        [Command("help")]
        public async Task HelpCommand()
        {
            Context.IsHelp = true;

            StringBuilder output = new StringBuilder();
            StringBuilder module = new StringBuilder();
            var SeenModules = new List<string>();
            int i = 0;

            output.Append("These are the commands you can use:");

            foreach (var c in commands.Commands)
            {
                if (!SeenModules.Contains(c.Module.Name))
                {
                    if (i > 0)
                        output.Append(module.ToString());

                    module.Clear();

                    module.Append($"\n**{c.Module.Name}:**");
                    SeenModules.Add(c.Module.Name);
                    i = 0;
                }

                if ((await c.CheckPreconditionsAsync(Context, services)).IsSuccess)
                {
                    if (i == 0)
                        module.Append(" ");
                    else
                        module.Append(", ");

                    i++;

                    module.Append($"`{c.Name}`");
                }
            }

            if (i > 0)
                output.AppendLine(module.ToString());

            await ReplyAsync(output.ToString());
        }

        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            await RespondAsync($"Blah to you too, {Context.User.Mention}.");
        }

        [Command("setnick")]
        [Summary("Change my nickname!")]
        [RequireOwner()]
        public async Task SetNickname(string Nick = "")
        {
            await (Context.Guild as SocketGuild).CurrentUser.ModifyAsync(x => x.Nickname = Nick);
            await RespondAsync(":thumbsup:");
        }

        [Command("quit", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Hide]
        public async Task ShutDown()
        {
            if (Context.User.Id != config.OwnerId)
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            events.Save();

            await RespondAsync("rip");
            await Context.Client.LogoutAsync();
            await Task.Delay(1000);
            Environment.Exit((int)ExitCodes.ExitCode.Success);
        }

        [Command("restart", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Hide]
        public async Task Restart()
        {
            if (Context.User.Id != config.OwnerId)
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            events.Save();

            await RespondAsync("Restarting...");
            await File.WriteAllTextAsync("./update", Context.Channel.Id.ToString());

            await Context.Client.LogoutAsync();
            await Task.Delay(1000);
            Environment.Exit((int)ExitCodes.ExitCode.Restart);
        }

        [Command("update", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Hide]
        public async Task UpdateAndRestart()
        {
            if (Context.User.Id != config.OwnerId)
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            await File.WriteAllTextAsync("./update", Context.Channel.Id.ToString());
            events.Save();

            await RespondAsync("hold on i gotta go break everything");
            await Context.Client.LogoutAsync();
            await Task.Delay(1000);
            Environment.Exit((int)ExitCodes.ExitCode.RestartAndUpdate);
        }

        [Command("deadlocksim", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Hide]
        public async Task DeadlockSimulation()
        {
            if (Context.User.Id != config.OwnerId)
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            File.Create("./deadlock");
            events.Save();

            await RespondAsync("Restarting...");
            await Context.Client.LogoutAsync();
            await Task.Delay(1000);
            Environment.Exit((int)ExitCodes.ExitCode.DeadlockEscape);
        }

        [Command("rotate", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Summary("ye")]
        [RequireGuild(124499234564210688)]
        [RequireOwner]
        public async Task Rotate()
        {
            if (!Context.Guild.CurrentUser.GuildPermissions.ManageGuild)
            {
                await RespondAsync("Nope, don't have permission to do that.");
                return;
            }

            if (!rotate.ContainsKey(Context.Guild.Id))
            {
                rotate[Context.Guild.Id] = false;
                angle[Context.Guild.Id] = 0f;
            }

            rotate[Context.Guild.Id] = !rotate[Context.Guild.Id];

            Bitmap bmp = (Bitmap)System.Drawing.Image.FromFile(@"Images/2.png");

            while (rotate[Context.Guild.Id])
            {
                angle[e.Server.Id] += 5f;

                System.Drawing.Imaging.PixelFormat pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

                angle[Context.Guild.Id] = angle[Context.Guild.Id] % 360;
                if (angle[Context.Guild.Id] > 180)
                    angle[Context.Guild.Id] -= 360;

                using (Bitmap newImg = new Bitmap(bmp.Width, bmp.Height, pf))
                {
                    using (Graphics gfx = Graphics.FromImage(newImg))
                    {
                        gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
                        gfx.RotateTransform(angle[Context.Guild.Id]);
                        gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
                        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        gfx.DrawImage(bmp, new Point(0, 0));
                    }

                    using (MemoryStream stream = new MemoryStream())
                    {
                        newImg.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Position = 0;
                        await Context.Guild.ModifyAsync(x => x.Icon = new Discord.Image(stream));
                    }
                }

                await Task.Delay(1000 * 60 * 10);
            }
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

            await RespondAsync(output.ToString());
        }

        [Command("listroles")]
        public async Task ListRoles([Remainder]string role)
        {
            if (role.Length > 0)
            {
                var r = Context.Guild.Roles.FirstOrDefault(x => x.Name == role);
                if (r != null)
                    await RespondAsync($"```{r.Id} | {r.Name}```");
                else
                    await RespondAsync($"I can't find a role named `{role}`!");

                return;
            }

            StringBuilder output = new StringBuilder();
            output.Append("```");

            foreach (var r in Context.Guild.Roles)
            {
                output.AppendLine($"{r.Id} | {r.Name}");
            }

            output.Append("```");

            await RespondAsync(output.ToString());
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
                    user = Context.Guild.GetUser(Context.Message.MentionedUserIds.FirstOrDefault());
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

            await RespondAsync($"*throws {objects} at {user.Mention}*");
        }

        [Command("choose")]
        [Alias("choice")]
        [Summary("Let the bot decide for you!")]
        public async Task Choose([Remainder]string remainder = "")
        {
            string[] choices = remainder.Split(';').Where(x => x.Trim().Length > 0).ToArray();
            
            if (choices[0] != "")
                await RespondAsync($"I choose **{choices[RandomInteger(0, choices.Length)].Trim()}**");
            else
                await RespondAsync("What do you want me to do with this?");
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
