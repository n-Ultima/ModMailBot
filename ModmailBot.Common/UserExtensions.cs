using System;
using System.Collections.Generic;
using System.Linq;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Core;

namespace ModmailBot.Common
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

        public static int GetHierarchy(this IGuildMember member, IGuild guild, IReadOnlyList<IRole> roles)
        {
            if (guild.OwnerID == member.User.Value.ID)
                return int.MaxValue;
            return roles.Count == 0 ? 0 : roles.Max<IRole>((Func<IRole, int>) (x => x.Position));
        }

        public static bool OutranksUser(this IGuildMember member, IGuildMember userToCompare, IGuild guild, IReadOnlyList<IRole> user1Roles, IReadOnlyList<IRole> user2Roles)
        {
            return member.GetHierarchy(guild, user1Roles) > userToCompare.GetHierarchy(guild, user2Roles);
        }
    }
}