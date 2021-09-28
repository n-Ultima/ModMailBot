using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Modmail.Common;
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
using Remora.Discord.Commands.Extensions;
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
        public ModmailConfiguration ModmailConfig = new();
        public HelpCommandGroup(CommandService commandService, IDiscordRestChannelAPI channelApi, MessageContext messageContext, CommandTree commandTree)
        {
            _commandService = commandService;
            _channelApi = channelApi;
            _messageContext = messageContext;
            _commandTree = commandTree;
        }

        [Command("help")]
        [Description("Shows help for the command provided.")]
        public async Task<IResult> DisplayHelpInfoAsync([Greedy] string name = null)
        {
            if (name == null)
            {
                var defaultEmbed = new Embed
                {
                    Title = "Help",
                    Footer = new EmbedFooter($"Use {ModmailConfig.Prefix}help <commandName> to display help for a specific command.\n[] is an indicator of aliases. So for respond, you can also use r, or reply."),
                    Description = $"Commands: ```\nping, help, respond[r, reply], edit, move, close[end], snippet, snippet preview, snippet create[snippet add], snippet edit[snippet modify], snippet remove[snippet delete], unblock, block\n```",
                    Colour = Color.Aqua,
                };
                var defaultResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, embeds: new[] {defaultEmbed}, ct: CancellationToken);
                return defaultResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(defaultResult.Error);
            }

            // Due to Remora.Discord's command system, there's a few things:
            // One: Groups aren't registered as part of the command name
            // Two: Reflection
            // Because of this, I have to resort to this. It isn't terrible until I have tons of commands.
            var loweredInput = name.ToLower();
            string cmd;
            switch (loweredInput)
            {
                case "ping":
                    cmd = "**Command Name:** ping\n**Command Description:** Pings for latency between Discord and the bot.";
                    break;
                case "help":
                    cmd = "**Command Name:** help\n**Command Description:** Replies with this embed.";
                    break;
                case "respond":
                    cmd = "**Command Name:** respond, r, reply\n**Command Parameters:** `<content>` `[attachments]`\n**Command Description:** Responds to the modmail thread.";
                    break;
                case "reply":
                    cmd = "**Command Name:** respond, r, reply\n**Command Parameters:** `<content>` `[attachments]`\n**Command Description:** Responds to the modmail thread.";
                    break;
                case "r":
                    cmd = "**Command Name:** respond, r, reply\n**Command Parameters:** `<content>` `[attachments]`\n**Command Description:** Responds to the modmail thread.";
                    break;
                case "close":
                    cmd = "**Command Name:** close, end\n**Command Description:** Closes the modmail thread. If the user sends another message, a new thread will be created.";
                    break;
                case "end":
                    cmd = "**Command Name:** close, end\n**Command Description:** Closes the modmail thread. If the user sends another message, a new thread will be created.";
                    break;
                case "edit":
                    cmd = "**Command Name:** edit\n**Command Parameters:** `<messageId>` `<newContent>`\n**Command Description:** Edits a message sent, should be used by Staff members as message edits from DM's are logged automatically.";
                    break;
                case "snippet":
                    cmd = "**Command Name:** snippet\n**Command Parameters:** `<snippetName>`\n**Command Description:** Sends the snippet provided to the corresponding DM channel of the guild. If ran in a non-modmail channel, sends the snippet content as a preview.";
                    break;
                case "move":
                    cmd = "**Command Name:** move\n**Command Parameters:** `<categoryId>`\n**Command Description:** Moves the modmail ticket over to another category.";
                    break;
                case "snippet preview":
                    cmd = "**Command Name:** snippet preview\n**Command Parameters:** `<snippetName>`\n**Command Description:** Previews a snippet even if ran in a modmail channel.";
                    break;
                case "snippet create":
                    cmd = "**Command Name:** snippet create, snippet add\n**Command Parameters:** `<snippetName>` `<snippetContent>`\n**Command Description:** Creates a snippet with the provided name and content.";
                    break;
                case "snippet add":
                    cmd = "**Command Name:** snippet create, snippet add\n**Command Parameters:** `<snippetName>` `<snippetContent>`\n**Command Description:** Creates a snippet with the provided name and content.";
                    break;
                case "snippet edit":
                    cmd = "**Command Name:** snippet edit, snippet modify\n**Command Parameters:** `<snippetName>` `<newContent>`\n**Command Description:** Edits an existing snippet's content.";
                    break;
                case "snippet modify":
                    cmd = "**Command Name:** snippet edit, snippet modify\n**Command Parameters:** `<snippetName>` `<newContent>`\n**Command Description:** Edits an existing snippet's content.";
                    break;
                case "snippet remove":
                    cmd = "**Command Name:** snippet remove, snippet delete\n**Command Parameters:** `<snippetName>`\n**Command Description:** Deletes the snippet provided.";
                    break;
                case "snippet delete":
                    cmd = "**Command Name:** snippet remove, snippet delete\n**Command Parameters:** `<snippetName>`\n**Command Description:** Deletes the snippet provided.";
                    break;
                case "block":
                    cmd = "**Command Name:** block\n**Command Parameters:** `<userMention | userId>` `[reason]`\n**Command Description:** Blocks a user from interacting with the bot.";
                    break;
                case "unblock":
                    cmd = "**Command Name:** unblock\n**Command Parameters:** `<userMention | userId>` `[reason]`\n**Command Description:** Unblocks a user from interacting with the bot.";
                    break;
                default:
                    return Result.FromError(new ExceptionError(new Exception("Command not found.")));
            }

            var embed = new Embed
            {
                Title = "Help",
                Description = cmd,
                Footer = new EmbedFooter($"Use {ModmailConfig.Prefix}help <command> to view help on a specific command.\n[] means the parameter is optional."),
                Colour = Color.Aqua
            };
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, embeds: new[] {embed}, ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }
    }
}