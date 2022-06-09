using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;

namespace MinitoriCore
{
    public class MinitoriSlashCommand
    {
        private readonly bool develop = true; // true to only add commands to guild
        private readonly ulong devServer = 185220517773574144;  // Server to add slash commands to for testing
        public SlashCommandBuilder Builder { get; private set; }
        public Task Run { get; private set; }

        public async Task RegisterCommand(DiscordSocketClient socketClient)
        {
            try
            {
                if (develop)
                {
                    var devGuild = socketClient.GetGuild(devServer);
                    await devGuild.CreateApplicationCommandAsync(Builder.Build());
                }
                else
                {
                    await socketClient.CreateGlobalApplicationCommandAsync(Builder.Build());
                }
            }
            catch (HttpException exception)
            {
                Console.WriteLine(exception.Errors);
            }
        
        }
    }
}
