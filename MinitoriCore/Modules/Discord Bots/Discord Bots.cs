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

namespace MinitoriCore.Modules.DiscordBots
{
    public class DiscordBots : ModuleBase
    {
        // Non-testing 132106771975110656
        // Full mute 132106637614776320
        // Unverified 318748748010487808

        // No emoji 241256979840892939
        // No embed 178823209217556480

        [Command("mute")]
        [Summary("Place a non-testing mute on a bot")]
        public async Task NormalMute([Remainder]string remainder = "")
        {
            if (Context.Guild.Id != 110373943822540800)
                return;

            if (((IGuildUser)Context.User).RoleIds.ToList().Contains(407326634819977217) ||
                ((IGuildUser)Context.User).RoleIds.ToList().Contains(113379036524212224))
            {
                var args = remainder.Split(' ').Where(x => x.Length > 0).ToList();
                string reason = "";
                var users = new List<ulong>();
                //((SocketGuild)Context.Guild).GetUser

                int i = 0;
                foreach (var s in new List<string>(args))
                {
                    var id = s.TrimStart('<').TrimStart('@').TrimEnd('>');
                    ulong temp;
                    if (ulong.TryParse(id, out temp))
                    {
                        users.Add(temp);
                        args.RemoveAt(i);
                        i++;
                    }
                    else
                        break;
                }

                if (users.Count() == 0)
                {
                    await ReplyAsync("You need to mention something for this to work!");
                    return;
                }

                reason = $"[ Mute by {Context.User.Username}#{Context.User.Discriminator} ] {string.Join(" ", args)}".Trim();

                StringBuilder output = new StringBuilder();
                output.AppendLine("Non-testing muted the following bots:");

                int mutedUsers = 0;
                var role = Context.Guild.GetRole(132106771975110656);

                foreach (var u in users)
                {
                    var user = await Context.Guild.GetUserAsync(u);

                    if (user == null)
                        continue;

                    if (!user.IsBot)
                        continue;

                    mutedUsers++;

                    // TODO: Add a catch
                    await user.AddRoleAsync(role, new RequestOptions() { AuditLogReason = reason });
                    output.Append($"**{user.Username}#{user.Discriminator}**, ");
                }

                if (mutedUsers == 0)
                {
                    await ReplyAsync("None of those are bots!");
                    return;
                }

                await ReplyAsync(output.ToString().Trim().TrimEnd(','));
            }
        }
    }
}
