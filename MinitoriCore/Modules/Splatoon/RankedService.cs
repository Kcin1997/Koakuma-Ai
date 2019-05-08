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

namespace MinitoriCore.Modules.Splatoon
{
    public class RankedService
    {
        public string GetRankedMode()
        {
            return "Clam Blitz";

            // Todo: actually make it rotate
        }

        public RankedService()
        {

        }
    }
}
