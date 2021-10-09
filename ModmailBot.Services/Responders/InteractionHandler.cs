using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace ModmailBot.Services.Responders
{
    public class InteractionHandler : IResponder<IInteractionCreate>
    {
        private readonly IDiscordRestInteractionAPI _interactionApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        
        public static Snowflake CurrentUserId { get; set; }
        public static bool? Confirmed { get; set; }
        
        public InteractionHandler(IDiscordRestInteractionAPI interactionApi, IDiscordRestChannelAPI channelApi)
        {
            _interactionApi = interactionApi;
            _channelApi = channelApi;
        }

        public async Task<Result> RespondAsync(IInteractionCreate gatewayEvent, CancellationToken ct)
        {
            if (gatewayEvent.Type != InteractionType.MessageComponent)
            {
                return Result.FromSuccess();
            }

            var message = gatewayEvent.Message.Value;

            await _interactionApi.CreateInteractionResponseAsync(gatewayEvent.ID, gatewayEvent.Token, new InteractionResponse(InteractionCallbackType.DeferredUpdateMessage), ct);
            if (gatewayEvent.Data.Value.CustomID == "Confirm")
            {
                Confirmed = true;
                await _channelApi.EditMessageAsync(gatewayEvent.ChannelID.Value, message.ID, "Confirmation received.", components: Array.Empty<IMessageComponent>(), ct: ct);
                CurrentUserId = default;
            }

            if (gatewayEvent.Data.Value.CustomID == "Cancel")
            {
                Confirmed = false;
                await _channelApi.EditMessageAsync(gatewayEvent.ChannelID.Value, message.ID, "Cancellation received.", components: Array.Empty<IMessageComponent>(), ct: ct);
                CurrentUserId = default;
            }
            return Result.FromSuccess();
        }
    }
}