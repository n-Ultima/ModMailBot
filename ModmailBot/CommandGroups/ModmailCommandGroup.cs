using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModmailBot.Common;
using ModmailBot.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Parsers;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Core;
using Remora.Results;
using Serilog;

namespace ModmailBot.CommandGroups
{
    public enum PermissionLevel
    {
        Moderator,
        Administrator
    }

    public class ModmailCommandGroup : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly ModmailTicketService _modmailTicketService;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly MessageContext _messageContext;

        public ModmailCommandGroup(IDiscordRestChannelAPI channelApi, ModmailTicketService modmailTicketService, IDiscordRestGuildAPI guildApi, MessageContext messageContext)
        {
            _channelApi = channelApi;
            _modmailTicketService = modmailTicketService;
            _guildApi = guildApi;
            _messageContext = messageContext;
        }
        public ModmailConfiguration ModmailConfig = new();

        [Command("reply", "respond", "r")]
        [Description("Replies to a modmail thread.")]
        public async Task<IResult> RespondAsync([Greedy] string content)
        {
            var guildMember = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(guildMember.Entity, guild.Entity,everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_messageContext.ChannelID);
            if (modmailTicket == null)
            {
                throw new Exception("This command can only be ran inside of modmail ticket channels.");
            }
            string highestRoleName;
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
                    Colour = Color.LimeGreen,
                    Author = _messageContext.User.WithUserAsAuthor(),
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
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _messageContext.User.ID, content);
                var successAttachmentResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Here's what was sent to the user:", embeds: new[] {attachmentEmbed}, ct: CancellationToken);
                return successAttachmentResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(attachmentResult.Error);
            }
            var embed = new Embed
            {
                Colour = Color.LimeGreen,
                Author = _messageContext.User.WithUserAsAuthor(),
                Description = content,
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter(highestRoleName),
            };
            var dmResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: CancellationToken);
            if (!dmResult.IsSuccess)
            {
                return Result.FromError(dmResult.Error);
            }
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _messageContext.User.ID, content);
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Here's what was sent to the user:", embeds: new[] {embed}, ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("close", "end")]
        [Description("Closes the modmail ticket.")]
        public async Task<IResult> CloseTicketAsync([Greedy] string reason = null)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_messageContext.ChannelID);
            if (modmailTicket == null)
            {
                throw new Exception("This command can only be ran inside of modmail ticket channels.");
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

        [Command("edit")]
        [Description("Edits a modmail message sent(use on the message you sent, not on the embed generated by the bot).")]
        public async Task<IResult> EditModmailMessageAsync(Snowflake messageId, [Greedy] string newMessageContent)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_messageContext.ChannelID);
            if (modmailTicket == null)
            {
                throw new Exception("This command can only be ran inside of modmail ticket channels.");
            }
            var message = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, messageId);
            if (!message.IsSuccess)
            {
                throw new Exception("The message ID provided is not valid.");
            }

            if (message.Entity.Author.ID != _messageContext.User.ID)
            {
                throw new Exception("You cannot edit a message that was not authored by you.");
            }

            var modmailMessages = await _modmailTicketService.FetchModmailMessagesAsync(modmailTicket.Id);
            var oldMessage = modmailMessages
                .Where(x => x.MessageId == message.Entity.ID)
                .SingleOrDefault();
            if (oldMessage == null)
            {
                Log.Logger.Error("Message ID {messageId} not found in the database.", messageId);
                throw new Exception("There was an issue attempting to edit this message, please see the console.");
            }
            var embed = new Embed
            {
                Timestamp = DateTimeOffset.UtcNow,
                Colour = Color.Gold,
                Title = "Message Edited",
                Description = $"**Before:** {oldMessage.Content}\n**After:** {newMessageContent}",
                Author = _messageContext.User.WithUserAsAuthor()
            };

            var messageResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: CancellationToken);
            if (!messageResult.IsSuccess)
            {
                return Result.FromError(messageResult.Error);
            }

            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, message.Entity.ID, message.Entity.Author.ID, $"(SYSTEM) Message Edited:\nB: {message.Entity.Content}\n A: {newMessageContent}");
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Message successfully edited.", ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("move")]
        [Description("Move the modmail ticket over to another category.")]
        public async Task<IResult> MoveTicketAsync(Snowflake categoryId)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            if (!ModmailConfig.AllowMove)
            {
                return Result.FromSuccess();
            }
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_messageContext.ChannelID);
            if (modmailTicket == null)
            {
                throw new Exception("This command can only be ran inside of modmail ticket channels.");
            }

            var channel = await _channelApi.GetChannelAsync(categoryId, CancellationToken);
            if (!channel.IsSuccess || channel.Entity.Type != ChannelType.GuildCategory)
            {
                throw new Exception("The category ID provided is not valid.");
            }
            
            var result = await _channelApi.ModifyChannelAsync(_messageContext.ChannelID, parentId: categoryId, ct: CancellationToken);
            if (!result.IsSuccess)
            {
                return Result.FromError(result.Error);
            }

            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _messageContext.User.ID, $"(SYSTEM){_messageContext.User.Tag()} moved the ticket to category {channel.Entity.Name.Value}");
            var success = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Ticket successfully moved.", ct: CancellationToken);
            return success.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(success.Error);
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