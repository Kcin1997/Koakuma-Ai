using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Discord;
using Discord.Interactions;
using System.Threading.Tasks;

namespace MinitoriCore.Modules.Standard
{
    public class StandardSlash : InteractionModuleBase
    {
        [SlashCommand("blah", "Blah!")]
        public async Task Blah()
        {
            await RespondAsync($"Blah to you too, {Context.User.Mention}.");
        }

        [SlashCommand("echo", "Echo an input")]
        public async Task Echo(string input)
        {
            await RespondAsync(input);
        }


        override public void OnModuleBuilding(InteractionService interactionService, ModuleInfo module)
        {
            Console.WriteLine("Modules Built");
        }

        override public void BeforeExecute(ICommandInfo info)
        {
            Console.WriteLine($"Command {info.MethodName} Called.");
        }
    }
}
