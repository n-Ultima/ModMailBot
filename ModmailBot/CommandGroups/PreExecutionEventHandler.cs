using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModmailBot.Common;
using ModmailBot.Services;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Results;

namespace ModmailBot.CommandGroups
{
    public class PreExecutionEventHandler : IPreExecutionEvent
    {
        private readonly UserService _userService;
        public ModmailConfiguration ModmailConfig = new();
        public PreExecutionEventHandler(UserService userService)
            => _userService = userService;
        public async Task<Result> BeforeExecutionAsync(ICommandContext context, CancellationToken ct = new CancellationToken())
        {
            if (!context.GuildID.HasValue)
            {
                return Result.FromError(new ExceptionError(new Exception("Commands can only be ran in the guild.")));
            }
            var bannedIds = await _userService.FetchBlacklistedUsersAsync();
            if (bannedIds.Contains(context.User.ID))
            {
                return Result.FromError(new ExceptionError(new Exception("User is blacklisted from using the bot.")));
            }

            if (context.GuildID.Value != new Snowflake(ModmailConfig.InboxServerId))
            {
                return Result.FromError(new ExceptionError(new Exception("Not in the inbox guild.")));
            }
            return Result.FromSuccess();
        }
    }
}