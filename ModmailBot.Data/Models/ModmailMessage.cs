using System;
using System.ComponentModel.DataAnnotations.Schema;
using Remora.Discord.Core;

namespace ModmailBot.Data.Models
{
    public class ModmailMessage
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        public Snowflake MessageId { get; set; }
        
        public Guid ModmailTicketId { get; set; }
        
        public Snowflake AuthorId { get; set; }
        
        public string Content { get; set; }
    }
}