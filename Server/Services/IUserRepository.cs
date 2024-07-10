using Microsoft.AspNetCore.Mvc;
using Server.Models.User;

namespace Server.Services
{
    public interface IUserRepository
    {
        public Task<IEnumerable<User>> GetAllUsers();

        public Task<User> GetUserById(Guid id);

        public Task<IEnumerable<User>> SearchUser(string? FirstName, string? LastName, string birthDate);

        public Task<int> CreateUser(User user, Guid userGuid);

        public Task<bool> UpdateUser(User user);

        public Task<bool> DeleteUser(User user);

        public Task<bool> DeleteUser(Guid id);
    }
}
