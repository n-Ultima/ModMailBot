using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.Extensions.Hosting;
using Modmail.Common;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Discord.Rest.API;
using Remora.Results;
using Serilog;

namespace Modmail.Services.Responders
{
    public class ClientReadyResponder : IResponder<IReady>
    {
        private readonly IDiscordRestGuildAPI _guildApi;
        private readonly IDiscordRestUserAPI _userApi;
        private readonly IHostApplicationLifetime _applicationLifetime;
        public ModmailConfiguration ModmailConfig = new();
        
        public ClientReadyResponder(IDiscordRestGuildAPI guildApi, IDiscordRestUserAPI userApi, IHostApplicationLifetime hostApplicationLifetime)
        {
            _guildApi = guildApi;
            _userApi = userApi;
            _applicationLifetime = hostApplicationLifetime;
        }

        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = new CancellationToken())
        {
            var guildsFound = gatewayEvent.Guilds;
            int expectedNumOfGuilds;
            var inboxGuildId = new Snowflake(ModmailConfig.InboxServerId);
            var mainGuildId = new Snowflake(ModmailConfig.MainServerId);
            if (inboxGuildId == mainGuildId)
            {
                expectedNumOfGuilds = 1;
            }
            else
            {
                expectedNumOfGuilds = 2;
            }
            
            if (guildsFound.Count != expectedNumOfGuilds)
            {
                // Why is the bot in an incorrect number of guilds? That's not right.
                var authGuilds = guildsFound
                    .Where(x => x.GuildID == inboxGuildId || x.GuildID == mainGuildId)
                    .ToList();
                var unauthorizedGuilds = guildsFound
                    .Except(authGuilds);
                Log.Logger.Error("Found {unauthGuilds} guilds, expected {correctNumOfGuilds}", unauthorizedGuilds.Count(), expectedNumOfGuilds);
                Log.Logger.Error("Guild(s) Info: {guildIds}", unauthorizedGuilds.Select(x => x.GuildID).Humanize());
                Log.Logger.Error("Shutting down due to incorrect number of guilds.");
                _applicationLifetime.StopApplication();
            }

            var inboxGuild = await _guildApi.GetGuildAsync(inboxGuildId, ct: ct);
            if (!inboxGuild.IsDefined())
            {
                Log.Logger.Error("The inboxGuildId provided is not valid.");
                _applicationLifetime.StopApplication();
            }

            var mainGuild = await _guildApi.GetGuildAsync(mainGuildId, ct: ct);
            if (!inboxGuild.IsDefined())
            {
                Log.Logger.Error("The mainServerId provided is not valid.");
                _applicationLifetime.StopApplication();
            }
            Log.Logger.Information("Successfully started with the following configuration:\nOwnerIds: {ownerIds}\nMainGuildName: {mainGuildName}\nInboxGuildName: {inboxGuildName}", ModmailConfig.OwnerIds.Humanize(), mainGuild.Entity.Name, inboxGuild.Entity.Name);
            return Result.FromSuccess();
        }
    }
}