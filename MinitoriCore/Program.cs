using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.IO;
using Microsoft.Extensions.DependencyInjection;

namespace MinitoriCore
{
    class Program
    {
        static void Main(string[] args) =>
            new Program().RunAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;
        private Config config;
        private CommandHandler handler;
        private RandomStrings strings;
        //private IServiceProvider services;
        //private readonly IDependencyMap map = new DependencyMap();
        //private readonly CommandService commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });

        private async Task RunAsync()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose
            });
            client.Log += Log;

            config = Config.Load();
            strings = new RandomStrings();

            //var map = new DependencyMap();
            var map = new ServiceCollection().AddSingleton(client).AddSingleton(config).AddSingleton(strings).BuildServiceProvider();

            //await ConfigureServicesAsync(map);

            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();
            
            client.UserJoined += Client_UserJoined;

            handler = new CommandHandler();
            await handler.Install(map);

            //await client.CurrentUser.ModifyAsync(x => x.Avatar = new Image(File.OpenRead("Minitori.png")));

            await Task.Delay(-1);
        }

        private async Task Client_UserJoined(SocketGuildUser user)
        {
            await Task.Delay(500);

            if (user.Guild.Id == 110373943822540800 && user.IsBot)
                await user.AddRoleAsync(user.Guild.GetRole(318748748010487808));
        }
        
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
