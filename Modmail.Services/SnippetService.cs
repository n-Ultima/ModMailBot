using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modmail.Data;
using Modmail.Data.Models;
using Remora.Discord.Core;

namespace Modmail.Services
{
    public class SnippetService : ModmailService
    {
        public SnippetService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        public async Task CreateSnippetAsync(string name, string content)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                modmailContext.ModmailSnippets.Add(new ModmailSnippet
                {
                    Name = name,
                    Content = content
                });
            }
        }

        public async Task<ModmailSnippet> FetchSnippetAsync(string snippetName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.ModmailSnippets
                    .Where(x => x.Name.Equals(snippetName, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefaultAsync();
            }
        }
    }
}