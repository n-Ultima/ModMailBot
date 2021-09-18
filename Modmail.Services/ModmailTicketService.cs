using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modmail.Data;
using Modmail.Data.Models;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace Modmail.Services
{
    public class ModmailTicketService : ModmailService
    {
        public ModmailTicketService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public async Task AddMessageToModmailTicketAsync(Guid modmailThreadId, Snowflake messageId, Snowflake authorId, string messageContent)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                modmailContext.ModmailMessages.Add(new ModmailMessage
                {
                    MessageId = messageId,
                    AuthorId = authorId,
                    Content = messageContent,
                    ModmailTicketId = modmailThreadId
                });
                await modmailContext.SaveChangesAsync();
            }
        }

        public async Task CreateModmailTicketAsync(Guid id, Snowflake dmChannelId, Snowflake modmailChannelId, Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                if (await FetchModmailTicketAsync(userId) != null)
                    throw new Exception("Modmail ticket with that user already exists.");
                modmailContext.ModmailTickets.Add(new ModmailTicket
                {
                    Id = id,
                    DmChannelId = dmChannelId,
                    ModmailThreadChannelId = modmailChannelId,
                    UserId = userId
                });
                await modmailContext.SaveChangesAsync();
            }
        }
        public async Task<ModmailTicket> FetchModmailTicketAsync(Snowflake userId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.ModmailTickets
                    .Where(x => x.UserId == userId)
                    .SingleOrDefaultAsync();
            }
        }

        public async Task<IEnumerable<ModmailMessage>> FetchModmailMessagesAsync(Guid ticketId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.ModmailMessages
                    .Where(x => x.ModmailTicketId == ticketId)
                    .ToListAsync();
            }
        }
        public async Task<ModmailTicket> FetchModmailTicketByModmailTicketChannelAsync(Snowflake channelId)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                return await modmailContext.ModmailTickets
                    .Where(x => x.ModmailThreadChannelId == channelId)
                    .SingleOrDefaultAsync();
            }
        }

        public async Task DeleteModmailTicketAsync(ModmailTicket modmailTicket)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var modmailContext = scope.ServiceProvider.GetRequiredService<ModmailContext>();
                var messsages = await modmailContext.ModmailMessages
                    .Where(x => x.ModmailTicketId == modmailTicket.Id)
                    .ToListAsync();
                modmailContext.ModmailMessages. RemoveRange(messsages);
                modmailContext.ModmailTickets.Remove(modmailTicket);
                await modmailContext.SaveChangesAsync();
            }
        }
    }
}