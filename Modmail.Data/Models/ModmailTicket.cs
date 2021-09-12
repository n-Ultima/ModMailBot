using System;
using Remora.Discord.Core;

namespace Modmail.Data.Models
{
    public class ModmailTicket
    {
        public Guid Id { get; set; }
        
        public Snowflake DmChannelId { get; set; }
        
        public Snowflake ModmailThreadChannelId { get; set; }
        
        /// <summary>
        /// <remarks>Always the owner of the DM channel.</remarks>
        /// </summary>
        public Snowflake UserId { get; set; }
        
    }
}