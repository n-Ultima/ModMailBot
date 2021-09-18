using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modmail.Common;
using Modmail.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace Doraemon.CommandGroups
{
    [Group("snippet")]
    public class SnippetCommandGroup : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly SnippetService _snippetService;
        private readonly UserService _userService;
        private readonly MessageContext _messageContext;
        private readonly ModmailTicketService _modmailTicketService;
        public ModmailConfiguration ModmailConfig = new();
        
        public SnippetCommandGroup(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, SnippetService snippetService, UserService userService, ModmailTicketService modmailTicketService, MessageContext messageContext)
        {
            _channelApi = channelApi;
            _guildApi = guildApi;
            _snippetService = snippetService;
            _userService = userService;
            _modmailTicketService = modmailTicketService;
            _messageContext = messageContext;
        }

        [Command("")]
        [Description("Sends the snippet to the DM Channel, or previews it if not in a modmail channel.")]
        public async Task<Result> DisplaySnippetAsync(string snippetName)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID);
            if (!TryAuthenticateUser(executor.Entity, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
            {
                return Result.FromError(new ExceptionError(new Exception("The snippet provided was not found.")));
            }
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value);
            var modmailTicket = await _modmailTicketService.FetchModmailTicketByModmailTicketChannelAsync(_messageContext.ChannelID);
            if (modmailTicket == null)
            {
                var snippetEmbed = new Embed
                {
                    Title = $"Snippet {snippet.Name}",
                    Colour = Color.Gold,
                    Description = $"```\n{snippet.Content}\n```",
                    Footer = new EmbedFooter("Use the `snippet preview <snippetName>` to preview snippets even in modmail channels.")
                };
                var displayResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Snippet content (not sent):", embeds: new[] {snippetEmbed}, ct: CancellationToken);
                return displayResult.IsSuccess
                    ? Result.FromSuccess()
                    : Result.FromError(displayResult.Error);
            }

            string highestRoleName;
            var guildRoles = await _guildApi.GetGuildRolesAsync(_messageContext.GuildID.Value, CancellationToken);
            var memberHighestRole = guildRoles.Entity
                .Where(x => executor.Entity.Roles.Contains(x.ID))
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
            var embed = new Embed
            {
                Colour = Color.Green,
                Author = _messageContext.User.WithUserAsAuthor(),
                Description = snippet.Content,
                Timestamp = DateTimeOffset.UtcNow,
                Footer = new EmbedFooter(highestRoleName),
            };
            var dmResult = await _channelApi.CreateMessageAsync(modmailTicket.DmChannelId, embeds: new[] {embed}, ct: CancellationToken);
            if (!dmResult.IsSuccess)
            {
                return Result.FromError(dmResult.Error);
            }
            await _modmailTicketService.AddMessageToModmailTicketAsync(modmailTicket.Id, _messageContext.MessageID, _messageContext.User.ID, $"(Snippet {snippet.Name}): {snippet.Content}");
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Here's what was sent to the user:", embeds: new[] {embed}, ct: CancellationToken);
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("create", "add")]
        [Description("Creates a snippet for later use.")]
        public async Task<Result> CreateSnippetAsync(string snippetName, [Greedy] string snippetContent)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID);
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value);
            if (!TryAuthenticateUser(executor.Entity, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet != null)
            {
                return Result.FromError(new ExceptionError(new Exception("A snippet with this name already exists.")));
            }

            await _snippetService.CreateSnippetAsync(snippetName, snippetContent);
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Snippet successfully created.");
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("edit", "modify")]
        [Description("Modifies an existing snippets content.")]
        public async Task<Result> EditSnippetAsync(string snippetName, [Greedy] string newContent)
        {
            
        }
        
        private bool TryAuthenticateUser(IGuildMember member, PermissionLevel permissionLevel)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (permissionLevel == null)
            {
                throw new ArgumentNullException(nameof(permissionLevel));
            }
            if (permissionLevel == PermissionLevel.Moderator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.ModRoleId)) || member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)))
                {
                    return true;
                }

                return false;
            }

            if (permissionLevel == PermissionLevel.Administrator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}