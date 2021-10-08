using System;
using System.Linq;
using Doraemon.CommandGroups;
using Microsoft.EntityFrameworkCore;
using ModmailBot.CommandGroups;
using Microsoft.Extensions.DependencyInjection;
using ModmailBot.Data;
using ModmailBot.Services;
using Remora.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;

namespace ModmailBot
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandGroups(this IServiceCollection collection)
        {
            collection
                .AddCommandGroup<ConnectivityCommandGroup>()
                .AddCommandGroup<HelpCommandGroup>()
                .AddCommandGroup<ModmailCommandGroup>()
                .AddCommandGroup<SnippetCommandGroup>()
                .AddCommandGroup<UserCommandGroup>()
                .AddCommandGroup<SnippetCommand>();
            return collection;
        }

        public static IServiceCollection AddReponders(this IServiceCollection collection)
        {
            var responderTypes = typeof(ModmailService).Assembly
                .GetExportedTypes()
                .Where(t => t.IsResponder());

            foreach (var responderType in responderTypes)
            {
                collection.AddResponder(responderType);
            }

            return collection;
        }

        public static IServiceCollection AddModmailServices(this IServiceCollection collection)
        {
            collection
                .AddSingleton<UserService>()
                .AddSingleton<ModmailTicketService>()
                .AddSingleton<SnippetService>();
            return collection;
        }
    }
}