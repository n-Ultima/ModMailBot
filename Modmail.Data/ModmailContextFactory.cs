using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Modmail.Common;

namespace Modmail.Data
{
    public class ModmailContextFactory : IDesignTimeDbContextFactory<ModmailContext>
    {
        public ModmailConfiguration ModmailConfig = new();
        public ModmailContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder()
                .UseNpgsql(ModmailConfig.DbConnectionString);
            return new ModmailContext(options.Options);
        }
    }
}