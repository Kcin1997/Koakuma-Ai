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
using Newtonsoft.Json;
using System.Security.Cryptography;
using MinitoriCore.Preconditions;
using System.Net.Http;

namespace MinitoriCore.Modules.Battlefield
{
    [RequireGuild(783783142737182720)]
    public class BattleField : MinitoriModule
    {
        private enum BF2Endpoint
        {
            Stats, Server, Player, PlayerServer
        }
        
        private async Task<string> GetData(string endPoint)
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"https://api.bflist.io/bf2/v1/{endPoint}?plainerr=1");
                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsStringAsync();
                else
                    throw new Exception(await result.Content.ReadAsStringAsync());
                    // I'm sure this is going to fail horribly some how, it needs more testing
            }
        }

        private string GetFlag(string team)
        {
            switch (team)
            {
                case "US":
                    return "<:US:814529984425230336>";
                case "MEC":
                    return "<:MEC:814529984399802408>";
                case "CH":
                    return "<:CH:814529983975522326>";
                default:
                    return "❓";
            }
        }

        [Command("servers")]
        private async Task GetServers(int page = 0)
        {

            var servers = JsonConvert.DeserializeObject<List<BF2Server>>(await GetData($"servers{(page > 0 ? $"/{page}" : "")}"));

            StringBuilder output = new StringBuilder();

            int i = 0;
            foreach (var s in servers)
            {
                output.AppendLine($"{s.Name} `{s.IP}:{s.GamePort}`");

                i++;
                if (i > 10)
                    break;
            }

            await RespondAsync(output.ToString());
        }

        [Command("server")]
        private async Task GetServer(string input)
        {
            // TODO: Validate input

            BF2Server server;

            try
            {
                server = JsonConvert.DeserializeObject<BF2Server>(await GetData($"servers/{input}"));
            }
            catch (Exception ex)
            {
                await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            if (server.CommunityLogoUrl != "")
                builder.ThumbnailUrl = server.CommunityLogoUrl;

            await ReplyAsync(embed:
                builder
                    .WithTitle($"{server.Name} | {server.IP}:{server.GamePort}")
                    .WithDescription($"**{server.MapName} - {server.OnlinePlayers}/{server.MaxPlayers}**")
                    //$"\nTeam 1: {GetFlag(server.Team1)}" +
                    //$"\n\nPassword protected: {server.Password}" +
                    //$"\nType: {server.GameType}" +
                    //$"\nTime limit: {server.Timelimit}" +
                    //$"\nRounds per map: {server.RoundsPerMap}" +
                    //$"\nAutobalance: {server.AutoBalance}" +
                    //$"\nFriendly fire: {server.FriendlyFire}" +
                    //$"\nTK Mode: {server.TKMode}" +
                    //$"\nTicket ratio: {server.TicketRatio}" +
                    //$"\nTeam Ratio: {server.TeamRatio}" +
                    //$"\nTeam 2: {GetFlag(server.Team2)}" +
                    //$"\nBots: {server.Bots}" +
                    //$"{(server.Bots ? "\nBot count: {server.CoopBotCount}" : "")}" +
                    //$"\nGlobal Unlocks: {server.GlobalUnlocks}" +
                    //$"\nServer FPS: {server.FPS}" +
                    //$"\nVehicles: {(server.NoVehicles == 0 ? "True" : "False")}" +
                    //$"\nDedicated server: {server.Dedicated}" +
                    //$"\nRanked: {server.Ranked}" +
                    //$"\nServer OS: {server.OS}" +
                    //$"\nBattle Recorder available: {server.BattleRecorder}" +
                    //$"{(server.BattleRecorder ? $"\nDemos link: {server.DemoDownload}" : "")}")
                    .WithFields(new EmbedFieldBuilder().WithIsInline(true).WithName($"Team 1: {GetFlag(server.Team1)}").WithValue(
                        $"Password protected: {server.Password}" +
                        $"\nType: {server.GameType}" +
                        $"\nTime limit: {server.Timelimit}" +
                        $"\nRounds per map: {server.RoundsPerMap}" +
                        $"\nAutobalance: {server.AutoBalance}" +
                        $"\nFriendly fire: {server.FriendlyFire}" +
                        $"\nTK Mode: {server.TKMode}" +
                        $"\nTicket ratio: {server.TicketRatio}" +
                        $"\nTeam Ratio: {server.TeamRatio}"
                    ), new EmbedFieldBuilder().WithIsInline(true).WithName($"Team 2: {GetFlag(server.Team2)}").WithValue(
                        $"Bots: {server.Bots}" +
                        $"{(server.Bots ? "\nBot count: {server.CoopBotCount}" : "")}" +
                        $"\nGlobal Unlocks: {server.GlobalUnlocks}" +
                        $"\nServer FPS: {server.FPS}" +
                        $"\nVehicles: {(server.NoVehicles == 0 ? "True" : "False")}" +
                        $"\nDedicated server: {server.Dedicated}" +
                        $"\nRanked: {server.Ranked}" +
                        $"\nServer OS: {server.OS}" +
                        $"\nBattle Recorder available: {server.BattleRecorder}" +
                        $"{(server.BattleRecorder ? $"\nDemos link: {server.DemoDownload}" : "")}")
                    )
                    .Build()
                );
        }
    }

    public class Team
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class Player
    {
        [JsonProperty("pid")]
        public int PlayerID { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("tag")]
        public string Tag { get; set; }
        [JsonProperty("score")]
        public int Score { get; set; }
        [JsonProperty("kills")]
        public int Kills { get; set; }
        [JsonProperty("deaths")]
        public int Deaths { get; set; }
        [JsonProperty("ping")]
        public int Ping { get; set; }
        //[JsonProperty("teamIndex")]
        //public int teamIndex { get; set; }
        [JsonProperty("teamLabel")]
        public string TeamLabel { get; set; }
        [JsonProperty("aibot")]
        public bool AIBot { get; set; }
    }

    public class BF2Server
    {
        [JsonProperty("ip")]
        public string IP { get; set; }
        //[JsonProperty("queryPort")]
        //public int queryPort { get; set; }
        [JsonProperty("gamePort")]
        public int GamePort { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("mapName")]
        public string MapName { get; set; }
        [JsonProperty("mapSize")]
        public int MapSize { get; set; }
        [JsonProperty("numPlayers")]
        public int OnlinePlayers { get; set; }
        [JsonProperty("maxPlayers")]
        public int MaxPlayers { get; set; }
        [JsonProperty("teams")]
        public List<Team> Teams { get; set; }
        [JsonProperty("players")]
        public List<Player> Players { get; set; }
        [JsonProperty("password")]
        public bool Password { get; set; }
        //[JsonProperty("gameVersion")]
        //public string GameVersion { get; set; }
        [JsonProperty("gameType")]
        public string GameType { get; set; }
        //[JsonProperty("gameVariant")]
        //public string GameVariant { get; set; }
        [JsonProperty("timelimit")]
        public int Timelimit { get; set; }
        [JsonProperty("roundsPerMap")]
        public string RoundsPerMap { get; set; }
        [JsonProperty("dedicated")]
        public bool Dedicated { get; set; }
        [JsonProperty("ranked")]
        public bool Ranked { get; set; }
        [JsonProperty("anticheat")]
        public bool AntiCheat { get; set; }
        [JsonProperty("os")]
        public string OS { get; set; }
        [JsonProperty("battlerecorder")]
        public bool BattleRecorder { get; set; }
        //[JsonProperty("demoIndex")]
        //public string demoIndex { get; set; }
        [JsonProperty("demoDownload")]
        public string DemoDownload { get; set; }
        //[JsonProperty("voip")]
        //public bool voip { get; set; }
        [JsonProperty("autobalance")]
        public bool AutoBalance { get; set; }
        [JsonProperty("friendlyfire")]
        public bool FriendlyFire { get; set; }
        [JsonProperty("tkmode")]
        public string TKMode { get; set; }
        //[JsonProperty("startdelay")]
        //public int StartDelay { get; set; }
        //[JsonProperty("spawntime")]
        //public int SpawnTime { get; set; }
        //[JsonProperty("sponsorText")]
        //public string SponsorText { get; set; }
        //[JsonProperty("sponsorLogoUrl")]
        //public string SponsorLogoUrl { get; set; }
        //[JsonProperty("communityLogoUrl")]
        public string CommunityLogoUrl { get; set; }
        //[JsonProperty("scorelimit")]
        //public int Scorelimit { get; set; }
        [JsonProperty("ticketratio")]
        public int TicketRatio { get; set; }
        [JsonProperty("teamratio")]
        public int TeamRatio { get; set; }
        [JsonProperty("team1")]
        public string Team1 { get; set; }
        [JsonProperty("team2")]
        public string Team2 { get; set; }
        [JsonProperty("bots")]
        public bool Bots { get; set; }
        [JsonProperty("pure")]
        public bool Pure { get; set; }
        [JsonProperty("globalUnlocks")]
        public bool GlobalUnlocks { get; set; }
        [JsonProperty("fps")]
        public int FPS { get; set; }
        //[JsonProperty("plasma")]
        //public bool Plasma { get; set; }
        //[JsonProperty("reservedSlots")]
        //public int ReservedSlots { get; set; }
        //[JsonProperty("coopBotRatio")]
        //public int CoopBotRatio { get; set; }
        [JsonProperty("coopBotCount")]
        public int CoopBotCount { get; set; }
        //[JsonProperty("coopBotDiff")]
        //public int CoopBotDiff { get; set; }
        [JsonProperty("noVehicles")]
        public int NoVehicles { get; set; }
    }


}
