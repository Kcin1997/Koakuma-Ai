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
using MinitoriCore.Preconditions;

namespace MinitoriCore.Modules.ImageCommands
{
    class ArenaScore
    {
        public int Score;
        public string Level;
        public TimeSpan Time;
        public string P1;
        public string P2 = "";
        public string P3 = "";
        public string P4 = "";
        public bool Group = false;
        public bool Amiibo = false;
    }

    public class Kirby : MinitoriModule
    {
        //[Command("addentry")]
        [Hide]
        public async Task AddLeaderboard([Remainder]string remainder)
        {
            if (remainder.Length == 0)
            {
                await RespondAsync("You can't leave this blank!"); // Todo: put a help command here
                return;
            }

            var split = remainder.Split(':').Select(x => x.Trim()).ToArray();

            if (split.Length % 2 == 1)
            {
                await RespondAsync("You gave me an uneven number of responses! Check your colons.");
                return;
            }

            var Ranking = new ArenaScore();

            for (int i = 0; i < split.Length; i += 2)
            {
                switch(split[i].ToLower())
                {
                    case "score":
                        int tempScore;
                        if (int.TryParse(split[i+1], out tempScore))
                            Ranking.Score = tempScore;
                        else
                        {
                            await RespondAsync($"`{split[i + 1]}` doesn't look like a valid score to me.");
                            return;
                        }
                        break;
                    case "time":
                        // Timespan.TryParse and add a "00:" to the start and see if that works
                        TimeSpan tempTime;
                        if (TimeSpan.TryParse(split[i+1], out tempTime))
                            Ranking.Time = tempTime;
                        else if (TimeSpan.TryParse($"00:{split[i + 1]}", out tempTime))
                            Ranking.Time = tempTime;
                        else
                        {
                            await RespondAsync($"`{split[i + 1]}` doesn't look like a valid time to me.");
                            return;
                        }
                        break;
                    case "level":
                        // string because S rank
                        break;
                    case "p1":
                        Ranking.P1 = split[i + 1];
                        break;
                    case "p2":
                        Ranking.P2 = split[i + 1];
                        break;
                    case "p3":
                        Ranking.P3 = split[i + 1];
                        break;
                    case "p4":
                        Ranking.P4 = split[i + 1];
                        break;
                    case "amiibo":
                        if (split[i + 1].ToLower() == "yes")
                            Ranking.Amiibo = true;
                        else if (split[i + 1].ToLower() == "no")
                            Ranking.Amiibo = false;
                        else
                        {
                            await RespondAsync("I was looking for a yes or no answer for the Amiibo section.");
                            return;
                        }
                        break;
                    case "group":
                        if (split[i + 1].ToLower() == "yes")
                            Ranking.Group = true;
                        else if (split[i + 1].ToLower() == "no")
                            Ranking.Group = false;
                        else
                        {
                            await RespondAsync("I was looking for a yes or no answer for the Group section.");
                            return;
                        }
                        break;
                }
            }

            await RespondAsync(
                $"Time: {Ranking.Time.ToString()}\n" +
                $"Score: {Ranking.Score.ToString()}\n" +
                $"Level: {Ranking.Level}\n" +
                $"P1: {Ranking.P1}\n" +
                $"P2: {Ranking.P2}\n" +
                $"P3: {Ranking.P3}\n" +
                $"P4: {Ranking.P4}\n" +
                $"Amiibo: {Ranking.Amiibo.ToString()}\n" +
                $"Group: {Ranking.Group.ToString()}");
        }

        private Dictionary<ulong, Dictionary<ulong, DateTime>> cooldown = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        private Dictionary<string, Dictionary<ulong, string>> lastImage = new Dictionary<string, Dictionary<ulong, string>>();

