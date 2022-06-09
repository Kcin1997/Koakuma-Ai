using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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


        [SlashCommand("roll", "Roll Dice")]
        public async Task DiceRoller(string rollCommand)
        {
            if (RegEx.isRoll(rollCommand))
            {
                try
                {
                    int commentIndex = rollCommand.IndexOf('#');
                    string roll = (commentIndex == -1) ? rollCommand : rollCommand[..commentIndex];
                    string title = (commentIndex != -1) ? rollCommand[(commentIndex + 1)..] : "Roll";
                    String result = RegEx.parseRolls(roll);
                    var finalValue = new DataTable().Compute(result, null);
                    await RespondAsync($"Rolling ``{roll}``\n__{title}__ : ``{result}``\n = **{finalValue}**");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);
                    throw;
                }

            }
            else
                await RespondAsync("Not a valid roll command.  For performance reasons, rolling more than 99 dice is not allowed.");
        }
    }
}
