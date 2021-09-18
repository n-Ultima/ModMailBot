using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;

namespace Modmail.Common
{
    public static class GuildExtensions
    {
        public static string GetAbsoluteIconUrl(this IGuild guild)
        {
            var guildIcon = CDN.GetGuildIconUrl(guild);
            if (!guildIcon.IsSuccess)
            {
                return null;
            }

            return guildIcon.Entity.AbsoluteUri;
        }    
    }
}