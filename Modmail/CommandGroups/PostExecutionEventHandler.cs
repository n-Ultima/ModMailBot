﻿using System;
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
                Log.Logger.Error("An exception bubbled up for command {command}\nError:{error}", _messageContext.Message.Content, commandResult.Error.Message);
                await _channelApi.CreateReactionAsync(context.ChannelID, _messageContext.MessageID, "⚠", ct: ct);
                await _channelApi.CreateMessageAsync(context.ChannelID, $"Error: {commandResult.Error?.Message}", ct: ct);
            }

            return Result.FromSuccess();
        }
    }
}