using Npgsql;
using Server.Models;

namespace Server.Services
{
    public class UserRepository(NpgsqlConnection connection) : IUserRepository, IDisposable
    {
        public Task<bool> CreateUser(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(int id)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            var users = new List<User>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ppsn.user;";
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    users.Add(new User() {
                                Id = Convert.ToInt32(reader["id"]),
                                FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty,
                                LastName = Convert.ToString(reader["last_name"]) ?? string.Empty,
                                PhotoLink = Convert.ToString(reader["photo_link"]) ?? string.Empty,
                                BirthDate = new DateOnly(DataTokensMetadata) 
                                HireDate: Convert.ToDateTime(reader["hire_date"])
                            });
                }
            }
            await connection.CloseAsync();
            return users.ToList();
        }

        public Task<User> GetUserById(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> SearchUser(string? FirstName, string? LastName, string birthDate)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUser(User user)
        {
            throw new NotImplementedException();
        }
    }
}
