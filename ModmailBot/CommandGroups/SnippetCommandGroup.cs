using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using ModmailBot.Common;
using ModmailBot.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Core;
using Remora.Results;

namespace ModmailBot.CommandGroups
{
    public class SnippetCommand : CommandGroup
    {
        private readonly MessageContext _messageContext;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly SnippetService _snippetService;
        private readonly ModmailTicketService _modmailTicketService;
        public ModmailConfiguration ModmailConfig = new();
        public SnippetCommand(MessageContext messageContext, IDiscordRestGuildAPI guildApi, IDiscordRestChannelAPI channelApi, SnippetService snippetService, ModmailTicketService modmailTicketService)
        {
            _messageContext = messageContext;
            _guildApi = guildApi;
            _channelApi = channelApi;
            _snippetService = snippetService;
            _modmailTicketService = modmailTicketService;
        }

        [Command("snippet")]
        [Description("Sends the snippet to the DM Channel, or previews it if not in a modmail channel.")]
        public async Task<IResult> DisplaySnippetAsync(string snippetName)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault(); var cmdGroup = new SnippetCommandGroup(_channelApi, _guildApi, _snippetService, _modmailTicketService, _messageContext);
            if (!await cmdGroup.TryAuthenticateUser(executor.Entity, guild.Entity, everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
            {
                return Result.FromError(new ExceptionError(new Exception("The snippet provided was not found.")));
            }
            var fullMessage = await _channelApi.GetChannelMessageAsync(_messageContext.ChannelID, _messageContext.MessageID);
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
                Colour = Color.LimeGreen,
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
    }
    [Group("snippet")]
    public class SnippetCommandGroup : CommandGroup
    {
        private readonly IDiscordRestChannelAPI _channelApi;
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly SnippetService _snippetService;
        private readonly MessageContext _messageContext;
        private readonly ModmailTicketService _modmailTicketService;
        public ModmailConfiguration ModmailConfig = new();
        
        public SnippetCommandGroup(IDiscordRestChannelAPI channelApi, IDiscordRestGuildAPI guildApi, SnippetService snippetService, ModmailTicketService modmailTicketService, MessageContext messageContext)
        {
            _channelApi = channelApi;
            _guildApi = guildApi;
            _snippetService = snippetService;
            _modmailTicketService = modmailTicketService;
            _messageContext = messageContext;
        }

        [Command("preview")]
        [Description("Previews a snippets content.")]
        public async Task<IResult> PreviewSnippetAsync(string snippetName)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Moderator))
            {
                return Result.FromSuccess();
            }
            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
            {
                throw new Exception("The snippet provided was not found.");
            }

            var embed = new Embed
            {
                Colour = Color.Red,
                Title = $"Snippet {snippet.Name}'s Content:",
                Description = $"```\n{snippet.Content}\n```"
            };
            var result = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, embeds: new[] {embed}, ct: CancellationToken);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
        }
        [Command("create", "add")]
        [Description("Creates a snippet for later use.")]
        public async Task<IResult> CreateSnippetAsync(string snippetName, [Greedy] string snippetContent)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet != null)
            {
                 throw new Exception("A snippet with that name already exists.");
            }

            await _snippetService.CreateSnippetAsync(snippetName, snippetContent);
            var successResult = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Snippet successfully created.");
            return successResult.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(successResult.Error);
        }

        [Command("edit", "modify")]
        [Description("Modifies an existing snippets content.")]
        public async Task<IResult> EditSnippetAsync(string snippetName, [Greedy] string newContent)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
            {
                throw new Exception("The snippet provided was not found.");
            }

            await _snippetService.EditSnippetAsync(snippet, newContent);
            var result = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Snippet successfully modified.", ct: CancellationToken);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
        }

        [Command("delete", "remove")]
        [Description("Deletes a snippet.")]
        public async Task<IResult> DeleteSnippetAsync(string snippetName)
        {
            var executor = await _guildApi.GetGuildMemberAsync(_messageContext.GuildID.Value, _messageContext.User.ID, CancellationToken);
            var guild = await _guildApi.GetGuildAsync(_messageContext.GuildID.Value, ct: CancellationToken);
            
            var guildRoles = await _guildApi.GetGuildRolesAsync(new Snowflake(ModmailConfig.InboxServerId), CancellationToken);
            var everyoneRole = guildRoles.Entity
                .Where(x => x.ID == guild.Entity.ID)
                .FirstOrDefault();
            if (!await TryAuthenticateUser(executor.Entity, guild.Entity,everyoneRole, PermissionLevel.Administrator))
            {
                return Result.FromSuccess();
            }

            var snippet = await _snippetService.FetchSnippetAsync(snippetName);
            if (snippet == null)
            {
                throw new Exception("The snippet provided was not found.");
            }

            await _snippetService.DeleteSnippetAsync(snippet);
            var result = await _channelApi.CreateMessageAsync(_messageContext.ChannelID, "Snippet successfully deleted.", ct: CancellationToken);
            return result.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(result.Error);
        }
        
        public async Task<bool> TryAuthenticateUser(IGuildMember member, IGuild guild, IRole everyoneRole, PermissionLevel permissionLevel)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var guildRoles = await _guildApi.GetGuildRolesAsync(guild.ID, CancellationToken);
            var memberRoles = guildRoles.Entity
                .Where(x => member.Roles.Contains(x.ID))
                .ToList();
            var permissions = DiscordPermissionSet.ComputePermissions(member.User.Value.ID, everyoneRole, memberRoles);
            if (permissionLevel == PermissionLevel.Moderator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.ModRoleId)) || member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)) || permissions.HasPermission(DiscordPermission.Administrator))
                {
                    return true;
                }

                return false;
            }

            if (permissionLevel == PermissionLevel.Administrator)
            {
                if (member.Roles.Contains(new Snowflake(ModmailConfig.AdminRoleId)) || permissions.HasPermission(DiscordPermission.Administrator))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}