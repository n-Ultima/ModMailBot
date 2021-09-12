using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using OneOf.Types;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Commands.Services;
using Remora.Commands.Trees;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;
using Serilog;

namespace Doraemon.CommandGroups
{
    public class HelpCommandGroup : CommandGroup
    {
        private readonly CommandService _commandService;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly MessageContext _messageContext;
        private readonly CommandTree _commandTree;
        
        public HelpCommandGroup(CommandService commandService, IDiscordRestChannelAPI channelApi, MessageContext messageContext, CommandTree commandTree)
        {
            _commandService = commandService;
            _channelApi = channelApi;
            _messageContext = messageContext;
            _commandTree = commandTree;
        }

        [Command("help")]
        [Description("Shows help for the command provided.")]
        public async Task<Result> DisplayModulesAsync()
        {
            var children = _commandTree.Root.Children;
            List<GroupNode> groupNodes = new();
            foreach (var node in children)
            {
                if (node is GroupNode gp)
                {
                    groupNodes.Add(gp);
                }

                if (node is CommandNode cp)
                {
                    continue;
                }
            }

            var footer = new EmbedFooter("test");
            var embed = new Embed
            {
                Description = groupNodes.Select(x => x.Key).Humanize(),
                Title = "Help",
                Footer = footer
            };
            var result = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, embeds: new[] {embed}, ct: CancellationToken);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
        }
    }
}