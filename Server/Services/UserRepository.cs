using BC = BCrypt.Net.BCrypt;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Server.Models.User;

namespace Server.Services
{
    public class UserRepository(NpgsqlConnection connection) : IUserRepository, IDisposable
    {
        public async Task<int> CreateUser(User user, Guid userGuid)
        {
            if (userGuid == Guid.Empty)
                throw new Exception("User guid can't be empty");

            var createUserQuery = "INSERT INTO public.\"user\"" +
                " (id, first_name, last_name, photo_link, birth_date, sex, city, interests, email, email_confirmed, phone, \"password\")" +
                " VALUES(@id, '@first_name', '@last_name', '@photo_link', '@birth_date', @sex, '@city', '@interests', '@email', @email_confirmed , '@phone', '@password');";
            
            using var cmd = connection.CreateCommand();
            cmd.CommandText = createUserQuery;
            
            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, userGuid);
            cmd.Parameters.AddWithValue("@first_name", NpgsqlTypes.NpgsqlDbType.Varchar, user.FirstName);
            cmd.Parameters.AddWithValue("@last_name", NpgsqlTypes.NpgsqlDbType.Varchar, user.LastName);
            cmd.Parameters.AddWithValue("@photo_link", NpgsqlTypes.NpgsqlDbType.Varchar, user.PhotoLink);
            cmd.Parameters.AddWithValue("@birth_date", NpgsqlTypes.NpgsqlDbType.Date, user.BirthDate);
            cmd.Parameters.AddWithValue("@sex", NpgsqlTypes.NpgsqlDbType.Integer, (int)user.Sex);
            cmd.Parameters.AddWithValue("@city", NpgsqlTypes.NpgsqlDbType.Varchar, user.City);
            cmd.Parameters.AddWithValue("@interests", NpgsqlTypes.NpgsqlDbType.Varchar, string.Join(',', user.Interests));
            cmd.Parameters.AddWithValue("@email", NpgsqlTypes.NpgsqlDbType.Varchar, user.Email);
            cmd.Parameters.AddWithValue("@email_confirmed", NpgsqlTypes.NpgsqlDbType.Boolean, user.EmailConfirmed);
            cmd.Parameters.AddWithValue("@phone", NpgsqlTypes.NpgsqlDbType.Varchar, user.Phone);
            cmd.Parameters.AddWithValue("@password", NpgsqlTypes.NpgsqlDbType.Varchar, BC.HashPassword(user.Password));

            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();

            await connection.CloseAsync();

            return result;
        }

        public Task<bool> DeleteUser(User user)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUser(Guid id)
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
                    users.Add(new User()
                    {
                        Id = Guid.Parse(Convert.ToString(reader["id"] ?? string.Empty)),
                        FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty,
                        LastName = Convert.ToString(reader["last_name"]) ?? string.Empty,
                        PhotoLink = Convert.ToString(reader["photo_link"]) ?? string.Empty,
                        BirthDate = new DateOnly()
                    });
                }
            }
            await connection.CloseAsync();
            return users.ToList();
        }

        public Task<User> GetUserById(Guid id)
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
