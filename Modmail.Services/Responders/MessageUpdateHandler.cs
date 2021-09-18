using System.Threading;
using System.Threading.Tasks;
using Modmail.Common;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Caching;
using Remora.Discord.Caching.Services;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using Serilog;

namespace Modmail.Services.Responders
{
    public class MessageUpdateHandler : IResponder<IMessageUpdate>
    {
        private readonly CacheService _cacheService;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly ModmailTicketService _modmailTicketService;

        public MessageUpdateHandler(CacheService cacheService, IDiscordRestChannelAPI channelApi, ModmailTicketService modmailTicketService)
        {
            _cacheService = cacheService;
            _channelApi = channelApi;
            _modmailTicketService = modmailTicketService;
        }

        public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var key = KeyHelpers.CreateMessageCacheKey(gatewayEvent.ChannelID.Value, gatewayEvent.ID.Value);
            var result = _cacheService.TryGetValue<IMessage>(key, out var oldMessage);
            if (!result)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Author.Value.IsBot.HasValue)
            {
                return Result.FromSuccess();
            }

            var modmailTicket = await _modmailTicketService.FetchModmailTicketAsync(gatewayEvent.Author.Value.ID);
            if (modmailTicket == null)
            {
                return Result.FromSuccess();
            }
            await _channelApi.CreateMessageAsync(modmailTicket.ModmailThreadChannelId, $"**{gatewayEvent.Author.Value.Tag()}** has edited their message.\n`B` {oldMessage.Content}\n`A` {gatewayEvent.Content.Value}", ct: ct);
            await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, "Message edited successfully.", ct: ct);
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, gatewayEvent.ID.Value, gatewayEvent.Author.Value.ID, $"(SYSTEM)Message edited by **{gatewayEvent.Author.Value.Tag()}**\nBefore: {oldMessage.Content}\nAfter: {gatewayEvent.Content.Value}");
            return Result.FromSuccess();
        }
    }
}