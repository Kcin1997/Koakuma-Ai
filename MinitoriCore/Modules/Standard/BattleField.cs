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
                var result = await client.GetAsync($"https://api.bflist.io/bf2/v1/{endPoint}");
                if (result.IsSuccessStatusCode)
                    return await result.Content.ReadAsStringAsync();
            }

            return "";
        }


        [Command("servers")]
        private async Task GetServers(int page = 0)
        {
            var servers = JsonConvert.DeserializeObject<List<BF2Server>>(await GetData($"servers{(page > 0? $"/{page}" : "")}"));

            StringBuilder output = new StringBuilder();

            foreach (var s in servers)
            {
                output.AppendLine(s.name);
            }

            await RespondAsync(output.ToString());
        }
    }

    public class Team
    {
        public int index { get; set; }
        public string label { get; set; }
    }

    public class Player
    {
        public int pid { get; set; }
        public string name { get; set; }
        public string tag { get; set; }
        public int score { get; set; }
        public int kills { get; set; }
        public int deaths { get; set; }
        public int ping { get; set; }
        public int teamIndex { get; set; }
        public string teamLabel { get; set; }
        public bool aibot { get; set; }
    }

    public class BF2Server
    {
        public string ip { get; set; }
        public int queryPort { get; set; }
        public int gamePort { get; set; }
        public string name { get; set; }
        public string mapName { get; set; }
        public int mapSize { get; set; }
        public int numPlayers { get; set; }
        public int maxPlayers { get; set; }
        public List<Team> teams { get; set; }
        public List<Player> players { get; set; }
        public bool password { get; set; }
        public string gameVersion { get; set; }
        public string gameType { get; set; }
        public string gameVariant { get; set; }
        public int timelimit { get; set; }
        public string roundsPerMap { get; set; }
        public bool dedicated { get; set; }
        public bool ranked { get; set; }
        public bool anticheat { get; set; }
        public string os { get; set; }
        public bool battlerecorder { get; set; }
        public string demoIndex { get; set; }
        public string demoDownload { get; set; }
        public bool voip { get; set; }
        public bool autobalance { get; set; }
        public bool friendlyfire { get; set; }
        public string tkmode { get; set; }
        public int startdelay { get; set; }
        public int spawntime { get; set; }
        public string sponsorText { get; set; }
        public string sponsorLogoUrl { get; set; }
        public string communityLogoUrl { get; set; }
        public int scorelimit { get; set; }
        public int ticketratio { get; set; }
        public int teamratio { get; set; }
        public string team1 { get; set; }
        public string team2 { get; set; }
        public bool bots { get; set; }
        public bool pure { get; set; }
        public bool globalUnlocks { get; set; }
        public int fps { get; set; }
        public bool plasma { get; set; }
        public int reservedSlots { get; set; }
        public int coopBotRatio { get; set; }
        public int coopBotCount { get; set; }
        public int coopBotDiff { get; set; }
        public int noVehicles { get; set; }
    }


}
