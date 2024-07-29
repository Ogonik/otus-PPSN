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
                " (id, first_name, last_name, photo_link, birth_date, sex, city, interests, email, email_confirmed, phone, \"password\", created_at, updated_at, is_removed)" +
                " VALUES(@id, @first_name, @last_name, @photo_link, @birth_date, @sex, @city, @interests, @email, @email_confirmed , @phone, @password, @created_at, @updated_at, @is_removed);";

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
            cmd.Parameters.AddWithValue("@created_at", NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@updated_at", NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@is_removed", NpgsqlTypes.NpgsqlDbType.Boolean, false);

            logger.LogInformation("UserRepository: insert command initilizated");

            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("UserRepository: user created");

            await connection.CloseAsync();

            return result;
        }

        public async Task<bool> DeleteUser(User user, bool hardDelete = false)
        {
            logger.LogInformation("Deleting user with uuid: {guid}", user.Id);

            using var cmd = connection.CreateCommand();
            cmd.CommandText = hardDelete ?
                $"UPDATE public.\"user\" u SET is_removed = true WHERE u.id = '{user.Id}' " :
                $"DELETE FROM public.\"user\" u WHERE u.id = '{user.Id}' ";
            await connection.OpenAsync();

            var usersDeleted = await cmd.ExecuteNonQueryAsync();
            Debug.Assert(usersDeleted == 1);

            await connection.CloseAsync();
            logger.LogInformation("UserRepository: user {deleted}", hardDelete ? "deleted":"marked removed");
            return usersDeleted == 1;
        }

        public void Dispose()
        {
            // To be added context disposion here
            logger.LogInformation("disposing...");
        }

        public async Task<IEnumerable<User>> GetAllUsers()
        {
            var users = new List<User>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM ppsn.user WHERE is_removed = false";
            await connection.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    users.Add(UserFromRow(reader));
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
            cmd.CommandText = $"SELECT * FROM public.\"user\" u WHERE u.id = '{userGuid}' AND u.is_removed = false";
            //cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, userGuid);

            await connection.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var userCount = 0;
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    userCount++;
                    user = UserFromRow(reader);
                    Debug.Assert(userCount == 1);
                }
            }
            await connection.CloseAsync();

            return user;
        }

        public async Task<IEnumerable<User>> SearchUser(string FirstName, string LastName)
        {
            List<User> usersFound = new List<User>();
            var whereClause = string.Empty;
            // сформируем условие на where имея в виду что оба параметра не могут быть пустыми - отрезали на валидации

            using var cmd = connection.CreateCommand();

            if (FirstName != string.Empty && LastName != string.Empty)
            {
                whereClause = $" WHERE u.last_name LIKE @last_name AND u.first_name LIKE @first_name AND is_removed = false";
                cmd.Parameters.AddWithValue("@last_name", NpgsqlTypes.NpgsqlDbType.Varchar, LastName);
                cmd.Parameters.AddWithValue("@first_name", NpgsqlTypes.NpgsqlDbType.Varchar, FirstName);
            }
            else if (FirstName == string.Empty)
            {
                whereClause = $" WHERE u.last_name LIKE @last_name";
                cmd.Parameters.AddWithValue("@last_name", NpgsqlTypes.NpgsqlDbType.Varchar, LastName);
            }
            else if (LastName == string.Empty)
            {
                whereClause = $" WHERE u.first_name = @first_name";
                cmd.Parameters.AddWithValue("@first_name", NpgsqlTypes.NpgsqlDbType.Varchar, FirstName);
            }

            cmd.CommandText = $"SELECT * FROM public.\"user\" u " + whereClause;
            logger.LogInformation("Final search query is {query}", cmd.CommandText);

            await connection.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            if (reader is not null)
            {
                while (await reader.ReadAsync())
                {
                    usersFound.Add(UserFromRow(reader));
                }
            }
            await connection.CloseAsync();

            return usersFound;
        }

        public Task<bool> UpdateUser(User user)
        {
            throw new NotImplementedException();
        }

        public static User UserFromRow(NpgsqlDataReader reader)
        {
            var user = new User
            {
                Id = Guid.Parse(Convert.ToString(reader["id"] ?? string.Empty)),
                FirstName = Convert.ToString(reader["first_name"]) ?? string.Empty,
                LastName = Convert.ToString(reader["last_name"]) ?? string.Empty,
                PhotoLink = Convert.ToString(reader["photo_link"]) ?? string.Empty,
                Sex = (Sex)Convert.ToInt16(reader["sex"]),
                City = Convert.ToString(reader["city"]) ?? string.Empty,
                Interests = Convert.ToString(reader["interests"] ?? string.Empty).Split(new char[] { ',' }).ToList(),
                Email = Convert.ToString(reader["email"]) ?? string.Empty,
                EmailConfirmed = Convert.ToBoolean(reader["email_confirmed"] ?? false),
                Phone = Convert.ToString(reader["phone"]) ?? string.Empty,
                Password = Convert.ToString(reader["password"]) ?? string.Empty,
                CreatedAt = Convert.ToDateTime(reader["created_at"]),
                UpdatedAt = Convert.ToDateTime(reader["updated_at"]),
                IsRemoved = Convert.ToBoolean(reader["is_removed"])
            };

            var birthDateTime = Convert.ToDateTime(reader["birth_date"]);
            user.BirthDate = new DateOnly(birthDateTime.Year, birthDateTime.Month, birthDateTime.Day);

            return user;
        }

    }
}
