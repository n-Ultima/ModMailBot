﻿using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ModmailBot.Common;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace ModmailBot.Services.Responders
{
    public class GuildMessageReceivedHandler : IResponder<IMessageCreate>
    {
        public ModmailConfiguration ModmailConfig = new();
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly ModmailTicketService _modmailTicketService;


        public GuildMessageReceivedHandler(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, ModmailTicketService modmailTicketService)
        {
            _channelApi = channelApi;
            _guildApi = guildApi;
            _modmailTicketService = modmailTicketService;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            // Only listen to messages from channels in the modmail category.
            
            var channel = await _channelApi.GetChannelAsync(gatewayEvent.ChannelID, ct);
            if (!channel.Entity.ParentID.HasValue)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Author.IsBot.HasValue)
            {
                return Result.FromSuccess();
            }
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(gatewayEvent.ChannelID);
            // Only listen to actual "thread" channels.
            if (modmailTicket == null)
            {
                return Result.FromSuccess();
            }

            // Don't respond if stated not to in config.json
            if (!ModmailConfig.ReplyToTicketsWithoutCommand)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Content.StartsWith(ModmailConfig.Prefix))
            {
                return Result.FromSuccess();
            }
            string highestRoleName;
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), ct);
            var memberHighestRole = guildRoles.Entity
                .Where(x => gatewayEvent.Member.Value.Roles.Value.Contains(x.ID))
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

            if (gatewayEvent.Attachments.Any())
            {
                var attachment = gatewayEvent.Attachments[0];
                var attachmentEmbed = new Embed
                {
                    Colour = Color.LimeGreen,
                    Author = gatewayEvent.Author.WithUserAsAuthor(),
                    Description = gatewayEvent.Content,
                    Timestamp = DateTimeOffset.UtcNow,
                    Footer = new EmbedFooter(highestRoleName),
                    Image = new EmbedImage(attachment.Url)
                };
                
                var dmAttachmentResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {attachmentEmbed}, ct: ct);
                if (!dmAttachmentResult.IsSuccess)
                {
                    return Result.FromError(dmAttachmentResult.Error);
                }
                await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, gatewayEvent.ID, gatewayEvent.Author.ID, gatewayEvent.Content);
                var successAttachmentResult = await _channelApi.CreateMessageAsync(gatewayEvent.ChannelID, "Here's what was sent to the user:", embeds: new[] {attachmentEmbed}, ct: ct);
                return successAttachmentResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(successAttachmentResult.Error);
            }

            var embed = new Embed
            {
                Colour = Color.LimeGreen,
                Author = gatewayEvent.Author.WithUserAsAuthor(),
                Description = gatewayEvent.Content,
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter(highestRoleName),
            };
            var dmResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: ct);
            if (!dmResult.IsSuccess)
            {
                return Result.FromError(dmResult.Error);
            }
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, gatewayEvent.ID, gatewayEvent.Author.ID, gatewayEvent.Content);
            var successResult = await _channelApi.CreateMessageAsync(gatewayEvent.ChannelID, "Here's what was sent to the user:", embeds: new[] {embed}, ct: ct);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }
    }
}