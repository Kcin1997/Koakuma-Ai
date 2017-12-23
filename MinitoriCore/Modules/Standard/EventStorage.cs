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

namespace MinitoriCore.Modules.Standard
{
    public class EventStorage
    {
        [JsonProperty("events")]
        public Dictionary<ulong, Dictionary<ulong, SnowballStats>> stats { get; set; }
        // <Guild ID, <User ID, Stats page>>

        [JsonIgnore]
        public Dictionary<ulong, Dictionary<ulong, SnowballStats>> oldStats { get; set; }

        [JsonIgnore]
        public Dictionary<ulong, Dictionary<ulong, DateTime>> cooldown = new Dictionary<ulong, Dictionary<ulong, DateTime>>();
        // <Guild ID, <User ID, Time last used>>

        public static EventStorage Load()
        {
            if (File.Exists("events.json"))
            {
                var json = File.ReadAllText("events.json");
                return JsonConvert.DeserializeObject<EventStorage>(json);
            }
            var events = new EventStorage();
            events.Save();

            return events;
        }

        public void Save()
        {
            JsonStorage.SerializeObjectToFile(this, "events.json").Wait();
        }

        public EventStorage()
        {
            stats = new Dictionary<ulong, Dictionary<ulong, SnowballStats>>();
            oldStats = new Dictionary<ulong, Dictionary<ulong, SnowballStats>>();

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (oldStats.All(x =>
                        {
                            Dictionary<ulong, SnowballStats> v;

                            if (stats.TryGetValue(x.Key, out v))
                                return x.Value.Equals(v);

                            return false;
                        }))
                        {
                            oldStats = new Dictionary<ulong, Dictionary<ulong, SnowballStats>>(stats);

                            Save();

                        }
                    }
                    catch (Exception ex)
                    {
                        // add logging pls
                        Console.WriteLine($"Error saving!\n{ex.Message}\n{ex.Source}");
                    }

                    await Task.Delay(1000 * 60);
                }
            });
        }
    }

    public class SnowballStats
    {
        public int Hits = 0;
        public int Misses = 0;
        public int Caught = 0;
        public int Downed = 0;
        public int Dodged = 0;
    }
}
