using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModmailBot.Common;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace ModmailBot.Services.Responders
{
    public class GuildMemberLeftResponder : IResponder<IGuildMemberRemove>
    {
        private readonly ModmailTicketService _modmailTicketService;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestUserAPI _userApi;
        private ModmailConfiguration ModmailConfig = new();
        public GuildMemberLeftResponder(ModmailTicketService modmailTicketService, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi, IDiscordRestUserAPI userApi)
        {
            _modmailTicketService = modmailTicketService;
            _guildApi = guildApi;
            _channelApi = channelApi;
            _userApi = userApi;
        }
        
        public async Task<Result> RespondAsync(IGuildMemberRemove gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var modmailTicket = await _modmailTicketService.FetchModmailTicketAsync(gatewayEvent.User.ID);
            if (modmailTicket == null)
            {
                return Result.FromSuccess();
            }
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"(SYSTEM){modmailTicket.UserId} opened a modmail thread.");
            var messages = await _modmailTicketService.FetchModmailMessagesAsync(modmailTicket.Id);
            foreach (var message in messages)
            {
                var user = await _userApi.GetUserAsync(message.AuthorId);
                stringBuilder.AppendLine($"{user.Entity.Tag()} - {message.Content}");
            }

            var memoryStream = new MemoryStream();
            var encoding = new UTF8Encoding(true);
            var info = encoding.GetBytes(stringBuilder.ToString());
            memoryStream.Write(info, 0, info.Length);
            memoryStream.Position = 0;
            await _channelApi.CreateMessageAsync(new Snowflake(ModmailConfig.LogChannelId), content: $"Closed automatically due to the user leaving.", file: new FileData($"Modmail Ticket ID {modmailTicket.Id}.txt", memoryStream), ct: ct);
            await _modmailTicketService.DeleteModmailTicketAsync(modmailTicket);
            return Result.FromSuccess();
        }
    }
}