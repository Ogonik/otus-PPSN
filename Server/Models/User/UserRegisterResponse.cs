namespace Server.Models.User
{
    public class UserRegisterResponse
    {
        public string Id { get; set; } = string.Empty;
        public UserRegisterResponse(Guid userId)
        {
            Id = (userId == Guid.Empty) ? string.Empty : userId.ToString();
        }
    }
}