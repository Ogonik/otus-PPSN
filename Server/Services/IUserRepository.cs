using Microsoft.AspNetCore.Mvc;
using Server.Models.User;

namespace Server.Services
{
    public interface IUserRepository
    {
        public Task<IEnumerable<User>> GetAllUsers();

        public Task<User> GetUserById(Guid id);

        public Task<IEnumerable<User>> SearchUser(string FirstName, string LastName, bool useTS);

        public Task<int> CreateUser(User user, Guid userGuid);

        public Task<int> CreateUserBatch(List<User> users, int seed);
        
        public Task<bool> UpdateUser(User user);

        public Task<bool> DeleteUser(User user, bool hardDelete = false);

        public Task<int> DeleteAllUsers(Guid preserveUserGuid);
    }
}
