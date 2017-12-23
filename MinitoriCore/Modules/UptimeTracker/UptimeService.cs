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
        
        private static readonly TimeSpan Offset = TimeZoneInfo.Local.BaseUtcOffset;

        public static bool Installed = false;

        public bool CheckInstalled()
        {
            return Installed;
        }

        public async Task Install(IServiceProvider _services)
        {
            Installed = true;

            client = _services.GetService<DiscordSocketClient>();
            config = _services.GetService<Config>();
            
            client.GuildMembersDownloaded += Client_GuildMembersDownloaded;

            await CheckAllMembers();
            UpdateDatabase();

            client.GuildMemberUpdated += GuildMemberUpdated;
        }

        private async Task Client_GuildMembersDownloaded(SocketGuild guild)
        {
            if (guild.Id != 110373943822540800)
                return;


        }

        private async Task CheckAllMembers()
        {
            await client.GetGuild(110373943822540800).DownloadUsersAsync();
        }

        private void UpdateDatabase()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000 * 60 * 1); // update every 10 minutes

                    try
                    {

                        Dictionary<ulong, FullStatus> SaveStats = new Dictionary<ulong, FullStatus>(StatusCache.Where(x => x.Value.Edited));

                        if (SaveStats.Count() == 0)
                            continue;

                        using (MySqlConnection db = new MySqlConnection(config.UptimeDB))
                        {
                            await db.OpenAsync();

                            var transaction = await db.BeginTransactionAsync();

                            foreach (var kv in SaveStats)
                            {
                                using (var cmd = new MySqlCommand($"INSERT INTO `status_{DateTimeOffset.Now.StartOfWeek(DayOfWeek.Sunday).ToString("yyyy-MM-dd")}` (botid, stats, lastupdated) VALUES(@1, @2, @3) " +
                                    $"ON DUPLICATE KEY UPDATE stats=@2, lastupdated=@3;", db))
                                {
                                    cmd.Parameters.AddWithValue("@1", kv.Value.BotId);
                                    cmd.Parameters.AddWithValue("@2", JsonConvert.SerializeObject(kv.Value.JsonStats));
                                    cmd.Parameters.AddWithValue("@3", kv.Value.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                                    await cmd.ExecuteNonQueryAsync();

                                    cmd.Dispose();
                                }
                            }

                            transaction.Commit();

                            await db.CloseAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
                    }
                }
            });
#pragma warning restore CS4014
        }

        private async Task GuildMemberUpdated(SocketGuildUser Before, SocketGuildUser After)
        {
            if (!After.IsBot)
                return;

            if (After.Guild.Id != 110373943822540800)
                return;

            if (Before.Status == After.Status)
                return;

            if (CheckOnline(Before.Status) == CheckOnline(After.Status))
                return;

            bool online = CheckOnline(After.Status);

            //Console.ForegroundColor = ConsoleColor.Green;
            //Console.WriteLine($"{After.Username} | [{After.Id}] | {After.Status.ToString()}");
            //Console.ResetColor();
            
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

            var temp = new FullStatus() { BotId = Id, JsonStats = tempStats };

            return temp;
        }

        private async Task<FullStatus> GetStats(ulong Id)
        {
            FullStatus temp = null;

            try
            {
                using (MySqlConnection db = new MySqlConnection(config.UptimeDB))
                {
                    await db.OpenAsync();

                    using (var cmd = new MySqlCommand($"SELECT * FROM `status_{DateTimeOffset.Now.StartOfWeek(DayOfWeek.Sunday).ToString("yyyy-MM-dd")}` WHERE botid = @1;", db))
                    {
                        cmd.Parameters.AddWithValue("@1", Id);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                temp = new FullStatus { BotId = (ulong)reader["botid"], JsonStats = new UptimeStats((string)reader["stats"]), LastUpdated = new DateTimeOffset(((DateTime)reader["lastupdated"]).Ticks, Offset) };
                            }

                            reader.Close();
                        }

                        cmd.Dispose();
                    }

                    await db.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\n{ex.StackTrace}");
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
            public ulong BotId { get; set; }
            public UptimeStats JsonStats { get; set; }
            public DateTimeOffset LastUpdated { get; set; }
            public bool Edited = false;

            public void AddOnlineEntry()
            {
                JsonStats.OnlineTicks.Add(DateTimeOffset.Now.Ticks);
                LastUpdated = DateTimeOffset.Now;
                Edited = true;
            }

            public void AddOfflineEntry()
            {
                JsonStats.OfflineTicks.Add(DateTimeOffset.Now.Ticks);
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
                    OnlineTicks = temp.OnlineTicks;
                    OfflineTicks = temp.OfflineTicks;
                    temp = null;
                }
                else
                {
                    OnlineTicks = new List<long>();
                    OfflineTicks = new List<long>();
                }
            }

            [JsonProperty("online")]
            public List<long> OnlineTicks { get; set; }
            [JsonProperty("offline")]
            public List<long> OfflineTicks { get; set; }

            [JsonIgnore]
            public List<DateTimeOffset> OnlineTimes
            {
                get { return OnlineTicks.Select(x => new DateTimeOffset(x, Offset)).ToList(); }
            }
            [JsonIgnore]
            public List<DateTimeOffset> OfflineTimes
            {
                get { return OfflineTicks.Select(x => new DateTimeOffset(x, Offset)).ToList(); }
            }
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
