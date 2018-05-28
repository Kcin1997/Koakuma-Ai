using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace MinitoriCore.Preconditions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireGuildAttribute : PreconditionAttribute
    {
        private readonly ulong _guildId;

        public RequireGuildAttribute(ulong guildId)
        {
            _guildId = guildId;
        }

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo commandInfo, IServiceProvider service)
        {
            var mContext = context as MinitoriContext;
            
            if (mContext == null || !mContext.IsHelp)
                return Task.FromResult(PreconditionResult.FromSuccess());

            if (mContext.Guild == null)
                return Task.FromResult(PreconditionResult.FromError("This command requires a guild to run."));

            if (mContext.Guild.Id == _guildId)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else
                return Task.FromResult(PreconditionResult.FromError("This command cannot be run in this guild"));
        }
    }
}
