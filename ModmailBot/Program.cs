using System;
using System.IO;
using System.Threading.Tasks;
using Doraemon;
using Doraemon.CommandGroups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModmailBot.Common;
using ModmailBot.Data;
using Serilog;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using ModmailBot.CommandGroups;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Serilog.Events;

namespace ModmailBot
{
    internal sealed class Program
    {
        public static ModmailConfiguration ModmailConfig = new();
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Filter.ByExcluding(x => x.Level == LogEventLevel.Verbose)
                .WriteTo.Console()
                .CreateLogger();
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(x =>
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile("config.json")
                        .Build();
                    x.AddConfiguration(configuration);
                })
                .ConfigureServices((context, services) =>
                {
                    var prefix = context.Configuration["Prefix"];
                    var token = context.Configuration["Token"];
                    services
                        .AddDiscordGateway(_ => token)
                        .AddDiscordCommands()
                        .AddCommands()
                        .AddCommandGroups()
                        .AddReponders()
                        .AddModmailServices()
                        .AddDiscordCaching()
                        .AddPostExecutionEvent<PostExecutionEventHandler>()
                        .AddPreExecutionEvent<PreExecutionEventHandler>()
                        .Configure<CommandResponderOptions>(x => x.Prefix = prefix)
                        .Configure<DiscordGatewayClientOptions>(x =>
                        {
                            x.Intents |= GatewayIntents.Guilds;
                            x.Intents |= GatewayIntents.DirectMessages;
                            x.Intents |= GatewayIntents.GuildBans;
                            x.Intents |= GatewayIntents.GuildMessages;
                            x.Intents |= GatewayIntents.GuildMembers;
                        })
                        .AddHostedService<ModmailBotHostedService>()
                        .AddDbContext<ModmailContext>(x => x.UseNpgsql(context.Configuration["DbConnectionString"]));
                })
                .UseSerilog(Log.Logger)
                .UseConsoleLifetime();
            using (var host = hostBuilder.Build())
            {
                using (var db = host.Services.CreateScope().ServiceProvider.GetRequiredService<ModmailContext>())
                {
                    Log.Logger.Information("Migrating...");
                    await db.Database.MigrateAsync();
                    Log.Logger.Information("Migrated!");
                }
                Log.Logger.Information(ModmailConfig.ConfirmThreadCreation.ToString());
                await host.RunAsync();
            }
        }
    }
}