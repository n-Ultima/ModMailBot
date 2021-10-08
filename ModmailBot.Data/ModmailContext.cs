using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ModmailBot.Data.Models;
using Remora.Discord.Core;

namespace ModmailBot.Data
{
    public class ModmailContext : DbContext
    {
        public ModmailContext(DbContextOptions options) : base(options)
        {}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var snowflakeConverter = new ValueConverter<Snowflake, ulong>(static snowflake => snowflake.Value, static @ulong => new Snowflake(@ulong));
            modelBuilder.UseValueConverterForType<Snowflake>(snowflakeConverter);        
        }
        public DbSet<BlockedUser> BlockedUsers { get; set; }
        
        public DbSet<ModmailMessage> ModmailMessages { get; set; }
        
        public DbSet<ModmailSnippet> ModmailSnippets { get; set; }
        
        public DbSet<ModmailTicket> ModmailTickets { get; set; }
    }
}