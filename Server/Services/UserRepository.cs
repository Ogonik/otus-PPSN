using Microsoft.Identity.Client;
using Npgsql;
using Server.Models.User;
using System.Diagnostics;
using BC = BCrypt.Net.BCrypt;

namespace Server.Services
{
    public class UserRepository(NpgsqlConnection connection, ILogger<UserRepository> logger) : IUserRepository, IDisposable
    {
        public async Task<int> CreateUser(User user, Guid userGuid)
        {
            logger.LogInformation("UserRepository: creating user...");
            if (userGuid == Guid.Empty)
                throw new Exception("User guid can't be empty");

            var createUserQuery = "INSERT INTO public.\"user\"" +
                " (id, first_name, last_name, photo_link, birth_date, sex, city, interests, email, email_confirmed, phone, \"password\")" +
                " VALUES(@id, @first_name, @last_name, @photo_link, @birth_date, @sex, @city, @interests, @email, @email_confirmed , @phone, @password);";

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

            logger.LogInformation("UserRepository: insert command initilizated");

            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("UserRepository: user created");

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
            // To be added context disposion here
            logger.LogInformation("disposing");
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            var users = new List<User>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ppsn.user";
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    users.Add(_userFromRow(reader));
                }
            }
            await connection.CloseAsync();
            return users;
        }

        public async Task<int> DeleteAllUsers()
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ppsn.user;";
            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();

            await connection.CloseAsync();
            return result;
        }

        public async Task<User> GetUserById(Guid userGuid)
        {
            User? user = null;

            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM public.\"user\" u WHERE u.id = '{userGuid}'";
            //cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, userGuid);
           
            await connection.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var userCount = 0;
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    userCount++;
                    user = _userFromRow(reader);
                    Debug.Assert(userCount == 1);
                }
            }
            await connection.CloseAsync();

            return user;
        }

        public Task<IEnumerable<User>> SearchUser(string? FirstName, string? LastName, string birthDate)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateUser(User user)
        {
            throw new NotImplementedException();
        }

        private User _userFromRow(NpgsqlDataReader reader)
        {
            var user = new User();
            user.Id = Guid.Parse(Convert.ToString(reader["id"] ?? string.Empty));
            user.FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty; 
            user.LastName = Convert.ToString(reader["last_name"]) ?? string.Empty;
            user.PhotoLink = Convert.ToString(reader["photo_link"]) ?? string.Empty;
            var birthDateTime = Convert.ToDateTime(reader["birth_date"]);
            user.BirthDate = new DateOnly(birthDateTime.Year, birthDateTime.Month, birthDateTime.Day);
            user.Sex = (Sex)Convert.ToInt16(reader["sex"]);
            user.City = Convert.ToString(reader["city"]) ?? string.Empty;
            user.Interests = Convert.ToString(reader["interests"] ?? string.Empty).Split(new char[] { ',' }).ToList();
            user.Email = Convert.ToString(reader["email"]) ?? string.Empty;
            user.EmailConfirmed = Convert.ToBoolean(reader["email_confirmed"] ?? false);
            user.Phone = Convert.ToString(reader["phone"]) ?? string.Empty;
            user.Password = Convert.ToString(reader["password"]) ?? string.Empty;

            return user;
        }

    }
}
