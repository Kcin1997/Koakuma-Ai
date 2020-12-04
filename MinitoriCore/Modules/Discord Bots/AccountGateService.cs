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

        private string MoreDifferentFancyTime(DateTimeOffset creationDate)
        {
            var time = DateTimeOffset.UtcNow - creationDate;

            //int years = 0;
            //int months = 0;
            int days = 0;
            int hours = 0;
            int minutes = 0;


            StringBuilder date = new StringBuilder();
            if (minutes + /*years + months +*/ days + hours > 0)
            {
                //if (time. > 0)
                //    date.Append($"{years} {((years == 1) ? "year" : "years")} ");
                //if (months > 0)
                //    date.Append($"{months} {((months == 1) ? "month" : "months")} ");
                //if (months + years < 1)
                //{
                    if (days > 0)
                        date.Append($"{days} {((days == 1) ? "day" : "days")} ");
                    if (days < 4)
                    {
                        if (hours > 0)
                            date.Append($"{hours} {((hours == 1) ? "hour" : "hours")} ");
                    }
                    if (days < 3)
                    {
                        if (minutes > 0)
                            date.Append($"{minutes} {((minutes == 1) ? "minute" : "minutes")} ");
                    }
                //}
            }
            else
                date.Append("less than a minute ");

            return date.ToString();
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            if (user.IsBot)
                return;

            if (user.Guild.Id != 110373943822540800)
                return;

            if (config.AgeGate <= 0)
                return;

            if (user.CreatedAt > DateTimeOffset.Now.AddDays(config.AgeGate * -1))
            {
                await user.AddRoleAsync(user.Guild.GetRole(784226125408763954));
                await ((SocketTextChannel)user.Guild.GetChannel(784491009249247253)).SendMessageAsync($"`[{DateTimeOffset.Now.ToLocalTime().ToString("HH:mm:ss")}]` Young account joined:\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id}) ({user.Mention})\n" +
                    $"Created {MoreDifferentFancyTime(user.CreatedAt)} ago.");
            }
        }
    }
}
