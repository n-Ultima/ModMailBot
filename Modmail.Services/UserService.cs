using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modmail.Data;
using Remora.Discord.Core;

namespace Modmail.Services
{
    public class UserService : ModmailService
    {
        public UserService(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {}

        public async Task<IEnumerable<Snowflake>> FetchBlacklistedUsersAsync()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.BlockedUsers
                    .Select(x => x.Id)
                    .ToListAsync();
            }
        }
    }
}