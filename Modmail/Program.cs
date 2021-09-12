﻿using System;
using System.IO;
using System.Threading.Tasks;
using Doraemon;
using Doraemon.CommandGroups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Modmail.Common;
using Modmail.Data;
using Serilog;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Serilog.Events;

namespace Modmail
{
    internal sealed class Program
    {
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
                        .Configure<CommandResponderOptions>(x => x.Prefix = prefix)
                        .Configure<DiscordGatewayClientOptions>(x =>
                        {
                            x.Intents |= GatewayIntents.Guilds;
                            x.Intents |= GatewayIntents.DirectMessages;
                            x.Intents |= GatewayIntents.GuildBans;
                            x.Intents |= GatewayIntents.GuildMessages;
                            x.Intents |= GatewayIntents.GuildMembers;
                        })
                        .AddHostedService<DoraemonBotHostedService>()
                        .AddDbContext<ModmailContext>(x => x.UseNpgsql(context.Configuration["DbConnectionString"]))
                        .AddTransient<ModmailBot>();
                })
                .UseSerilog(Log.Logger)
                .UseConsoleLifetime();
            using (var host = hostBuilder.Build())
            {
                await host.RunAsync();
            }
        }
    }
}