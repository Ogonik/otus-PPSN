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


        public async Task<int> CreateUserBatch(List<User> users, int seed = 1000)
        {
            logger.LogInformation("Batch creating users...");
            var seedCount = 0;
            var totalUsersSaved = 0;
            await connection.OpenAsync();
            using (var writer = await connection.BeginBinaryImportAsync("copy public.\"user\" FROM STDIN (FORMAT BINARY)"))
            {
                while (seedCount < (users.Count / seed) + 1)
                {
                    logger.LogInformation("  Seed #{seedCount}", seedCount);

                    foreach (var user in users.Take(new Range(seedCount * seed, (seedCount + 1) * seed - 1)))
                    {
                        writer.StartRow();
                        writer.Write(user.Id, NpgsqlTypes.NpgsqlDbType.Uuid);
                        writer.Write(user.FirstName, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.LastName, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.PhotoLink, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.BirthDate, NpgsqlTypes.NpgsqlDbType.Date);
                        writer.Write((int)user.Sex);
                        writer.Write(user.City, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(string.Join(',', user.Interests), NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.Email, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.EmailConfirmed, NpgsqlTypes.NpgsqlDbType.Boolean);
                        writer.Write(user.Phone, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(user.Password, NpgsqlTypes.NpgsqlDbType.Varchar);
                        writer.Write(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(DateTime.UtcNow, NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(false, NpgsqlTypes.NpgsqlDbType.Boolean);
                        totalUsersSaved++;
                    }

                    seedCount++;
                }
                await writer.CompleteAsync();

                logger.LogInformation("Batch import finished {totalUsersSaved} users saved", totalUsersSaved);
                await connection.CloseAsync();
                return totalUsersSaved;
            }
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
            logger.LogInformation("UserRepository: user {deleted}", hardDelete ? "deleted" : "marked removed");
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

        public async Task<int> DeleteAllUsers(Guid preserveUserGuid)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"DELETE FROM public.\"user\" u WHERE u.Id != '{preserveUserGuid}'";
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

        public async Task<IEnumerable<User>> SearchUser(string firstName, string lastName, bool useTSQuery = false)
        {
            List<User> usersFound = new List<User>();
            var whereClause = string.Empty;
            // сформируем условие на where имея в виду что оба параметра не могут быть пустыми - отрезали на валидации

            using var cmd = connection.CreateCommand();
            if (!useTSQuery)
            {
                if (firstName != string.Empty && lastName != string.Empty)
                {
                    whereClause = $" WHERE u.last_name ILIKE '%{lastName}%' AND u.first_name ILIKE '%{firstName}%' AND is_removed = false";
                    cmd.Parameters.AddWithValue("@lastName", NpgsqlTypes.NpgsqlDbType.Varchar, lastName);
                    cmd.Parameters.AddWithValue("@firstName", NpgsqlTypes.NpgsqlDbType.Varchar, firstName);
                }
                else if (firstName == string.Empty)
                {
                    whereClause = $" WHERE u.last_name ILIKE '%@lastName%'";
                    cmd.Parameters.AddWithValue("@lastName", NpgsqlTypes.NpgsqlDbType.Varchar, lastName);
                }
                else if (lastName == string.Empty)
                {
                    whereClause = $" WHERE u.first_name ILIKE '%@firstName%'";
                    cmd.Parameters.AddWithValue("@firstName", NpgsqlTypes.NpgsqlDbType.Varchar, firstName);
                }
            }
            else
            {
                if (firstName != string.Empty && lastName != string.Empty)
                {
                    whereClause = $" WHERE to_tsvector('russian', u.last_name) @@ to_tsquery('russian', '{lastName}:*') AND to_tsvector('russian', u.first_name) @@ to_tsquery('russian', '{firstName}:*') ";
                   
                }
                else if (firstName == string.Empty)
                {
                    whereClause = $" WHERE u.last_name ILIKE '%@lastName%'";
                    cmd.Parameters.AddWithValue("@lastName", NpgsqlTypes.NpgsqlDbType.Varchar, lastName);
                }
                else if (lastName == string.Empty)
                {
                    whereClause = $" WHERE u.first_name ILIKE '%@firstName%'";
                    cmd.Parameters.AddWithValue("@firstName", NpgsqlTypes.NpgsqlDbType.Varchar, firstName);
                }
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
