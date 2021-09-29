using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Modmail.Common;
using Modmail.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
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
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            var user1Roles = guildRoles.Entity
                .Where(x => executor.Entity.Roles.Contains(x.ID))
                .ToList();
            var user2Roles = guildRoles.Entity
                .Where(x => member.Roles.Contains(x.ID))
                .ToList();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            if (!executor.Entity.OutranksUser(member, guild.Entity, user1Roles, user2Roles))
            {
                throw new Exception("Command executor must have a higher hierarchy.");
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
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            var user1Roles = guildRoles.Entity
                .Where(x => executor.Entity.Roles.Contains(x.ID))
                .ToList();
            var user2Roles = guildRoles.Entity
                .Where(x => member.Roles.Contains(x.ID))
                .ToList();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            if (!executor.Entity.OutranksUser(member, guild.Entity, user1Roles, user2Roles))
            {
                throw new Exception("Command executor must have a higher hierarchy.");
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
        
        public async Task<bool> TryAuthenticateUser(IGuildMember member, IGuild guild, IRole everyoneRole, PermissionLevel permissionLevel)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var guildRoles = await _guildApi.GetGuildRolesAsync(guild.ID, CancellationToken);
            var memberRoles = guildRoles.Entity
                .Where(x => member.Roles.Contains(x.ID))
                .ToList();
            var permissions = DiscordPermissionSet.ComputePermissions(member.User.Value.ID, everyoneRole, memberRoles);
            if (permissionLevel == PermissionLevel.Moderator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.ModRoleId)) || member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)) || permissions.HasPermission(DiscordPermission.Administrator))
                {
                    return true;
                }

                return false;
            }

            if (permissionLevel == PermissionLevel.Administrator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)) || permissions.HasPermission(DiscordPermission.Administrator))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}