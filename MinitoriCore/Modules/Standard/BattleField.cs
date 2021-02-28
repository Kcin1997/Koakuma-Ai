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
                    .WithDescription($"**{server.MapName} - {server.OnlinePlayers}/{server.MaxPlayers}**" +
                    $"\n\n**Password Protected:** {server.Password}" +
                    $"\n**Type:** {server.GameType}" +
                    $"\n**Time Limit:** {server.Timelimit}" +
                    $"\n**Rounds Per Map:** {server.RoundsPerMap}" +
                    $"\n**Autobalance:** {server.AutoBalance}" +
                    $"\n**Friendly Fire:** {server.FriendlyFire}" +
                    $"\n**TK Mode:** {server.TKMode}" +
                    $"\n**Ticket Ratio:** {server.TicketRatio}" +
                    $"\n**Team Ratio:** {server.TeamRatio}" +
                    $"\n**Team 1:** {GetFlag(server.Team1)}" +
                    $"\n**Team 2:** {GetFlag(server.Team2)}" +
                    $"\n**Bots:** {server.Bots}" +
                    $"{(server.Bots ? "\n**Bot count: {server.CoopBotCount}**" : "")}" +
                    $"\n**Global Unlocks:** {server.GlobalUnlocks}" +
                    $"\n**Server FPS:** {server.FPS}" +
                    $"\n**Vehicles: {(server.NoVehicles == 0 ? "True" : "False")}**" +
                    $"\n**Dedicated Server:** {server.Dedicated}" +
                    $"\n**Ranked:** {server.Ranked}" +
                    $"\n**Server OS:** {server.OS}" +
                    $"\n**Battle Recorder available:** {(server.BattleRecorder ? $"[Link]({server.DemoDownload})" : "False")}")
                    .WithFields(
                    new EmbedFieldBuilder().WithIsInline(true).WithName($"**Team 1:** {GetFlag(server.Team1)}").WithValue(
                        $"```{server.Players.Where(x => x.TeamIndex == 1).Select(x => x.Name).Join("\n")}```"
                        ),
                    new EmbedFieldBuilder().WithIsInline(true).WithName($"**Team 2:** {GetFlag(server.Team2)}").WithValue(
                        $"```{server.Players.Where(x => x.TeamIndex == 2).Select(x => x.Name).Join("\n")}```"
                        )
                    )
                    .Build()
                );
        }

        [Command("scores")]
        private async Task ScoreBoard(string input)
        {
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

            // max lengths

            //int team1 = server.Players.Where(x => x.TeamIndex == 1).Count();
            //int team2 = server.Players.Where(x => x.TeamIndex == 2).Count();

            //int maxCount = 0;

            //if (team1 > team2)
            //    maxCount = team1.ToString().Length + 1;
            //else
            //    maxCount = team2.ToString().Length + 1;

            //int tag = server.Players.Max(x => x.Tag.Length);
            //int name = server.Players.Max(x => x.Name.Length);
            //int score = server.Players.Max(x => x.Score.ToString().Length);
            //int teamwork = server.Players.Max(x => x.Teamwork.ToString().Length);
            //int kills = server.Players.Max(x => x.Kills.ToString().Length);
            //int deaths = server.Players.Max(x => x.Deaths.ToString().Length);
            //int kd = server.Players.Max(x => x.KDRatio.ToString().Length);
            //int ping = server.Players.Max(x => x.Ping.ToString().Length);

            //int maxCount = 3; // "No."
            int tag = server.Players.Max(x => x.Tag.Length);
            int name = server.Players.Max(x => x.Name.Length);
            //int score = 5; // "Score"
            //int teamwork = 8; // "Teamwork"
            //int kills = 5; // "Kills"
            //int deaths = 6; // "Deaths"
            int kd = server.Players.Max(x => x.KDRatio.ToString("0.0").Length); // "K/D"

            if (kd < 3)
                kd = 3;
            //int ping = 4; // "Ping"

            //int maxLength = maxCount + tag + name + score + teamwork + kills + deaths + kd + ping + 8;

            int maxLength = 39 + tag + name + kd;

            StringBuilder output1 = new StringBuilder();
            StringBuilder output2 = new StringBuilder();

            output1.AppendLine("```");
            output1.AppendLine($"No. {"Tag".PadLeft(tag)} {"Name".PadRight(name)} Score Teamwork Kills Deaths {"K/D".PadRight(kd)} Ping");
            output1.AppendLine(new string('_', maxLength));

            int index = 1;
            foreach (var p in server.Players.Where(x => x.TeamIndex == 1).OrderByDescending(x => x.Score))
            {
                string temp = $"{index,2}. {p.Tag.PadLeft(tag)} {p.Name.PadRight(name)} " +
                    $"{p.Score,5} {p.Teamwork,8} {p.Kills,5} {p.Deaths,6} {p.KDRatio.ToString("0.0").PadLeft(kd)} {p.Ping,4}";

                if (output1.Length + temp.Length > 2000)
                {
                    output1.Append("```");
                    await RespondAsync(output1.ToString());
                    output1.Clear();
                    output1.AppendLine("```");
                    output1.AppendLine($"No. {"Tag".PadLeft(tag)} {"Name".PadRight(name)} Score Teamwork Kills Deaths {"K/D".PadRight(kd)} Ping");
                    output1.AppendLine(new string('_', maxLength));
                }

                output1.AppendLine(temp);
                index++;
            }

            output1.Append("```");

            await RespondAsync(output1.ToString());

            output2.AppendLine("```");
            output2.AppendLine($"No. {"Tag".PadLeft(tag)} {"Name".PadRight(name)} Score Teamwork Kills Deaths {"K/D".PadRight(kd)} Ping");
            output2.AppendLine(new string('_', maxLength));

            index = 1;
            foreach (var p in server.Players.Where(x => x.TeamIndex == 2).OrderByDescending(x => x.Score))
            {
                string temp = $"{index,2}. {p.Tag.PadLeft(tag)} {p.Name.PadRight(name)} " +
                    $"{p.Score,5} {p.Teamwork,8} {p.Kills,5} {p.Deaths,6} {p.KDRatio.ToString("0.0").PadLeft(kd)} {p.Ping,4}";

                if (output2.Length + temp.Length > 2000)
                {
                    output2.Append("```");
                    await RespondAsync(output2.ToString());
                    output2.Clear();
                    output2.AppendLine("```");
                    output2.AppendLine($"No. {"Tag".PadLeft(tag)} {"Name".PadRight(name)} Score Teamwork Kills Deaths {"K/D".PadRight(kd)} Ping");
                    output2.AppendLine(new string('_', maxLength));
                }

                output2.AppendLine(temp);
                index++;
            }

            output2.Append("```");

            await RespondAsync(output2.ToString());

            //if (output1.Length + output2.Length > 2000)
            //{
            //    await RespondAsync(output1.ToString());
            //    await RespondAsync(output2.ToString());
            //}
            //else
            //    await RespondAsync(output1.ToString() + output2.ToString());

            //await RespondAsync(output.ToString());
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

        public int Teamwork
        {
            get
            {
                return Score - (Kills * 2);
            }
        }
        public double KDRatio
        {
            get
            {
                if (Deaths == 0)
                    return Kills;
                else
                    return Math.Round((double)Kills / (double)Deaths, 1);
            }
        }
        public int Ping { get; set; }
        [JsonProperty("teamIndex")]
        public int TeamIndex { get; set; }
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
