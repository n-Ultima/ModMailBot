using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Results;
using Remora.Results;
using Serilog;

namespace Modmail
{
    public class ModmailBotHostedService : IHostedService
    {
        private readonly DiscordGatewayClient _discordGatewayClient;
        private readonly IHostApplicationLifetime _applicationLifetime;
        public ModmailBotHostedService(DiscordGatewayClient discordGatewayClient, IHostApplicationLifetime applicationLifetime)
        {
            _discordGatewayClient = discordGatewayClient;
            _applicationLifetime = applicationLifetime;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var runResult = await _discordGatewayClient.RunAsync(cancellationToken);
            if (!runResult.IsSuccess)
            {
                switch (runResult.Error)
                {
                    case ExceptionError exe:
                    {
                        Log.Logger.Error(exe.Exception,"Exception during gateway connection: {ExceptionMessage}", exe.Message);
                        break;
                    }
                    case GatewayWebSocketError:
                    case GatewayDiscordError:
                    {
                        Log.Logger.Error("Gateway error: {Message}", runResult.Error.Message);
                        break;
                    }
                    default:
                    {
                        Log.Logger.Error("Unknown error: {Message}", runResult.Error.Message);
                        break;
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.StopApplication();
            Log.Logger.Information("Stopped ModmailBot");
            return Task.CompletedTask;
        }
    }
}