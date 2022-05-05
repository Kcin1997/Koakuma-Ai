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
using NodaTime;
using System.Data.SQLite;

namespace MinitoriCore.Modules.DiscordBots
{
    public class DiscordBotsService
    {
        private DiscordSocketClient client;
        private IServiceProvider services;
        private Config config;

        private List<ulong> bots = new List<ulong>();
        private bool watchBots = false;

        private List<ulong> muteRoles = new List<ulong>() { 132106771975110656, 132106637614776320 };
        private List<ulong> ignoreRoles = new List<ulong>() { 113379036524212224, 366668416058130432, 234237413675892736, 598574793712992286, 407326634819977217, 361408725241561088, 323535955925663744, 754993555658899486 };

        private const Int64 BOT_ROLE = 185222882463318016;

        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();
            services = _services;

            client.MessageReceived += Client_MessageReceived;
        }

        private async Task Client_MessageReceived(SocketMessage msg)
        {
            if (!watchBots)
                return;

            if (msg.Author.Id == client.CurrentUser.Id)
                return;

            var channel = msg.Channel as IGuildChannel;

            if (channel == null)
                return;

            if (channel.Guild.Id != 110373943822540800)
                return;

            if (channel.Id != 744813231452979220)
                return;

            var user = msg.Author as SocketGuildUser;
            var roles = user.Roles.Select(x => x.Id).ToList();

            if (!roles.Contains(BOT_ROLE)) // check for bot role
                return;

            if (roles.Any(x => ignoreRoles.Contains(x)))
                return;

            if (bots.Contains(user.Id))
                return;

            bots.Add(user.Id);
        }

        public void EnableWatch()
        {
            bots = new List<ulong>();
            watchBots = true;
        }

        public List<ulong> EndWatch()
        {
            watchBots = false;
            return bots;
        }

        public bool CheckWatch()
        {
            return watchBots; // yes I'm aware this code is terrible. I'm tired.
        }
    }
}
