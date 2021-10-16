using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Humanizer;
using ModmailBot.Common;
using Polly.CircuitBreaker;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Rest;
using Remora.Results;

namespace ModmailBot.Services.Responders
{
    public class PrivateMessageReceivedHandler : ModmailService, IResponder<IMessageCreate>
    {
        //private readonly MessageContext _messageContext;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly UserService _userService;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly ModmailTicketService _modmailTicketService;
        public ModmailConfiguration ModmailConfig = new();
        public PrivateMessageReceivedHandler(IServiceProvider serviceProvider, IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, UserService userService, ModmailTicketService modmailTicketService)
            : base(serviceProvider)
        {
            _channelApi = channelApi;
            _userService = userService;
            _modmailTicketService = modmailTicketService;
            _guildApi = guildApi;
        }

        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = new())
        {
            // Only listen to DM's
            if (gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Author.IsBot.HasValue)
            {
                return Result.FromSuccess();
            }
            // Make sure they aren't blacklisted.
            var blacklistedUsers = await _userService.FetchBlacklistedUsersAsync();
            if (blacklistedUsers.Contains(gatewayEvent.Author.ID))
            {
                return Result.FromSuccess();
            }
            var dmModmail = await _modmailTicketService.FetchModmailTicketAsync(gatewayEvent.Author.ID);
            if (dmModmail == null)
            {
                InteractionHandler.Confirmed = null;
                var modmailGuild = await _guildApi.GetGuildAsync(new Snowflake(ModmailConfig.MainServerId), ct: ct);
                var inboxGuild = await _guildApi.GetGuildAsync(new Snowflake(ModmailConfig.InboxServerId), ct: ct);
                var modmailGuildMember = await _guildApi.GetGuildMemberAsync(modmailGuild.Entity.ID, gatewayEvent.Author.ID, ct);
                var id = Guid.NewGuid();
                if (ModmailConfig.ConfirmThreadCreation)
                {
                    List<IMessageComponent> components = new();
                    components.Add(new ActionRowComponent(new[]
                    {
                        new ButtonComponent(ButtonComponentStyle.Success, "Open Thread", CustomID: "Confirm"),
                        new ButtonComponent(ButtonComponentStyle.Danger, "Cancel", CustomID: "Cancel")
                    }));
                    // Assume we have to confirm the creation
                    var confirmMessage = await _channelApi.CreateMessageAsync(gatewayEvent.ChannelID, "Are you sure you want to open a ticket? Please confirm below within 30 seconds.", components: components, ct: ct);
                    if (!confirmMessage.IsSuccess)
                    {
                        return Result.FromError(confirmMessage.Error);
                    }

                    InteractionHandler.CurrentUserId = gatewayEvent.Author.ID;
                    while (InteractionHandler.Confirmed == null)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            if (InteractionHandler.Confirmed != null)
                            {
                                break;
                            }

                            await Task.Delay(1000);
                        }

                        if (InteractionHandler.Confirmed != null)
                        {
                            break;
                        }
                        var edit = await _channelApi.EditMessageAsync(confirmMessage.Entity.ChannelID, confirmMessage.Entity.ID, "Timed out", components: Array.Empty<IMessageComponent>(), ct: ct);
                        if (!edit.IsSuccess)
                        {
                            return Result.FromError(edit.Error);
                        }
                    }

                    if (InteractionHandler.Confirmed == false)
                    {
                        return Result.FromSuccess();
                    }

                    InteractionHandler.Confirmed = null;
                }
                
                var welcomeMessageResult = await _channelApi.CreateMessageAsync(gatewayEvent.ChannelID, ModmailConfig.NewTicketCreationMessage, ct: ct);
                if (!welcomeMessageResult.IsSuccess)
                {
                    return Result.FromError(welcomeMessageResult.Error);
                }

                string channelName = gatewayEvent.Author.Username + "-" + gatewayEvent.Author.Discriminator.ToString("0000");
                var createdModmailChannel = await _guildApi.CreateGuildChannelAsync(inboxGuild.Entity.ID, channelName, ChannelType.GuildText, gatewayEvent.Author.ID.ToString(), parentID: new Snowflake(ModmailConfig.ModmailCategoryId), ct: ct);
                var embed = new Embed
                {
                    Author = gatewayEvent.Author.WithUserAsAuthor(),
                    Colour = Color.Gold,
                    Description = gatewayEvent.Content,
                    Timestamp = DateTimeOffset.UtcNow,
                    Footer = new EmbedFooter($"Message ID: {gatewayEvent.ID}")
                };
                var roles = await _guildApi.GetGuildRolesAsync(modmailGuild.Entity.ID, ct);
                var memberRoles = roles.Entity
                    .Where(x => modmailGuildMember.Entity.Roles.Contains(x.ID))
                    .Select(x => x.Mention())
                    .ToList();

                var newTicketEmbed = new Embed
                {
                    Title = "New Modmail Thread",
                    Colour = Color.LimeGreen,
                    Description = $"**{gatewayEvent.Author.Tag()}**(created at {gatewayEvent.Author.ID.Timestamp.ToString("d")}) has opened a modmail thread.",
                    Fields = new[]
                    {
                        new EmbedField("Roles", memberRoles.Any() ? memberRoles.Humanize() : "No roles", true),
                        new EmbedField("Joined At", modmailGuildMember.Entity.JoinedAt.ToString("d"), true)
                    },
                };
                await _channelApi.CreateMessageAsync(createdModmailChannel.Entity.ID, embeds: new[] {newTicketEmbed, embed}, ct: ct);
                await _modmailTicketService.CreateModmailTicketAsync(id, gatewayEvent.ChannelID, createdModmailChannel.Entity.ID, gatewayEvent.Author.ID);
                await _modmailTicketService.AddMessageToModmailTicketAsync(id, gatewayEvent.ID, gatewayEvent.Author.ID, $"{gatewayEvent.Content}");
                return Result.FromSuccess();
            }

            var ongoingModmailInboxGuild = await _guildApi.GetGuildAsync(new Snowflake(ModmailConfig.InboxServerId), ct: ct);
            if (gatewayEvent.Attachments.Any())
            {
                var attachment = gatewayEvent.Attachments[0];
                var embedWithAttachments = new Embed
                {
                    Author = gatewayEvent.Author.WithUserAsAuthor(),
                    Colour = Color.Gold,
                    Description = gatewayEvent.Content,
                    Timestamp = DateTimeOffset.UtcNow,
                    Footer = new EmbedFooter($"Message ID: {gatewayEvent.ID}"),
                    Image = new EmbedImage(attachment.Url)
                };
                await _channelApi.CreateMessageAsync(dmModmail.ModmailThreadChannelId, embeds: new[] {embedWithAttachments}, ct: ct);
                await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, gatewayEvent.ID, gatewayEvent.Author.ID, gatewayEvent.Content);
                return Result.FromSuccess();
            }
            var continuedEmbed = new Embed
            {
                Author = gatewayEvent.Author.WithUserAsAuthor(),
                Colour = Color.Gold,
                Description = gatewayEvent.Content,
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter($"Message ID: {gatewayEvent.ID}"),
            };
            await _channelApi.CreateMessageAsync(dmModmail.ModmailThreadChannelId, embeds: new[] {continuedEmbed}, ct: ct);
            await _modmailTicketService.AddMessageToModmailTicketAsync(dmModmail.Id, gatewayEvent.ID, gatewayEvent.Author.ID, gatewayEvent.Content);
            return Result.FromSuccess();
        }
    }
}