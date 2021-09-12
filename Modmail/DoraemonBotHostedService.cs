using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Modmail;

namespace Doraemon
{
    public class DoraemonBotHostedService : IHostedService
    {
        private readonly ModmailBot _botClient;
        private Task? _runTask;

        public DoraemonBotHostedService(ModmailBot botClient)
            => _botClient = botClient;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runTask = _botClient.RunAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _runTask ?? Task.CompletedTask;
        }
    }
}