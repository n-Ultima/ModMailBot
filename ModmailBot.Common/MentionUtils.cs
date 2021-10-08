using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace ModmailBot.Common
{
    public static class MentionUtils
    {
        public static string Mention(this IRole role) => $"<@&{role.ID}>";

        public static string Mention(this IUser user) => $"<@{user.ID}>";

        public static string Mention(this IChannel channel) => $"<@#{channel.ID}>";
    }
}