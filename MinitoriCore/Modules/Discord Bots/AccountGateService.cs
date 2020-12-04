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
using MinitoriCore.Preconditions;
using Microsoft.Extensions.DependencyInjection;


namespace MinitoriCore
{
    public class AccountGateService
    {
        private DiscordSocketClient client;
        private IServiceProvider services;
        private Config config;

        

        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();
            services = _services;

            client.UserJoined += Client_UserJoined;
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            if (user.Guild.Id != 110373943822540800)
                return;

            if (config.AgeGate <= 0)
                return;

            if (user.CreatedAt > DateTimeOffset.Now.AddDays(config.AgeGate * -1))
            {
                await user.AddRoleAsync(user.Guild.GetRole(784226125408763954));
                await ((SocketTextChannel)user.Guild.GetChannel(467192652463341578)).SendMessageAsync($"`[{DateTimeOffset.Now.ToString("HH:mm:ss")}]` Young account joined:\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id}) ({user.Mention})\n" +
                    $"**Account created** `{user.CreatedAt.ToString("d")} {user.CreatedAt.ToString("T")}`");
            }
        }
    }
}
