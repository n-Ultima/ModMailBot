using Remora.Discord.API.Abstractions.Objects;

namespace Modmail.Common
{
    public static class UserExtensions
    {
        public static string Tag(this IUser user) => user.Username + "#" + user.Discriminator;
        
        public static string GetDefiniteAvatarUrl(this IUser user)
        {
            if (user.Avatar == default)
            {
                return $"https://cdn.discordapp.com/embed/avatars/{user.Discriminator % 5}.png";
            }

            if (user.Avatar.HasGif)
            {
                return $"https://cdn.discordapp.com/avatars/{user.ID.Value}/{user.Avatar.Value}.gif?size=512";
            }
            
            return $"https://cdn.discordapp.com/avatars/{user.ID.Value}/{user.Avatar.Value}.png?size=512";
        }
    }
}