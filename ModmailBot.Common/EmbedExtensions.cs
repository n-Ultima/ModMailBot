using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace ModmailBot.Common
{
    public static class EmbedExtensions
    {
        #nullable enable
        public static Optional<IEmbedAuthor> WithUserAsAuthor(this IUser? user, string? extra = null)
        {
            if (user == null)
            {
                return new Optional<IEmbedAuthor>();
            }

            var suffix = string.Empty;

            if (!string.IsNullOrWhiteSpace(extra))
            {
                suffix = $" ({extra})";
            }

            return new EmbedAuthor(user.Tag() + suffix, IconUrl: user.GetDefiniteAvatarUrl());
        }
        #nullable disable
    }
}