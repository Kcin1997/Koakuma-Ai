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

namespace MinitoriCore.Modules.ImageCommands
{
    class Kirby
    {
        public Kirby(CommandService commands)
        {
            commands.CreateModuleAsync("", x =>
            {
                x.Name = "Kirby";

                foreach (string[] source in new string[][] {
                    new string[] { "poyo", "kirby", "gorb" },
                    new string[] { "ddd", "dedede" },
                    new string[] { "metaborb", "metaknight", "borb" },
                    new string[] { "bandana", "waddee", "waddle" },
                    new string[] { "egg", "lor" },
                    new string[] { "spiderman", "taranza" },
                    new string[] { "squeak", "squek" },
                    new string[] { "familyproblems", "susie", "soos" },
                    new string[] { "artist", "adeleine" },
                    new string[] { "randomfairy", "ribbon" },
                    new string[] { "dreamland" },
                    new string[] { "birb" },
                    new string[] { "onion", "witch", "gryll" },
                    new string[] { "queen", "secc", "sectonia" },
                    new string[] { "helper", "helpers", "helpful", "friendship", "friendo" },
                    new string[] { "moretsu", "manga", "mungu", "kirbymanga" },
                    new string[] { "grenpa", "mommy" },
                    new string[] { "eye", "eyeborb", "badsphere" },
                    new string[] { "dad", "father", "baddad", "haltman", "daddy" },
                    new string[] { "clown", "marx", "grape" } })
                {
                    x.AddCommand(source[0], async (context, param, serv, command) =>
                    {
                        await context.Channel.SendMessageAsync("It didnt work son");
                    },
                    command => 
                    {
                        command.AddAliases(source.Skip(1).ToArray());
                        command.Summary = $"***{source[0]}***";
                    });
                }
                
                x.Build(commands);
            });
        }

        
    }
}
