using Microsoft.AspNetCore.Mvc;
using Server.Models.User;

namespace Server.Services
{
    public interface IUserFriendsRepository
    {
        public Task<IEnumerable<User>> GetFriends(Guid currentUserId);

        public Task<bool> AddFriend(Guid userGuid, Guid friendGuid);

        public Task<bool> DeleteFriend(Guid userGuid, Guid friendGuid, bool hardDelete = false);

    }
}
