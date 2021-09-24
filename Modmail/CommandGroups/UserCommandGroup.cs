using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Modmail.Common;
using Modmail.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Doraemon.CommandGroups
{

    public class UserCommandGroup : CommandGroup
    {
        private readonly MessageContext _messageContext;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly ICommandContext _context;
        private readonly UserService _userService;
        private readonly IDiscordRestChannelAPI _channelApi;
        public ModmailConfiguration ModmailConfig = new();

        public UserCommandGroup(MessageContext messageContext, IDiscordRestGuildAPI guildApi, ICommandContext context, UserService userService, IDiscordRestChannelAPI channelApi)
        {
            _messageContext = messageContext;
            _guildApi = guildApi;
            _context = context;
            _userService = userService;
            _channelApi = channelApi;
        }
        
        [Command("block")]
        [Description("Blocks a user from contacting modmail.")]
        public async Task<IResult> BlockUserAsync(IGuildMember member, [Greedy] string reason = null)
        {
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
            var executor = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID);
            var guild = await _guildApi.GetGuildAsync(_context.GuildID.Value);
            if (!TryAuthenticateUser(executor.Entity, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            if (!await UserOutranksUser(guild.Entity, executor.Entity, member))
            {
                return Result.FromError(new ExceptionError(new Exception("Command executor must have a higher hierarchy.")));
            }

            var block = await _userService.BlacklistUserAsync(member.User.Value.ID);
            if (!block.IsSuccess)
            {
                return Result.FromError(block.Error);
            }
            var logResult = await _channelApi.CreateMessageAsync(new Snowflake(ModmailConfig.LogChannelId), $"**{executor.Entity.User.Value.Tag()}** blocked **{member.User.Value.Tag()}**. Reason:\n```\n{reason ?? "Not specified"}\n```", ct: CancellationToken);
            if (!logResult.IsSuccess)
            {
                return Result.FromError(logResult.Error);
            }
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, $"**{member.User.Value.Tag()}** has been blocked.", ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("unblock")]
        [Description("Unblocks a user from contacting modmail.")]
        public async Task<IResult> UnblockUserAsync(IGuildMember member, [Greedy] string reason = null)
        {
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
            var executor = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID);
            var guild = await _guildApi.GetGuildAsync(_context.GuildID.Value);
            if (!TryAuthenticateUser(executor.Entity, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            if (!await UserOutranksUser(guild.Entity, executor.Entity, member))
            {
                return Result.FromError(new ExceptionError(new Exception("Command executor must have a higher hierarchy.")));
            }

            var unblock = await _userService.WhitelistUserAsync(member.User.Value.ID);
            if (!unblock.IsSuccess)
            {
                return Result.FromError(unblock.Error);
            }
            var logResult = await _channelApi.CreateMessageAsync(new Snowflake(ModmailConfig.LogChannelId), $"**{executor.Entity.User.Value.Tag()}** unblocked **{member.User.Value.Tag()}**. Reason:\n```\n{reason ?? "Not specified"}\n```", ct: CancellationToken);
            if (!logResult.IsSuccess)
            {
                return Result.FromError(logResult.Error);
            }
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, $"**{member.User.Value.Tag()}** has been unblocked.", ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }
        
        private bool TryAuthenticateUser(IGuildMember member, PermissionLevel permissionLevel)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }
            
            if (permissionLevel == PermissionLevel.Moderator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.ModRoleId)) || member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)))
                {
                    return true;
                }

                return false;
            }

            if (permissionLevel == PermissionLevel.Administrator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
        private async Task<bool> UserOutranksUser(IGuild guild, IGuildMember user1, IGuildMember user2)
        {
            if (user1.User.Value.ID == user2.User.Value.ID)
                return false;
            if (guild.OwnerID == user1.User.Value.ID)
                return true;
            if (guild.OwnerID == user2.User.Value.ID)
                return false;
            if(user1.Roles.Any() && user2.Roles.Any())
            {
                var guildRoles = await _guildApi.GetGuildRolesAsync(guild.ID, CancellationToken);
                var user1Roles = guildRoles.Entity
                    .Where(x => user1.Roles.Contains(x.ID))
                    .OrderByDescending(x => x.Position)
                    .First();
                var user2Roles = guildRoles.Entity
                    .Where(x => user2.Roles.Contains(x.ID))
                    .OrderByDescending(x => x.Position)
                    .First();
                return user1Roles.Position > user2Roles.Position;   
            }

            if (user1.Roles.Any() && !user2.Roles.Any())
            {
                return true;
            }

            if (!user1.Roles.Any() && user2.Roles.Any())
            {
                return false;
            }
            return false;
        }
    }
}