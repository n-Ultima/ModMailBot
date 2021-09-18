using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Modmail.Common;
using Modmail.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Responders;
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
        private readonly UserService _userService;
        private readonly SnippetService _snippetService;

        public ModmailCommandGroup(ICommandContext context, IDiscordRestChannelAPI channelApi, ModmailTicketService modmailTicketService, IDiscordRestGuildAPI guildApi, MessageContext messageContext, UserService userService, SnippetService snippetService)
        {
            _context = context;
            _channelApi = channelApi;
            _modmailTicketService = modmailTicketService;
            _guildApi = guildApi;
            _messageContext = messageContext;
            _userService = userService;
            _snippetService = snippetService;
        }
        public ModmailConfiguration ModmailConfig = new();

        [Command("reply", "respond", "r")]
        [Description("Replies to a modmail thread.")]
        public async Task<Result> RespondAsync([Greedy] string content)
        {
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
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
                if (!attachmentResult.IsSuccess)
                {
                    return Result.FromError(attachmentResult.Error);
                }
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _context.User.ID, content);
                var successAttachmentResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Here's what was sent to the user:", embeds: new[] {attachmentEmbed}, ct: CancellationToken);
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
            var dmResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: CancellationToken);
            if (!dmResult.IsSuccess)
            {
                return Result.FromError(dmResult.Error);
            }
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _context.User.ID, content);
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Here's what was sent to the user:", embeds: new[] {embed}, ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("close", "end")]
        [Description("Closes the modmail ticket.")]
        public async Task<Result> CloseTicketAsync([Greedy] string reason = null)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_context.GuildID.Value, _context.User.ID);
            if (!TryAuthenticateUser(executor.Entity, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
            var guild = await _guildApi.GetGuildAsync(_context.GuildID.Value);
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_context.ChannelID);
            if (modmailTicket == null)
            {
                return Result.FromError(new ExceptionError(new Exception("This command can only be ran in modmail ticket channels.")));
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"(SYSTEM){modmailTicket.UserId} opened a modmail thread.");
            var messages = await _modmailTicketService.FetchModmailMessagesAsync(modmailTicket.Id);
            foreach (var message in messages)
            {
                var user = await _guildApi.GetGuildMemberAsync(new Snowflake(ModmailConfig.MainServerId), message.AuthorId);
                stringBuilder.AppendLine($"{user.Entity.User.Value.Tag()} - {message.Content}");
            }

            var memoryStream = new MemoryStream();
            var encoding = new UTF8Encoding(true);
            var info = encoding.GetBytes(stringBuilder.ToString());
            memoryStream.Write(info, 0, info.Length);
            memoryStream.Position = 0;
            await _channelApi.CreateMessageAsync(new Snowflake(ModmailConfig.LogChannelId), content: $"Closed by {executor.Entity.User.Value.Tag()}", file: new FileData($"Modmail Ticket ID {modmailTicket.Id}.txt", memoryStream), ct: CancellationToken);
            var closedTicketEmbed = new Embed
            {
                Title = "Thread Closed",
                Colour = Color.Red,
                Description = $"{executor.Entity.User.Value.Mention()} has closed this Modmail thread.",
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter("Replying will create a new thread.", IconUrl: guild.Entity.GetAbsoluteIconUrl() ?? null)
            };
            await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {closedTicketEmbed}, ct: CancellationToken);
            await _channelApi.DeleteChannelAsync(modmailTicket.ModmailThreadChannelId, $"Ticket closed by {executor.Entity.User.Value.Tag()}", CancellationToken);
            await _modmailTicketService.DeleteModmailTicketAsync(modmailTicket);
            return Result.FromSuccess();
        }
        [Command("block")]
        [Description("Blocks a user from contacting modmail.")]
        public async Task<Result> BlockUserAsync(IGuildMember member, [Greedy] string reason)
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

            await _userService.BlacklistUserAsync(member.User.Value.ID);
            await _channelApi.CreateMessageAsync(new Snowflake(ModmailConfig.LogChannelId), $"**{executor.Entity.User.Value.Tag()}** blocked **{member.User.Value.Tag()}**. Reason:\n```\n{reason ?? "Not specified"}\n```", ct: CancellationToken);
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, $"**{member.User.Value.Tag()}** has been blocked.", ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        private async Task<bool> UserOutranksUser(IGuild guild, IGuildMember user1, IGuildMember user2)
        {
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
        
        private bool TryAuthenticateUser(IGuildMember member, PermissionLevel permissionLevel)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (permissionLevel == null)
            {
                throw new ArgumentNullException(nameof(permissionLevel));
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
    }
}