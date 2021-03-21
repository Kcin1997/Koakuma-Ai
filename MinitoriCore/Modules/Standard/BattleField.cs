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
using System.Net;
using System.Web;

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

        private bool ValidateAddress(string address)
        {
            if (!address.Contains(':'))
                return false;

            // this could probably be a lot better but it works and it's simple
            string ip = address.Split(':')[0];
            string port = address.Split(':')[1];

            if (IPAddress.TryParse(ip, out _) && int.TryParse(port, out _))
                return true;
            else
                return false;
        }

        private string GetFlag(string team)
        {
            // Swap emotes out with local copies

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

        private string[] FormatScoreBoard(IEnumerable<BF2Player> players, bool VerboseOutput = false)
        {
            List<string> output = new List<string>();

            //int maxCount = 3; // "No."
            int tag = players.Max(x => x.Tag.Length);

            if (tag < 3)
                tag = 3;

            int name = players.Max(x => x.Name.Length);
            //int score = 5; // "Score"
            //int teamwork = 8; // "Teamwork"
            //int kills = 5; // "Kills"
            //int deaths = 6; // "Deaths"
            int kd = players.Max(x => x.KDRatio.ToString("0.0").Length);

            if (kd < 3)
                kd = 3;

            //int ping = 4; // "Ping"

            int maxLength;
            string key;

            if (VerboseOutput)
            {
                maxLength = 39 + tag + name + kd;
                key = $"No. {"Tag".PadLeft(tag)} {"Name".PadRight(name)} Score Teamwork Kills Deaths {"K/D".PadLeft(kd)} Ping";
            }
            else
            {
                maxLength = 11 + name + kd;
                key = $"No. {"Name".PadRight(name)} Score {"K/D".PadLeft(kd)}";
            }

            StringBuilder builder = new StringBuilder();
            int index = 1;
            //⎯
            builder.AppendLine($"```\n{key}\n{new string('─', maxLength)}");

            foreach (var p in players.OrderByDescending(x => x.Score))
            {
                string temp = "";

                if (VerboseOutput)
                    temp = $"{index,2}. {p.Tag.PadLeft(tag)} {p.Name.PadRight(name)} {p.Score,5} {p.Teamwork,8} {p.Kills,5} {p.Deaths,6} {p.KDRatio.ToString("0.0").PadLeft(kd)} {p.Ping,4}";
                else
                    temp = $"{index,2}. {p.Name.PadRight(name)} {p.Score,5} {p.KDRatio.ToString("0.0").PadLeft(kd)}";

                if (builder.Length + temp.Length > 1024)
                {
                    builder.Append("```");
                    output.Add(builder.ToString());
                    builder.Clear();
                    builder.AppendLine($"```\n{key}\n{new string('_', maxLength)}");
                }

                builder.AppendLine(temp);
                index++;
            }

            builder.Append("```");

            output.Add(builder.ToString());

            return output.ToArray();
        }

        [Command("servers")]
        private async Task GetServers(int psuedoPage = 1)
        {
            if (psuedoPage < 0)
            {
                await RespondAsync("You cannot use negative values");
                return;
            }

            if (psuedoPage == 0)
                psuedoPage = 1;

            var position = psuedoPage * 10;
            var pageOffset = (position - 10) / 50;
            if (pageOffset > 0)
                position %= 50;


            string content = "";
            List<BF2Server> servers;
            BF2Status stats;
            var totalPages = 0;

            try
            {
                stats = JsonConvert.DeserializeObject<BF2Status>(await GetData($"livestats"));
            }
            catch (Exception ex)
            {
                await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            try
            {
                servers = JsonConvert.DeserializeObject<List<BF2Server>>(await GetData($"servers"));
            }
            catch (Exception ex)
            {
                await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            totalPages = stats.ServerCount / 10;

            if (stats.ServerCount % 10 > 0)
                totalPages++;

            EmbedBuilder builder = new EmbedBuilder();

            builder.Title = $"BF2 Servers, Page {psuedoPage}/{totalPages}";
            builder.Description= $"**Servers:** {stats.ServerCount}\n**Players:** {stats.PlayerCount}\n⠀"; // extra linebreak with blank unicode character (U+2800) for extra padding on mobile

            StringBuilder output = new StringBuilder();
            //output.AppendLine($"Page {psuedoPage}/{totalPages * 5}");

            int i = 0;
            foreach (var s in servers.Skip(position - 10))
            {
                //output.AppendLine($"{s.Name} `{s.IP}:{s.GamePort}` {s.OnlinePlayers}/{s.MaxPlayers}");
                builder.AddField(new EmbedFieldBuilder().WithIsInline(true).WithName($"**{s.Name}**")
                    .WithValue($"`{s.IP}:{s.GamePort}`" +
                                $"\n**Players:** {s.OnlinePlayers}**/**{s.MaxPlayers}" +
                                $"{(s.OnlinePlayers > 0 && s.Players.Count(x => x.AIBot) > 0? $" ({s.Players.Count(x => x.AIBot)} bots)" : "")}" + // Display bot count, but only if there's real players online
                                $"\n**Password:** {s.Password}\n⠀")); // same as above

                i++;
                if (i > 9)
                    break;
            }

            //await RespondAsync(output.ToString());
            await ReplyAsync(embed: builder.Build());
        }

        [Command("server")]
        private async Task GetServer(string address, [Remainder]string remainder = "")
        {
            if (!ValidateAddress(address))
            {
                await RespondAsync($"{address} doesn't appear to be a valid server address.");
                return;
            }

            BF2Server server;

            try
            {
                server = JsonConvert.DeserializeObject<BF2Server>(await GetData($"servers/{address}"));
            }
            catch (Exception ex)
            {
                await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            if (remainder != "")
            {
                if (server.Players.Count() == 0)
                {
                    await ReplyAsync("That server is empty, or does not report scoreboard information.");
                    return;
                }

                remainder = remainder.ToLower();

                int teamIndex = 0;
                bool verbose = false;

                if (remainder.Contains("verbose"))
                {
                    verbose = true;
                    remainder = remainder.Remove("verbose").Trim();
                }
                
                switch (remainder)
                {
                    case "1":
                    case "one":
                    case "team1":
                    case "team 1":
                    case "team one":
                        teamIndex = 1;
                        break;
                    case "2":
                    case "two":
                    case "team2":
                    case "team 2":
                    case "team two":
                        teamIndex = 2;
                        break;
                    case "mec":
                    case "us":
                    case "ch":
                        if (server.Team1 == remainder.ToUpper())
                            teamIndex = 1;
                        else if (server.Team2 == remainder.ToUpper())
                            teamIndex = 2;
                        else
                            teamIndex = -1;
                        break;
                    default:
                        teamIndex = -1;
                        break;
                }

                if (teamIndex == -1)
                {
                    await RespondAsync($"There is no team associated with `{remainder}` in this game.");
                    return;
                }

                
                var scores = FormatScoreBoard(server.Players.Where(x => x.TeamIndex == teamIndex), verbose);
                List<EmbedFieldBuilder> pages = new List<EmbedFieldBuilder>();

                for (int i = 0; i < scores.Length; i++)
                {
                    if (i == 0)
                        pages.Add(new EmbedFieldBuilder().WithIsInline(false).WithName($"**Team {teamIndex}:** {GetFlag(teamIndex == 1 ? server.Team1 : server.Team2)}").WithValue(scores[i]));
                    else
                        pages.Add(new EmbedFieldBuilder().WithIsInline(false).WithName($"[Page {i + 1}]").WithValue(scores[i]));
                }


                await ReplyAsync(embed:
                    builder
                        .WithTitle($"{server.Name} | {server.IP}:{server.GamePort}")
                        .WithDescription($"**{server.MapName} - {server.OnlinePlayers}/{server.MaxPlayers}**")
                        .WithFields(pages)
                        .Build()
                    );

                return;
            }

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
                    $"{(server.Bots ? $"\n**Bot count:** {server.Players.Count(x => x.AIBot)}" : "")}" +
                    $"\n**Global Unlocks:** {server.GlobalUnlocks}" +
                    $"\n**Server FPS:** {server.FPS}" +
                    $"\n**Vehicles: {(server.NoVehicles == 0 ? "True" : "False")}**" +
                    $"\n**Dedicated Server:** {server.Dedicated}" +
                    $"\n**Ranked:** {server.Ranked}" +
                    $"\n**Server OS:** {server.OS}" +
                    $"\n**Battle Recorder available:** {(server.BattleRecorder ? $"[Link]({server.DemoDownload})" : "False")}")
                    .Build()
                );
        }

        [Command("player")]
        private async Task GetPlayer(string username)
        {
            BF2Player player;
            BF2Server server;

            try
            {
                player = JsonConvert.DeserializeObject<BF2Player>(await GetData($"players/{HttpUtility.UrlEncode(username)}")); // Unsure if UrlEncode will break certain usernames.
            }
            catch (Exception ex)
            {
                if (ex.Message == "Player not found")
                    await RespondAsync($"Player name {username} does not exist, or player is not online.");
                else
                    await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            try
            {
                server = JsonConvert.DeserializeObject<BF2Server>(await GetData($"players/{HttpUtility.UrlEncode(username)}/server"));
            }
            catch (Exception ex)
            {
                await RespondAsync($"Something went wrong: {ex.Message}");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            await ReplyAsync(embed: builder
                .WithTitle($"**{player.Tag}{(player.Tag.Length > 0? " " : "")}{player.Name}**") // ternary operator to add space *only* if a tag exists
                .WithDescription($"**Score:** {player.Score}" +
                                $"\n**Kills:** {player.Kills}" +
                                $"\n**Deaths:** {player.Deaths}" +
                                $"{(player.AIBot ? "\n**AI Bot:** True" : "")}" +
                                $"\n**Ping:** {player.Ping}" +
                                $"\n**Team:** {GetFlag(player.TeamLabel)}" +
                                $"\n\n**Server:** {server.Name}" +
                                $"\n**Address:** {server.IP}:{server.GamePort}" +
                                $"\n**Players:** {server.OnlinePlayers}/{server.MaxPlayers}")
                .WithFooter($"Player ID: {player.PlayerID}")
                .Build());
        }
    }

    public class BF2Status
    {
        [JsonProperty("servers")]
        public int ServerCount { get; set; }
        [JsonProperty("players")]
        public int PlayerCount { get; set; }
    }

    public class BF2Team
    {
        [JsonProperty("index")]
        public int Index { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class BF2Player
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
        public List<BF2Team> Teams { get; set; }
        [JsonProperty("players")]
        public List<BF2Player> Players { get; set; }
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
        [JsonProperty("coopBotRatio")]
        public int CoopBotRatio { get; set; }
        [JsonProperty("coopBotCount")]
        public int CoopBotCount { get; set; }
        [JsonProperty("coopBotDiff")]
        public int CoopBotDiff { get; set; }
        [JsonProperty("noVehicles")]
        public int NoVehicles { get; set; }
    }


}
