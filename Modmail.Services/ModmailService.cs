using System;

namespace Modmail.Services
{
    public class ModmailService : Attribute
    {
        internal protected IServiceProvider ServiceProvider;

        public ModmailService(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;
    }
}