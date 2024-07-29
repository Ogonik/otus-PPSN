using Microsoft.Identity.Client;
using Npgsql;
using Server.Models.User;
using System.Diagnostics;
using BC = BCrypt.Net.BCrypt;

namespace Server.Services
{
    public class UserFriendsRepository(NpgsqlConnection connection, ILogger<UserRepository> logger, IUserRepository userRepository) : IUserFriendsRepository, IDisposable
    {
        public async Task<bool> AddFriend(Guid userGuid, Guid friendGuid)
        {
            logger.LogInformation("Adding friend for {source_user_id} as {dest_user_id}", userGuid, friendGuid);
            if (userGuid == Guid.Empty)
                throw new Exception("User guid can't be empty");
            if (friendGuid == Guid.Empty)
                throw new Exception("Friend guid can't be empty");

            if (userRepository.GetUserById(friendGuid) == null)
                throw new Exception("Friend not found by guid");


            var createFriendQuery = "INSERT INTO public.\"friend\"" +
                " (id, user_id, friend_id, created_at, updated_at, is_removed)" +
                " VALUES(@id, @user_id, @friend_id, @created_at, @updated_at, @is_removed);";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = createFriendQuery;

            cmd.Parameters.AddWithValue("@id", NpgsqlTypes.NpgsqlDbType.Uuid, Guid.NewGuid());
            cmd.Parameters.AddWithValue("@user_id", NpgsqlTypes.NpgsqlDbType.Varchar, userGuid);
            cmd.Parameters.AddWithValue("@friend_id", NpgsqlTypes.NpgsqlDbType.Varchar, friendGuid);
            cmd.Parameters.AddWithValue("@created_at", NpgsqlTypes.NpgsqlDbType.Timestamp, DateTime.UtcNow.ToString("U"));
            cmd.Parameters.AddWithValue("@updated_at", NpgsqlTypes.NpgsqlDbType.Timestamp, DateTime.UtcNow.ToString("U"));
            cmd.Parameters.AddWithValue("@is_removed", NpgsqlTypes.NpgsqlDbType.Boolean, false);

            logger.LogInformation("insert command initilizated");

            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("friend record created");

            await connection.CloseAsync();

            return result == 1;
        }

        public async Task<bool> DeleteFriend(Guid userGuid, Guid friendGuid, bool hardDelete = false)
        {
            logger.LogInformation("Soft deleting friend for {source_user_id} as {dest_user_id} with hard mode = {isHard}", userGuid, friendGuid, hardDelete);
            if (userGuid == Guid.Empty)
                throw new Exception("User guid can't be empty");
            if (friendGuid == Guid.Empty)
                throw new Exception("Friend guid can't be empty");

            if (userRepository.GetUserById(friendGuid) == null)
                throw new Exception("Friend not found by guid");


            var softDeleteFriendQuery = "UPDATE public.\"friend\" f SET f.is_removed = true WHERE f.user_id = @user_id AND f.freind_id = @friend_id";
            var hardDeleteFriendQuery = "DELETE FROM public.\"friend\" f WHERE f.user_id = @user_id AND f.freind_id = @friend_id";

            using var cmd = connection.CreateCommand();
            cmd.CommandText = hardDelete ? hardDeleteFriendQuery: softDeleteFriendQuery;

            cmd.Parameters.AddWithValue("@user_id", NpgsqlTypes.NpgsqlDbType.Varchar, userGuid);
            cmd.Parameters.AddWithValue("@friend_id", NpgsqlTypes.NpgsqlDbType.Varchar, friendGuid);
            if (!hardDelete)
                cmd.Parameters.AddWithValue("@updated_at", NpgsqlTypes.NpgsqlDbType.Timestamp, DateTime.UtcNow.ToString("U"));

            logger.LogInformation("delete command initilizated");

            await connection.OpenAsync();

            var result = await cmd.ExecuteNonQueryAsync();
            logger.LogInformation("friend record {deleted}", hardDelete? "deleted":"marked as deleted");

            await connection.CloseAsync();

            return result == 1;
        }

        public void Dispose()
        {
            //nop
        }

        public Task<IEnumerable<User>> GetFriends(Guid currentUserId)
        {
            throw new NotImplementedException();
        }
    }
}
