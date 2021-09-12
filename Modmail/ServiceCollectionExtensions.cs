using System;
using System.Linq;
using Doraemon.CommandGroups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modmail.Data;
using Modmail.Services;
using Remora.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;

namespace Modmail
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandGroups(this IServiceCollection collection)
        {
            collection
                .AddCommandGroup<ConnectivityCommandGroup>()
                .AddCommandGroup<HelpCommandGroup>()
                .AddCommandGroup<ModmailCommandGroup>();
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
                .AddSingleton<ModmailTicketService>();
            return collection;
        }
    }
}