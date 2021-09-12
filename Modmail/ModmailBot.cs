using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Results;
using Remora.Results;
using Serilog;

namespace Modmail
{
    public class ModmailBot
    {
        private readonly DiscordGatewayClient _discordGatewayClient;

        public ModmailBot(DiscordGatewayClient discordGatewayClient)
            => _discordGatewayClient = discordGatewayClient;
        public async Task RunAsync(CancellationToken cancellationToken)
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
    }
}