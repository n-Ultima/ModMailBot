using System;
using System.ComponentModel;
using System.Drawing;
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
    public enum PermissionLevel
    {
        Moderator,
        Administrator
    }

    public class ModmailCommandGroup : CommandGroup
    {
        private readonly ICommandContext _context;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly ModmailTicketService _modmailTicketService;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly MessageContext _messageContext;
        
        public ModmailCommandGroup(ICommandContext context, IDiscordRestChannelAPI channelApi, ModmailTicketService modmailTicketService, IDiscordRestGuildAPI guildApi, MessageContext messageContext)
        {
            _context = context;
            _channelApi = channelApi;
            _modmailTicketService = modmailTicketService;
            _guildApi = guildApi;
            _messageContext = messageContext;
        }
        public ModmailConfiguration ModmailConfig = new();
        [Command("reply", "respond", "r")]
        [Description("Replies to a modmail thread.")]
        public async Task<Result> RespondAsync([Greedy] string content)
        {
            var guildMember = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID, CancellationToken);
            if (!TryAuthenticateUser(guildMember.Entity, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_context.ChannelID);
            if (modmailTicket == null)
            {
                return Result.FromError(new ExceptionError(new Exception("That command can only be ran inside of modmail ticket channels.")));
            }
            string highestRoleName;
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var memberHighestRole = guildRoles.Entity
                .Where(x => guildMember.Entity.Roles.Contains(x.ID))
                .OrderByDescending(x => x.Position)
                .Select(x => x.Name)
                .FirstOrDefault();
            if (memberHighestRole == null)
            {
                highestRoleName = "@everyone";
            }
            else
            {
                highestRoleName = memberHighestRole;
            }

            if (_messageContext.Message.Attachments.Value.Any())
            {
                var attachment = _messageContext.Message.Attachments.Value[0];
                var attachmentEmbed = new Embed
                {
                    Colour = Color.Green,
                    Author = _context.User.WithUserAsAuthor(),
                    Description = content,
                    Timestamp = DateTimeOffset.UtcNow,
                    Footer = new EmbedFooter(highestRoleName),
                    Image = new EmbedImage(attachment.Url)
                };
                var attachmentResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {attachmentEmbed}, ct: CancellationToken);
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _context.User.ID, content);
                return attachmentResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(attachmentResult.Error);
            }
            var embed = new Embed
            {
                Colour = Color.Green,
                Author = _context.User.WithUserAsAuthor(),
                Description = content,
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter(highestRoleName),
            };
            var result = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: CancellationToken);
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _context.User.ID, content);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
        }
        private bool TryAuthenticateUser(IGuildMember member, PermissionLevel permissionLevel)
        {
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
    }
}