using System;

namespace ModmailBot.Services
{
    public class ModmailService : Attribute
    {
        internal protected IServiceProvider ServiceProvider;

        public ModmailService(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;
    }
}