        public Kirby(CommandService commands, IServiceProvider services)
        {
            commands.CreateModuleAsync("Kirby", x =>
            {
                x.Name = "Kirby";

                foreach (string[] source in new string[][] {
                    new string[] { "poyo", "kirby", "gorb" },
                    new string[] { "ddd", "dedede" },
                    new string[] { "metaborb", "metaknight", "borb" },
                    new string[] { "bandana", "waddee", "waddle" },
                    new string[] { "egg", "lor" },
                    new string[] { "spiderman", "taranza", "spid" },
                    new string[] { "squeak", "squek" },
                    new string[] { "familyproblems", "susie", "soos" },
                    new string[] { "artist", "adeleine" },
                    new string[] { "randomfairy", "ribbon" },
                    new string[] { "dreamland" },
                    new string[] { "birb", "gala" },
                    new string[] { "onion", "witch", "gryll" },
                    new string[] { "queen", "secc", "sectonia" },
                    new string[] { "helper", "helpers", "helpful", "friendship", "friendo" },
                    new string[] { "moretsu", "manga", "mungu", "kirbymanga" },
                    new string[] { "grenpa", "mommy" },
                    new string[] { "eye", "eyeborb", "badsphere" },
                    new string[] { "dad", "father", "baddad", "haltman", "daddy" },
                    new string[] { "clown", "marx", "grape" } })
                {
                    
                    x.AddCommand(source[0], async (e, param, serv, command) =>
                    {
                        //var e = context as CommandContext;

                        if (e.Guild != null && 
                        (cooldown.ContainsKey(e.Guild.Id) && cooldown[e.Guild.Id].ContainsKey(e.User.Id) &&
                        cooldown[e.Guild.Id][e.User.Id] >  DateTime.Now.AddMinutes(-2)))
                        {
                            TimeSpan t = cooldown[e.Guild.Id][e.User.Id] - DateTime.Now.AddMilliseconds(-2);
                            await e.Channel.SendMessageAsync($"You are in a cooldown period, you must wait {t.Minutes:0}:{t.Minutes:00} until you can use these commands again.");

                            return;
                        }

                        await UploadImage(source[0], e);
                        //await context.Channel.SendMessageAsync("It didnt work son");
                    },
                    command => 
                    {
                        command.AddAliases(source.Skip(1).ToArray());
                        command.Summary = $"***{source[0]}***";
                        //command.AddPrecondition(new HideAttribute());
                        
                    });
                }
                
                x.Build(commands, services);
            });
        }

        private async Task UploadImage(string source, ICommandContext e)
        {
            Random asdf = new Random();
            string[] valid = new string[] { ".jpg", ".jpeg", ".png", ".gif" };
            string file = "";

            if (!lastImage.ContainsKey(source))
            {
                lastImage[source] = new Dictionary<ulong, string>();
            }
            if (!lastImage[source].ContainsKey(e.Channel.Id))
            {
                lastImage[source][e.Channel.Id] = "";
            }

            if (!Directory.Exists($"./Images/{source}/"))
            {
                Directory.CreateDirectory(source);
            }

            int fileCount = Directory.GetFiles($@"./Images/{source}/", "*.*").Where(x => valid.Contains(x.Substring(x.LastIndexOf('.')))).Count();

            if (fileCount == 0)
            {
                await e.Channel.SendMessageAsync($"There are no files in the {source} folder yet! Use the `{source} add` command to add some!");
                return;
            }
            else if (fileCount == 1)
                file = Directory.GetFiles($@"./Images/{source}/", "*.*").Where(x => valid.Contains(x.Substring(x.LastIndexOf('.')))).OrderBy(x => asdf.Next()).FirstOrDefault();
            else if (fileCount > 1)
                file = Directory.GetFiles($@"./Images/{source}/", "*.*").Where(x => valid.Contains(x.Substring(x.LastIndexOf('.'))) && lastImage[source][e.Channel.Id] != x).OrderBy(x => asdf.Next()).FirstOrDefault();

            await e.Channel.SendFileAsync(file);

            lastImage[source][e.Channel.Id] = file;

            if (e.User.Id == 83390631937839104)
            {
                var r = new Random();
                if (r.Next(0, 100) > 60)
                {
                    if (r.Next(0, 100) > 60)
                        await e.Channel.SendMessageAsync("Compa was here :^)");
                    else
                        await e.Channel.SendMessageAsync("hi itsuka kotori (miku)");
                }
            }

            if (e.Guild != null)
            {
                if (!cooldown.ContainsKey(e.Guild.Id))
                    cooldown[e.Guild.Id] = new Dictionary<ulong, DateTime>();

                cooldown[e.Guild.Id][e.User.Id] = DateTime.Now;
            }
        }
    }
}
