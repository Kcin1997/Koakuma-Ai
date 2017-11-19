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
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;

namespace MinitoriCore.Modules.UptimeTracker
{
    class UptimeService
    {
        private Config config;
        private DiscordSocketClient client;

        private Dictionary<ulong, FullStatus> StatusCache = new Dictionary<ulong, FullStatus>();


        public async Task Install(IServiceProvider _services)
        {
            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();
            
            client.GuildMemberUpdated += GuildMemberUpdated;

            // Run storage method
        }

        private async Task UpdateDatabase()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000 * 60 * 10); // update every 10 minutes

                    Dictionary<ulong, FullStatus> SaveStats = new Dictionary<ulong, FullStatus>(StatusCache.Where(x => x.Value.Edited));

                    if (SaveStats.Count() == 0)
                        continue;

                    using (MySqlConnection db = new MySqlConnection(""))
                    {
                        await db.OpenAsync();

                        var transaction = await db.BeginTransactionAsync();

                        foreach (var kv in SaveStats)
                        {
                            using (var cmd = new MySqlCommand($"INSERT INTO `status_{DateTimeOffset.Now.StartOfWeek(DayOfWeek.Sunday).ToString("yyyy-MM-dd")}` (botid, stats, lastupdated) VALUES(@1, @2, @3) " +
                                $"ON DUPLICATE KEY UPDATE stats=@2, lastupdated=@3;", db))
                        }
                    }
                }
            });
#pragma warning restore CS4014
        }

        private async Task GuildMemberUpdated(SocketGuildUser Before, SocketGuildUser After)
        {
            if (After.Guild.Id != 110373943822540800)
                return;

            if (Before.Status == After.Status)
                return;

            if (CheckOnline(Before.Status) == CheckOnline(After.Status))
                return;

            bool online = CheckOnline(After.Status);
            
            // Check if it's cached
            if (!StatusCache.ContainsKey(After.Id))
            {
                // Not cached, try to pull it from the db
                var stats = await GetStats(After.Id);
                
                if (stats != null)
                    // It exists, cache it
                    StatusCache[After.Id] = stats;
                else
                    // Doesn't exist, make a new entry
                    StatusCache[After.Id] = GenerateNewStatus(After.Id);
            }

            // Add this event

            if (online)
                StatusCache[After.Id].AddOnlineEntry();
            else
                StatusCache[After.Id].AddOfflineEntry();
        }

        private FullStatus GenerateNewStatus(ulong Id, DateTimeOffset online = new DateTimeOffset(), DateTimeOffset offline = new DateTimeOffset())
        {
            var tempStats = new UptimeStats();

            if (online != DateTimeOffset.MinValue)
                tempStats.OnlineTimes.Add(online);

            if (offline != DateTimeOffset.MinValue)
                tempStats.OfflineTimes.Add(offline);

            var temp = new FullStatus() { BotId = Id, JsonStats = tempStats };

            return temp;
        }

        private async Task<FullStatus> GetStats(ulong Id)
        {
            FullStatus temp = null;

            using (MySqlConnection db = new MySqlConnection(""))
            {
                await db.OpenAsync();

                using (var cmd = new MySqlCommand($"SELECT * FROM `status_{DateTimeOffset.Now.StartOfWeek(DayOfWeek.Sunday).ToString("yyyy-MM-dd")}` WHERE botid = @1;", db))
                {
                    cmd.Parameters.AddWithValue("@1", Id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            temp = new FullStatus { BotId = (ulong)reader["botid"], JsonStats = new UptimeStats((string)reader["stats"]), LastUpdated = (DateTimeOffset)reader["lastupdated"]};
                        }

                        reader.Close();
                    }

                    cmd.Dispose();
                }

                await db.CloseAsync();
            }

            return temp;
        }

        private bool CheckOnline(UserStatus status)
        {
            switch (status)
            {
                case UserStatus.Online:
                case UserStatus.AFK:
                case UserStatus.Idle:
                case UserStatus.DoNotDisturb:
                    return true;
                case UserStatus.Offline:
                case UserStatus.Invisible:
                    return false;
                default:
                    return false;
            }
        }

        private void ClearFlags()
        {
            var Ids = StatusCache.Keys;

            foreach (var i in Ids)
            {
                StatusCache[i].Edited = false;
            }
        }

        public class FullStatus
        {
            private UptimeStats _jsonStats;

            public ulong BotId { get; set; }
            public UptimeStats JsonStats { get; set; }
            public DateTimeOffset LastUpdated { get; set; }
            public bool Edited = false;

            public void AddOnlineEntry()
            {
                JsonStats.OnlineTimes.Add(DateTimeOffset.Now);
                LastUpdated = DateTimeOffset.Now;
                Edited = true;
            }

            public void AddOfflineEntry()
            {
                JsonStats.OfflineTimes.Add(DateTimeOffset.Now);
                LastUpdated = DateTimeOffset.Now;
                Edited = true;
            }
        }

        public class UptimeStats
        {
            public UptimeStats(string input = "")
            {
                if (input != null && input != "")
                {
                    var temp = JsonConvert.DeserializeObject<UptimeStats>(input);
                    OnlineTimes = temp.OnlineTimes;
                    OfflineTimes = temp.OfflineTimes;
                    temp = null;
                }
                else
                {
                    OnlineTimes = new List<DateTimeOffset>();
                    OfflineTimes = new List<DateTimeOffset>();
                }
            }

            [JsonProperty("online")]
            public List<DateTimeOffset> OnlineTimes { get; set; }
            [JsonProperty("offline")]
            public List<DateTimeOffset> OfflineTimes { get; set; }
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTimeOffset StartOfWeek(this DateTimeOffset dt, DayOfWeek startOfWeek)
        {
            int diff = dt.DayOfWeek - startOfWeek;
            if (diff < 0)
            {
                diff += 7;
            }
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
