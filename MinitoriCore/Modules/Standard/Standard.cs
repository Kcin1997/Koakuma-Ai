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
using System.Net;

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
        private DiscordSocketClient socketClient;

        public Standard(RandomStrings _strings, EventStorage _events, CommandService _commands, IServiceProvider _services, Config _config, DiscordSocketClient _socketClient)
        {
            strings = _strings;
            events = _events;
            commands = _commands;
            services = _services;
            config = _config;
            socketClient = _socketClient;
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

        //[Command("emergencyban")]
        //public async Task EmergencyBan(ulong UserId = 0)
        //{
        //    if (Context.User.Id != 102528327251656704)
        //    {
        //        return;
        //    }

        //    if (UserId == 0)
        //        return;

        //    await Context.Guild.AddBanAsync(UserId, 0, "Emergency ban, will need an actual reason later.");
        //}

        [Command("help")]
        public async Task HelpCommand()
        {
            Context.IsHelp = true;

            StringBuilder output = new StringBuilder();
            Dictionary<string, List<string>> modules = new Dictionary<string, List<string>>();
            //StringBuilder module = new StringBuilder();
            //var SeenModules = new List<string>();
            //int i = 0;

            output.Append("These are the commands you can use:");

            foreach (var c in commands.Commands)
            {
                //if (!SeenModules.Contains(c.Module.Name))
                //{
                //    if (i > 0)
                //        output.Append(module.ToString());

                //    module.Clear();

                //    foreach (var h in c.Module.Commands)
                //    {
                //        if ((await c.CheckPreconditionsAsync(Context, services)).IsSuccess)
                //        {
                //            module.Append($"\n**{c.Module.Name}:**");
                //            break;
                //        }
                //    }
                //    SeenModules.Add(c.Module.Name);
                //    i = 0;
                //}

                if ((await c.CheckPreconditionsAsync(Context, services)).IsSuccess)
                {
                    //if (i == 0)
                    //    module.Append(" ");
                    //else
                    //    module.Append(", ");

                    //i++;

                    if (!modules.ContainsKey(c.Module.Name))
                        modules.Add(c.Module.Name, new List<string>());

                    if (!modules[c.Module.Name].Contains(c.Name))
                        modules[c.Module.Name].Add(c.Name);

                    //module.Append($"`{c.Name}`");
                }
            }

            //if (i > 0)
            //    output.AppendLine(module.ToString());

            foreach (var kv in modules)
            {
                output.Append($"\n**{kv.Key}:** {kv.Value.Select(x => $"`{x}`").Join(", ")}");
            }

            await ReplyAsync(output.ToString());
        }



        [Command("blah")]
        [Summary("Blah!")]
        [Priority(1000)]
        public async Task Blah()
        {
            await RespondAsync($"Blah to you too, {Context.User.Mention}.");
        }

        [Command("getbots", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Summary("na")]
        public async Task ListBots()
        {
            if (!config.OwnerIds.Contains(Context.User.Id))
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            try
            {
                var botAccounts = new List<IGuildUser>();
                //var msg = await ReplyAsync("Downloading the full member list, this might take a little bit...");
                //await Context.Guild.DownloadUsersAsync();

                foreach (var u in Context.Guild.Users)
                {
                    if (u.IsBot)
                        botAccounts.Add(u);
                }

                StringBuilder output = new StringBuilder();

                output.AppendLine($"**I found {botAccounts.Count()} bot accounts in the server.**");
                output.AppendLine("Note: This is *only* bot accounts, this would not include a user account with a bot attached.");
                output.AppendLine("```");

                foreach (var b in botAccounts)
                {
                    output.AppendLine($"{b.Username}#{b.Discriminator} [{b.Id}] | " +
                        $"Joined at {b.JoinedAt.Value.ToLocalTime().ToString("d")} {b.JoinedAt.Value.ToLocalTime().ToString("T")}");
                }

                

                output.AppendLine("```");

                await RespondAsync(output.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [Command("setnick")]
        [Summary("Change my nickname!")]
        [Hide]
        public async Task SetNickname(string Nick = "")
        {
            if (!config.OwnerIds.Contains(Context.User.Id))
            {
                await RespondAsync(":no_good::skin-tone-3: You don't have permission to run this command!");
                return;
            }

            await (Context.Guild as SocketGuild).CurrentUser.ModifyAsync(x => x.Nickname = Nick);
            await RespondAsync(":thumbsup:");
        }

        [Command("prefix")]
        [Alias("prefix list", "prefixes", "prefixes list")]
        public async Task PrefixList()
        {
            return;
        }

        [Command("prefix help")]
        [Alias("help prefix", "prefixes help", "help prefixes", "prefix list help", "help prefix list", "prefixes list help", "help prefixes list", "help prefix set")]
        public async Task PrefixHelp()
        {
            return;
        }

        [Command("prefix set")]
        public async Task SetPrefix(/*[Remainder]List<string> prefixes*/)
        {
            return;
        }

        [Command("quit", RunMode = RunMode.Async)]
        [Priority(1000)]
        [Hide]
        public async Task ShutDown()
        {
            if (!config.OwnerIds.Contains(Context.User.Id))
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
            if (!config.OwnerIds.Contains(Context.User.Id))
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
            if (!config.OwnerIds.Contains(Context.User.Id))
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
            if (!config.OwnerIds.Contains(Context.User.Id))
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

        //[Command("rotate", RunMode = RunMode.Async)]
        //[Priority(1000)]
        //[Summary("ye")]
        //[RequireGuild(124499234564210688)]
        //public async Task Rotate()
        //{
        //    if (!Context.Guild.CurrentUser.GuildPermissions.ManageGuild)
        //    {
        //        await RespondAsync("Nope, don't have permission to do that.");
        //        return;
        //    }

        //    if (!rotate.ContainsKey(Context.Guild.Id))
        //    {
        //        rotate[Context.Guild.Id] = false;
        //        angle[Context.Guild.Id] = 0f;
        //    }

        //    rotate[Context.Guild.Id] = !rotate[Context.Guild.Id];

        //    Bitmap bmp = (Bitmap)System.Drawing.Image.FromFile(@"Images/2.png");

        //    while (rotate[Context.Guild.Id])
        //    {
        //        angle[Context.Guild.Id] += 5f;

        //        System.Drawing.Imaging.PixelFormat pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

        //        angle[Context.Guild.Id] = angle[Context.Guild.Id] % 360;
        //        if (angle[Context.Guild.Id] > 180)
        //            angle[Context.Guild.Id] -= 360;

        //        using (Bitmap newImg = new Bitmap(bmp.Width, bmp.Height, pf))
        //        {
        //            using (Graphics gfx = Graphics.FromImage(newImg))
        //            {
        //                gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
        //                gfx.RotateTransform(angle[Context.Guild.Id]);
        //                gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
        //                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //                gfx.DrawImage(bmp, new Point(0, 0));
        //            }

        //            using (MemoryStream stream = new MemoryStream())
        //            {
        //                newImg.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        //                stream.Position = 0;
        //                await Context.Guild.ModifyAsync(x => x.Icon = new Discord.Image(stream));
        //            }
        //        }

        //        await Task.Delay(1000 * 60 * 10);
        //    }
        //}

        [Command("icon")]
        [Summary("na")]
        [RequireOwner]
        public async Task ServerIcon()
        {
            await RespondAsync($"https://cdn.discordapp.com/icons/{Context.Guild.Id}/{Context.Guild.IconId}.png?size=2048");
        }

        [Command("servercount")]
        [Summary("Take a guess")]
        public async Task ServerCount()
        {
            await RespondAsync($"I am currently in {Context.Client.Guilds.Count()} servers.");
        }

        [Command("botcollections")]
        [RequireOwner]
        [Hide]
        public async Task ListCollectionServers()
        {
            Task.Run(async () =>
            {
                StringBuilder output = new StringBuilder();
                foreach (var guild in socketClient.Guilds)
                {
                    var percentage = (Convert.ToDouble(guild.Users.Count(x => x.IsBot)) / (guild.Users.Count())) * 100;

                    if (percentage < 30)
                        continue;

                    if (guild.Users.Count(x => x.IsBot) < 15)
                        continue;

                    output.AppendLine($"{guild.Name} [{guild.Id}] | Users: {guild.Users.Count(x => !x.IsBot)}, Bots {guild.Users.Count(x => x.IsBot)} | " +
                        $"{percentage}% Bots");
                }
                await RespondAsync($"```{output}```");
            });
        }

        [Command("forceleave")]
        [RequireOwner]
        [Hide]
        public async Task ForceLeave(ulong[] guilds)
        {
            foreach (var g in guilds)
            {
                var guild = socketClient.GetGuild(g);
                if (guild != null)
                    await guild.LeaveAsync();
            }
        }

        //[Command("zoom reset", RunMode = RunMode.Async)]
        //[Priority(1000)]
        //[Summary("ya")]
        //[RequireOwner]
        //public async Task ZoomClearCache()
        //{
        //    if (File.Exists($"./Images/Servers/{Context.Guild.Id}.png"))
        //    {
        //        await Context.Guild.ModifyAsync(x => x.Icon = new Discord.Image(File.OpenRead($"./Images/Servers/{Context.Guild.Id}.png")));
        //        File.Delete($"./Images/Servers/{Context.Guild.Id}.png");
        //        rotate[Context.Guild.Id] = false;
        //        angle[Context.Guild.Id] = 0f;
        //        await RespondAsync("Cache cleared, old icon restored.");
        //    }
        //}

        //[Command("zoom", RunMode = RunMode.Async)]
        //[Priority(1000)]
        //[Summary("ya")]
        //[RequireOwner]
        //public async Task Zoom(float zoomLevel = 0f)
        //{
        //    if (!Context.Guild.CurrentUser.GuildPermissions.ManageGuild)
        //    {
        //        await RespondAsync("Nope, don't have permission to do that.");
        //        return;
        //    }

        //    if (!rotate.ContainsKey(Context.Guild.Id))
        //    {
        //        rotate[Context.Guild.Id] = false;
        //        angle[Context.Guild.Id] = 1f;
        //    }

        //    if (zoomLevel != 0f)
        //        angle[Context.Guild.Id] = zoomLevel;

        //    rotate[Context.Guild.Id] = !rotate[Context.Guild.Id];

        //    if (!File.Exists($"./Images/Servers/{Context.Guild.Id}.png"))
        //    {
        //        using (WebClient client = new WebClient())
        //        {
        //            client.DownloadFile(new Uri($"https://cdn.discordapp.com/icons/{Context.Guild.Id}/{Context.Guild.IconId}.png?size=2048"), $"./Images/Servers/{Context.Guild.Id}.png");
        //            await Context.Channel.SendMessageAsync("Started!");
        //        };
        //    }
        //    else
        //    {
        //        if (zoomLevel == 0f)
        //        {
        //            await Context.Channel.SendMessageAsync("I already have that one!");
        //            return;
        //        }
        //    }

        //    Bitmap bmp = (Bitmap)System.Drawing.Image.FromFile($"./Images/Servers/{Context.Guild.Id}.png");

        //    while (rotate[Context.Guild.Id])
        //    {
        //        if (zoomLevel != 0f)
        //        {
        //            rotate[Context.Guild.Id] = false;
        //        }
        //        else
        //            angle[Context.Guild.Id] += 0.01f;

        //        System.Drawing.Imaging.PixelFormat pf = System.Drawing.Imaging.PixelFormat.Format32bppArgb;

        //        //angle[Context.Guild.Id] = angle[Context.Guild.Id] % 360;
        //        //if (angle[Context.Guild.Id] > 180)
        //        //    angle[Context.Guild.Id] -= 360;

        //        using (Bitmap newImg = new Bitmap(bmp.Width, bmp.Height, pf))
        //        {
        //            using (Graphics gfx = Graphics.FromImage(newImg))
        //            {
        //                gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);
        //                //gfx.RotateTransform(angle[Context.Guild.Id]);
        //                gfx.ScaleTransform(angle[Context.Guild.Id], angle[Context.Guild.Id]);
        //                gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);
        //                gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        //                gfx.DrawImage(bmp, new Point(0, 0));
        //            }

        //            using (MemoryStream stream = new MemoryStream())
        //            {
        //                newImg.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        //                stream.Position = 0;
        //                await Context.Guild.ModifyAsync(x => x.Icon = new Discord.Image(stream));
        //            }
        //        }

        //        if (zoomLevel == 0f)
        //            break;

        //        await Task.Delay(1000 * 60 * 60);
        //    }
        //}

        [Command("joined")]
        [Hide]
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

        //[Command("gardedede")]
        //[Summary("why did i do this")]
        //public async Task Gardeposting([Remainder]string Big = "")
        //{
        //    if (Big.ToLower() == "big")
        //    {
        //        await RespondAsync("<a:_Gardedede90:665027960851398657><a:_Gardedede89:665027953398120478><a:_Gardedede88:665027943461945364><a:_Gardedede87:665027934251253761><a:_Gardedede86:665027926248390677><a:_Gardedede85:665027918010777613><a:_Gardedede84:665027908859068416><a:_Gardedede83:665027896892719114><a:_Gardedede82:665027887337832458><a:_Gardedede81:665027872380944414>\n<a:_Gardedede80:665027862562209802><a:_Gardedede79:665027853150060545><a:_Gardedede78:665027843184394253><a:_Gardedede77:665027830320594945><a:_Gardedede76:665027820589809673><a:_Gardedede75:665027782505529346><a:_Gardedede74:665027772833595413><a:_Gardedede73:665027761374625836><a:_Gardedede72:665027751576600608><a:_Gardedede71:665027740449112064>");

        //        await RespondAsync("<a:_Gardedede70:665027732027211787><a:_Gardedede69:665027724204703775><a:_Gardedede68:665027716235526163><a:_Gardedede67:665027706420985879><a:_Gardedede66:665027696740270090><a:_Gardedede65:665027687030456343><a:_Gardedede64:665027677136093214><a:_Gardedede63:665027668965589012><a:_Gardedede62:665027659444650004><a:_Gardedede61:665027651429203972>\n<a:_Gardedede60:665027642512375828><a:_Gardedede59:665027633171660830><a:_Gardedede58:665027623281491978><a:_Gardedede57:665027612568977409><a:_Gardedede56:665027597452836864><a:_Gardedede55:665027587101425684><a:_Gardedede54:665027578310164530><a:_Gardedede53:665027536694018048><a:_Gardedede52:665027528175517699><a:_Gardedede51:665027519535251496>");

        //        await RespondAsync("<a:_Gardedede50:665027512438489105><a:_Gardedede49:665027503819325441><a:_Gardedede48:665027494532874250><a:_Gardedede47:665027485393616897><a:_Gardedede46:665027475394396179><a:_Gardedede45:665027463100891158><a:_Gardedede44:665027427788914689><a:_Gardedede43:665027416569282560><a:_Gardedede42:665027408457629706><a:_Gardedede41:665027395337846796>\n<a:_Gardedede40:665027386219167774><a:_Gardedede39:665027374567522353><a:_Gardedede38:665027365721866260><a:_Gardedede37:665027357396041759><a:_Gardedede36:665027347208077322><a:_Gardedede35:665027338286792744><a:_Gardedede34:665027329969618962><a:_Gardedede33:665027322163757103><a:_Gardedede32:665027314098372608><a:_Gardedede31:665027306749689905>");

        //        await RespondAsync("<a:_Gardedede30:665027298910535681><a:_Gardedede29:665027292627599370><a:_Gardedede28:665027284302037012><a:_Gardedede27:665027275837669383><a:_Gardedede26:665027266442690591><a:_Gardedede25:665027257756024862><a:_Gardedede24:665027246590787589><a:_Gardedede23:665027237581422623><a:_Gardedede22:665027226458259495><a:_Gardedede21:665027215557394483>\n<a:_Gardedede20:665027207118454786><a:_Gardedede19:665027197853237261><a:_Gardedede18:665027188181041180><a:_Gardedede17:665027175937867777><a:_Gardedede16:665027164281896972><a:_Gardedede15:665027154131681323><a:_Gardedede14:665027120862461991><a:_Gardedede13:665027106224209920><a:_Gardedede12:665027097810567178><a:_Gardedede11:665027088683761705>");

        //        await RespondAsync("<a:_Gardedede10:665027079519338497><a:_Gardedede09:665027071222874122><a:_Gardedede08:665027061966176266><a:_Gardedede07:665027050133782572><a:_Gardedede06:665027041053114409><a:_Gardedede05:665027030278078465><a:_Gardedede04:665027020589367318><a:_Gardedede03:665027009792966657><a:_Gardedede02:665027001093980180><a:_Gardedede01:665026990268481566>");
        //    }
        //    else
        //    {
        //        await RespondAsync("<a:_Gardedede90:665027960851398657><a:_Gardedede89:665027953398120478><a:_Gardedede88:665027943461945364><a:_Gardedede87:665027934251253761><a:_Gardedede86:665027926248390677><a:_Gardedede85:665027918010777613><a:_Gardedede84:665027908859068416><a:_Gardedede83:665027896892719114><a:_Gardedede82:665027887337832458><a:_Gardedede81:665027872380944414>\n<a:_Gardedede80:665027862562209802><a:_Gardedede79:665027853150060545><a:_Gardedede78:665027843184394253><a:_Gardedede77:665027830320594945><a:_Gardedede76:665027820589809673><a:_Gardedede75:665027782505529346><a:_Gardedede74:665027772833595413><a:_Gardedede73:665027761374625836><a:_Gardedede72:665027751576600608><a:_Gardedede71:665027740449112064>\n<a:_Gardedede70:665027732027211787><a:_Gardedede69:665027724204703775><a:_Gardedede68:665027716235526163><a:_Gardedede67:665027706420985879><a:_Gardedede66:665027696740270090><a:_Gardedede65:665027687030456343><a:_Gardedede64:665027677136093214><a:_Gardedede63:665027668965589012><a:_Gardedede62:665027659444650004><a:_Gardedede61:665027651429203972>\n<a:_Gardedede60:665027642512375828><a:_Gardedede59:665027633171660830><a:_Gardedede58:665027623281491978><a:_Gardedede57:665027612568977409><a:_Gardedede56:665027597452836864><a:_Gardedede55:665027587101425684><a:_Gardedede54:665027578310164530><a:_Gardedede53:665027536694018048><a:_Gardedede52:665027528175517699><a:_Gardedede51:665027519535251496>\n<a:_Gardedede50:665027512438489105><a:_Gardedede49:665027503819325441><a:_Gardedede48:665027494532874250><a:_Gardedede47:665027485393616897><a:_Gardedede46:665027475394396179><a:_Gardedede45:665027463100891158><a:_Gardedede44:665027427788914689><a:_Gardedede43:665027416569282560><a:_Gardedede42:665027408457629706><a:_Gardedede41:665027395337846796>");

        //        await RespondAsync("<a:_Gardedede40:665027386219167774><a:_Gardedede39:665027374567522353><a:_Gardedede38:665027365721866260><a:_Gardedede37:665027357396041759><a:_Gardedede36:665027347208077322><a:_Gardedede35:665027338286792744><a:_Gardedede34:665027329969618962><a:_Gardedede33:665027322163757103><a:_Gardedede32:665027314098372608><a:_Gardedede31:665027306749689905>\n<a:_Gardedede30:665027298910535681><a:_Gardedede29:665027292627599370><a:_Gardedede28:665027284302037012><a:_Gardedede27:665027275837669383><a:_Gardedede26:665027266442690591><a:_Gardedede25:665027257756024862><a:_Gardedede24:665027246590787589><a:_Gardedede23:665027237581422623><a:_Gardedede22:665027226458259495><a:_Gardedede21:665027215557394483>\n<a:_Gardedede20:665027207118454786><a:_Gardedede19:665027197853237261><a:_Gardedede18:665027188181041180><a:_Gardedede17:665027175937867777><a:_Gardedede16:665027164281896972><a:_Gardedede15:665027154131681323><a:_Gardedede14:665027120862461991><a:_Gardedede13:665027106224209920><a:_Gardedede12:665027097810567178><a:_Gardedede11:665027088683761705>\n<a:_Gardedede10:665027079519338497><a:_Gardedede09:665027071222874122><a:_Gardedede08:665027061966176266><a:_Gardedede07:665027050133782572><a:_Gardedede06:665027041053114409><a:_Gardedede05:665027030278078465><a:_Gardedede04:665027020589367318><a:_Gardedede03:665027009792966657><a:_Gardedede02:665027001093980180><a:_Gardedede01:665026990268481566>");
        //    }
        //}
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
