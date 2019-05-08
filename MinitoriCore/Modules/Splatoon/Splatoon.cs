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
using Color = Discord.Color;

namespace MinitoriCore.Modules.Splatoon
{
    //[RequireGuild(568302640371335168)]
    public class Splatoon : MinitoriModule
    {
        // private RankedService rankedService;
        private Config config;
        private CommandService commands;
        private IServiceProvider services;

        public Splatoon(CommandService _commands, IServiceProvider _services, Config _config)
        {
            // rankedService = _rankedService;
            commands = _commands;
            services = _services;
            config = _config;
        }
        
        [Command("map", RunMode = RunMode.Async)]
        [Summary("Pick a map!")]
        [Priority(1000)]
        public async Task SelectMap()
        {
            try
            {
                string stage = "";

                if (!Directory.Exists("./Images/Splatoon/"))
                {
                    Directory.CreateDirectory("./Images/Splatoon/");
                }

                Random asdf = new Random(); // Todo: Implement better rng
                int fileCount = Directory.GetFiles("./Images/Splatoon/", "*.png").Count();

                if (fileCount == 0)
                {
                    await RespondAsync("Something went wrong and I have no maps in my list!");
                    return;
                }
                else if (fileCount == 1)
                    stage = Directory.GetFiles("./Images/Splatoon/", "*.png").FirstOrDefault();
                else if (fileCount > 1)
                    stage = Directory.GetFiles("./Images/Splatoon/", "*.png").ToList().OrderBy(x => asdf.Next()).FirstOrDefault();

                stage = stage.Replace("./Images/Splatoon/", "");
                string stageName = stage.Replace('_', ' ').Substring(0, stage.IndexOf('.'));

                var role = (Context.User as IGuildUser).GetRoles().Where(x => x.Color != Color.Default).OrderBy(x => x.Position).Last();

                string multiplier = "";

                if (asdf.Next(0, 100) < 10)
                    multiplier = $" __**2x Battle**__";

                EmbedBuilder builder = new EmbedBuilder();

                builder.ThumbnailUrl = $"attachment://{stage}";
                //builder.Title = stageName;
                builder.AddField(stageName, $"**Mode:** Turfwar{multiplier}", true);
                builder.Timestamp = DateTimeOffset.Now;

                builder.WithFooter($"Requested by {(Context.User as IGuildUser).Nickname ?? Context.User.Username}#{Context.User.Discriminator}", Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());
                builder.Color = role.Color;
                

                await Context.Channel.SendFileAsync($"./Images/Splatoon/{stage}", embed: builder.Build());
            }
            catch (Exception ex)
            {
                await RespondAsync($"There was an error uploading that file:\n{ex.Message}");
                string exMessage;
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

                Console.WriteLine(exMessage);

                return;
            }
        }
    }
}
