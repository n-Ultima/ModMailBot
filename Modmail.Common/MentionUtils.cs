using Remora.Discord.API.Abstractions.Objects;

namespace Modmail.Common
{
    public static class MentionUtils
    {
        public static string Mention(this IRole role) => $"<@&{role.ID}>";

        public static string Mention(this IUser user) => $"<@{user.ID}>";

        public static string Mention(this IChannel channel) => $"<@#{channel.ID}>";
    }
}