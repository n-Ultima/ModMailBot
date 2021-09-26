using System;
using System.Threading;
using System.Threading.Tasks;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Results;
using Serilog;

namespace Doraemon.CommandGroups
{
    public class PostExecutionEventHandler : IPostExecutionEvent
    {
        private readonly IDiscordRestChannelAPI _channelApi;

        private readonly MessageContext _messageContext;

        public PostExecutionEventHandler(IDiscordRestChannelAPI channelApi, MessageContext messageContext)
        {
            _channelApi = channelApi;
            _messageContext = messageContext;
        }
        public async Task<Result> AfterExecutionAsync(ICommandContext context, IResult commandResult, CancellationToken ct = new CancellationToken())
        {
            if (!commandResult.IsSuccess)
            {
                if (commandResult.Error.Message.Equals("User is blacklisted from using the bot.") || commandResult.Error.Message.Equals("Commands can only be ran in the guild.") || commandResult.Error.Message.Equals("Not in the inbox guild."))
                {
                    return Result.FromSuccess();
                }
                Log.Logger.Error("An exception bubbled up for command {command}\nError:{error}", _messageContext.Message.Content, commandResult.Error);
                await _channelApi.CreateReactionAsync(context.ChannelID, _messageContext.MessageID, "⚠", ct: ct);
                await _channelApi.CreateMessageAsync(context.ChannelID, $"Error: {commandResult.Error?.Message}", ct: ct);
            }

            return Result.FromSuccess();
        }
    }
}