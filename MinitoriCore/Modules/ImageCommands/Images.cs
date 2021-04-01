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
using System.Net;

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

    public class ImageCommands : MinitoriModule
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
        private Config config;

        public ImageCommands(CommandService commands, IServiceProvider services, Config _config)
        {
            config = _config;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            commands.CreateModuleAsync("", x =>
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
                    new string[] { "soos", "susie", "familyproblems" },
                    new string[] { "adeleine", "artist", "ado" },
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
                    new string[] { "clown", "marx", "grape" },
                    new string[] { "nose", "ebi", "juh" },
                    new string[] { "gary", "escargoon" } })
                {
                    // Upload image
                    x.AddCommand(source[0], async (context, param, serv, command) =>
                    {
                        try
                        {
                            await UploadImage(source[0], context);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    },
                    command => 
                    {
                        command.AddAliases(source.Skip(1).ToArray());
                        command.Summary = $"***{source[0]}***";
                        //command.AddPrecondition(new HideAttribute());
                    });

                    // Download image
                    x.AddCommand($"{source[0]} add", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        await DownloadImage(source[0], context, param[0]?.ToString());
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} add").ToArray());
                        command.Summary = $"Never enough {source[0]}, we need more.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("url", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = true;
                        });
                    });

                    // Delete image
                    x.AddCommand($"{source[0]} remove", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        if (config.OwnerIds.Contains(context.User.Id) || // Bot owners
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(190657363798261769)) || // /r/kirby Admins
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(132721372848848896)) || // /r/kirby Mods
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(422853409377615872)))   // /r/kirby Helpers
                        {
                            await DeleteImage(source[0], context, param[0]?.ToString());
                        }
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} remove").ToArray());
                        command.Summary = $"A bit too much {source[0]}. Somehow.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("file", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = false;
                        });
                    });
                }

                // reserved
                x.AddCommand("reserved", async (context, param, serv, command) =>
                {
                    try
                    {
                        await UploadImage("reserved", context);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                },
                command =>
                {
                    command.Summary = $"*this is great*";
                    command.AddPrecondition(new HideAttribute());
                });

                x.Build(commands, services);
            });

            commands.CreateModuleAsync("", x =>
            {
                x.Name = "Touhou";

                foreach (string[] source in new string[][] {
                    new string[] { "honk", "chen" },
                    new string[] { "mukyu" },
                    new string[] { "unyu" },
                    new string[] { "ayaya" },
                    new string[] { "mokou"},
                    new string[] { "awoo" },
                    new string[] { "9ball" },
                    new string[] { "zun" },
                    new string[] { "yuyuko" },
                    new string[] { "kappa" },
                    new string[] { "uuu", "remilia" },
                    new string[] { "alice" },
                    new string[] { "2hu" } })
                {
                    // Upload image
                    x.AddCommand(source[0], async (context, param, serv, command) =>
                    {
                        try
                        {
                            await UploadImage(source[0], context);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).ToArray());
                        command.Summary = $"***{source[0]}***";
                        //command.AddPrecondition(new HideAttribute());
                    });

                    // Download image
                    x.AddCommand($"{source[0]} add", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        await DownloadImage(source[0], context, param[0]?.ToString());
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} add").ToArray());
                        command.Summary = $"Never enough {source[0]}, we need more.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("url", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = true;
                        });
                    });

                    // Delete image
                    x.AddCommand($"{source[0]} remove", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        if (config.OwnerIds.Contains(context.User.Id) || // Bot owners
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(190657363798261769)) || // /r/kirby Admins
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(132721372848848896)) || // /r/kirby Mods
                            ((SocketGuildUser)context.User).Roles.Contains(context.Guild.GetRole(422853409377615872)))   // /r/kirby Helpers
                        {
                            await DeleteImage(source[0], context, param[0]?.ToString());
                        }
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} remove").ToArray());
                        command.Summary = $"A bit too much {source[0]}. Somehow.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("file", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = false;
                        });
                    });
                }

                x.Build(commands, services);
            });

            commands.CreateModuleAsync("", x =>
            {
                x.Name = "Misc";

                foreach (string[] source in new string[][] {
                    new string[] { "desu" },
                    new string[] { "teto", "bread", "🍞", "🥖" } })
                {
                    // Upload image
                    x.AddCommand(source[0], async (context, param, serv, command) =>
                    {
                        try
                        {
                            await UploadImage(source[0], context);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).ToArray());
                        command.Summary = $"***{source[0]}***";
                        command.AddPrecondition(new HideAttribute());
                    });

                    // Download image
                    x.AddCommand($"{source[0]} add", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        await DownloadImage(source[0], context, param[0]?.ToString());
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} add").ToArray());
                        command.Summary = $"Never enough {source[0]}, we need more.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("url", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = true;
                        });
                    });

                    // Delete image
                    x.AddCommand($"{source[0]} remove", async (context, param, serv, command) =>
                    {
                        if (!ImageDownloadWhitelist(context.Guild.Id, context.User.Id))
                            return;

                        if (!config.OwnerIds.Contains(context.User.Id)) // check for bot owners 
                            return;

                        await DeleteImage(source[0], context, param[0]?.ToString());
                    },
                    command =>
                    {
                        command.AddAliases(source.Skip(1).Select(y => $"{y} remove").ToArray());
                        command.Summary = $"A bit too much {source[0]}. Somehow.";
                        command.AddPrecondition(new HideAttribute());
                        command.AddParameter("file", typeof(string), y =>
                        {
                            y.IsRemainder = true;
                            y.IsOptional = false;
                        });
                    });
                }

                x.Build(commands, services);
            });
        }

        private bool ImageDownloadWhitelist(ulong server, ulong user)
        {
            if (config.OwnerIds.Contains(user))
                return true;

            switch (server)
            {
                case 103028520011190272: // RoboNitori
                case 132720341058453504: // /r/Kirby
                    return true;
                default:
                    return false;
            }
        }

        private async Task DeleteImage(string source, ICommandContext context, string file)
        {
            if (!Directory.Exists($"./Images/removed {source}/"))
            {
                Directory.CreateDirectory($"./Images/removed {source}/");
            }

            if (!File.Exists($@"./Images/{source}/{file}"))
            {
                await context.Channel.SendMessageAsync($"I can't find an image named `{file}` in `Images/{source}`");
                return;
            }

            if (!File.Exists($@"./Images/removed {source}/{file}"))
            {
                File.Move($"./Images/{source}/{file}", $"./Images/removed {source}/{file}");
                await context.Channel.SendMessageAsync("Image removed.");
                return;
            }
            else
            {
                int i = 0;

                while (File.Exists($@"./Images/removed {source}/{file}{i}"))
                    i++;

                File.Move($"./Images/{source}/file", $"./Images/removed {source}/{file}{i}");

                await context.Channel.SendMessageAsync($"Image removed. It has also been removed {i + 1} time(s) before.");
                return;
            }
        }

        private async Task UploadImage(string source, ICommandContext context)
        {
            if (context.Guild != null &&
                            (cooldown.ContainsKey(context.Guild.Id) && cooldown[context.Guild.Id].ContainsKey(context.User.Id) &&
                            cooldown[context.Guild.Id][context.User.Id] > DateTime.Now.AddMinutes(-2)))
            {
                TimeSpan t = cooldown[context.Guild.Id][context.User.Id] - DateTime.Now.AddMinutes(-2);

                Task.Run(async () =>
                {
                    var msg = await context.Channel.SendMessageAsync($"You are in a cooldown period, you must wait {t.Minutes:0}:{t.Seconds:00} until you can use these commands again.");

                    await Task.Delay(1000 * 3);

                    await msg.DeleteAsync();
                    await context.Message.DeleteAsync();
                });

                return;
            }

            Random asdf = new Random();
            string[] valid = new string[] { ".jpg", ".jpeg", ".png", ".gif" };
            string file = "";

            if (!lastImage.ContainsKey(source))
            {
                lastImage[source] = new Dictionary<ulong, string>();
            }
            if (!lastImage[source].ContainsKey(context.Channel.Id))
            {
                lastImage[source][context.Channel.Id] = "";
            }

            if (!Directory.Exists($"./Images/{source}/"))
            {
                Directory.CreateDirectory($"./Images/{source}/");
            }

            var dir = Directory.GetFiles($@"./Images/{source}/", "*.*", SearchOption.AllDirectories).Where(x => valid.Contains(x.Substring(x.LastIndexOf('.'))));
            //int fileCount = .Count();

            if (dir.Count() == 0)
            {
                await context.Channel.SendMessageAsync($"There are no files in the {source} folder yet! Use the `{source} add` command to add some!");
                return;
            }
            else if (dir.Count() == 1)
                file = dir.FirstOrDefault();
            else if (dir.Count() > 1)
                file = dir.OrderBy(x => asdf.Next()).FirstOrDefault(x => lastImage[source][context.Channel.Id] != x);

            await context.Channel.SendFileAsync(file);

            lastImage[source][context.Channel.Id] = file;

            if (context.User.Id == 83390631937839104)
            {
                var r = new Random();
                if (r.Next(0, 100) > 60)
                {
                    if (r.Next(0, 100) > 60)
                        await context.Channel.SendMessageAsync("Compa was here :^)");
                    else
                        await context.Channel.SendMessageAsync("hi itsuka kotori (miku)");
                }
            }

            if (context.Guild != null)
            {
                if (!cooldown.ContainsKey(context.Guild.Id))
                    cooldown[context.Guild.Id] = new Dictionary<ulong, DateTime>();

                cooldown[context.Guild.Id][context.User.Id] = DateTime.Now;
            }
        }

        private async Task DownloadImage(string source, ICommandContext context, string url)
        {
            string image = "";

            if (url != null)
                image = url;
            else
            {
                if (context.Message.Attachments.Count() > 0)
                    image = context.Message.Attachments.First().Url;
                else
                {
                    await context.Channel.SendMessageAsync("You have to actually link a file!");
                    return;
                }
            }

            if (!Directory.Exists($"./Images/{source}/"))
            {
                Directory.CreateDirectory($"./Images/{source}/");
            }

            string ext = image.Substring(image.LastIndexOf('.'));
            string file = image.Substring(image.LastIndexOf('/') + 1);

            string[] valid = new string[] { ".jpg", ".jpeg", ".png", ".gif" };

            if (valid.Contains(ext.ToLower()))
            {
                if (file == "unknown.png" || file == "image0.jpg" || file == "image0.png")
                {
                    do
                        file = $".{context.User.Id} - {Path.GetRandomFileName().Substring(0, 8)}{ext}";
                    while (File.Exists($@"./Images/{source}/{file}"));
                }

                if (!Directory.Exists($"./Images/{source}/"))
                {
                    Directory.CreateDirectory(source);
                }

                if (!File.Exists($@"./Images/{source}/{file}"))
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(image), $@"./Images/{source}/{file}");
                        await context.Channel.SendMessageAsync("Downloaded!");
                    };
                }
                else
                    await context.Channel.SendMessageAsync("I already have that one!");
            }
            else
                await context.Channel.SendMessageAsync("That doesn't look like a valid image to me, sorry!");
        }
    }
}
