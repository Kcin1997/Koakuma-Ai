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
                x.AddCommand("reserved", async (context, param, serv, command) => 
                {
                    await context.Channel.SendMessageAsync("It didnt work son");
                },
                command => { command.AddAliases("reserved1", "reserved3"); });
                x.Build(commands);
            });
        }

        
    }
}
