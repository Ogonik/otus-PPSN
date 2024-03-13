using Server.Models;

namespace Server.Services
{
    public interface IUserRepository
    {
        public Task<IEnumerable<User>> GetAllUsers();

        public Task<User> GetUserById(int id);

        public Task<IEnumerable<User>> SearchUser(string? FirstName, string? LastName, string birthDate);

        public Task<bool> CreateUser(User user);

        public Task<bool> UpdateUser(User user);

        public Task<bool> DeleteUser(User user);

        public Task<bool> DeleteUser(int id);
    }
}
