using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModmailBot.Data;
using ModmailBot.Data.Models;
using Remora.Discord.Core;

namespace ModmailBot.Services
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
                await modmailContext.SaveChangesAsync();
            }
        }

        public async Task EditSnippetAsync(ModmailSnippet snippet, string newContent)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                snippet.Content = newContent;
                await modmailContext.SaveChangesAsync();
            }
        }

        public async Task DeleteSnippetAsync(ModmailSnippet snippet)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                modmailContext.ModmailSnippets.Remove(snippet);
                await modmailContext.SaveChangesAsync();
            }
        }
        
        public async Task<ModmailSnippet> FetchSnippetAsync(string snippetName)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.ModmailSnippets
                    .Where(x => x.Name == snippetName)
                    .FirstOrDefaultAsync();
            }
        }
    }
}