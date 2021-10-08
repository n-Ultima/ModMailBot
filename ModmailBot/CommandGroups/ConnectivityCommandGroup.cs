using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Doraemon.CommandGroups {
    public class ConnectivityCommandGroup : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly MessageContext _messageContext;
        public ConnectivityCommandGroup(IDiscordRestChannelAPI channelApi, MessageContext messageContext)
        {
            _channelApi = channelApi;
            _messageContext = messageContext;
        }

        [Command("ping")]
        [Description("Replies with pong.")]
        public async Task<IResult> PingAsync()
        {
            var time = DateTimeOffset.UtcNow - _messageContext.Message.Timestamp.Value;
            var result = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, $"🏓 Pong! {time.Milliseconds} ms", ct: CancellationToken);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
            
        }
    }
}