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

namespace MinitoriCore
{
    public class AccountGateService
    {
        private DiscordSocketClient client;
        private IServiceProvider services;
        private Config config;

        [Flags]
        private enum Filter
        {
            NewAccount = 1,
            NoAvatar = 2,
            OffensiveUsername = 4,
            Advertisement = 8,
            Banned = 16,
            Impersonation = 32
        }

        private class LoggedUser
        {
            public ulong UserId { get; set; }
            public bool ApprovedAccess { get; set; }
            public bool NewAccount { get; set; }
            public ulong ApprovalModId { get; set; }
            public Filter DenialReasons { get; set; }
            public ulong LogMessageId { get; set; }
            public DateTimeOffset OriginalJoinTime { get; set; }
            public int JoinCount { get; set; }
    }

        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();
            services = _services;

            client.UserJoined += Client_UserJoined;
        }

        private string MoreDifferentFancyTime(DateTimeOffset creationDate)
        {

            Period period = Period.Between(LocalDateTime.FromDateTime(creationDate.LocalDateTime), LocalDateTime.FromDateTime(DateTime.Now));

            long years = period.Years;
            long months = period.Months;
            long days = period.Days;
            long hours = period.Hours;
            long minutes = period.Minutes;



            StringBuilder date = new StringBuilder();
            if (minutes + years + months + days + hours > 0)
            {
                if (years > 0)
                    date.Append($"{years} {((years == 1) ? "year" : "years")} ");
                if (months > 0)
                    date.Append($"{months} {((months == 1) ? "month" : "months")} ");
                if (months + years < 1)
                {
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
                }
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

            Filter result = new Filter();

            if (config.AgeGate > 0)
            {
                if (user.CreatedAt > DateTimeOffset.Now.AddDays(config.AgeGate * -1))
                    result |= Filter.NewAccount;
            }

            if (user.AvatarId == null)
            {
                // only filter accounts for no avatar if they're younger than a month
                // make this configurable later
                if (user.CreatedAt > DateTimeOffset.Now.AddMonths(-1))
                    result |= Filter.NoAvatar;
            }

            if (result > 0)
            {
                await user.AddRoleAsync(user.Guild.GetRole(784226125408763954));
                await ((SocketTextChannel)user.Guild.GetChannel(784491009249247253)).SendMessageAsync(
                    $"`[{DateTimeOffset.Now.ToLocalTime().ToString("HH:mm:ss")}]` Filtered account for the following reasons: `{result.ToString()}`\n" +
                    $"{user.Username}#{user.Discriminator} ({user.Id}) ({user.Mention})\n" +
                    $"Created {MoreDifferentFancyTime(user.CreatedAt)}ago.");
            }
        }

        private async Task<bool> InitializeDB()
        {
            bool tableExists = false;

            using (SQLiteConnection db = new SQLiteConnection(config.DatabaseConnectionString))
            {
                await db.OpenAsync();

                using (var cmd = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table' AND name='users';", db))
                {
                    if ((await cmd.ExecuteScalarAsync()) != null)
                        tableExists = true;
                }

                if (!tableExists)
                {
                    using (var cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS users " +
                        "(UserId TEXT NOT NULL PRIMARY KEY, ApprovedAccess INTEGER NOT NULL, NewAccount INTEGER NOT NULL, ApprovalModId TEXT, DenialReasons INTEGER NOT NULL, LogMessage TEXT);", db))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                db.Close();
            }

            return tableExists;
        }
    }
}
