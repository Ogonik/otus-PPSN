
using System.ComponentModel.DataAnnotations;

namespace Server.Models.User
{
    public class UserGetResponse
    {
        public string FirstName { get; set; } = string.Empty;
        public string SecondName { get; set; } = string.Empty;
        public DateOnly BirthDate { get; set; } = DateOnly.MinValue;
        public string Biography { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public static explicit operator UserGetResponse(User user)
        {
            return new UserGetResponse()
            {
                FirstName = user.FirstName,
                SecondName = user.LastName,
                BirthDate = user.BirthDate,
                Biography = string.Join(',', user.Interests),
                City = user.City,
                Email = user.Email
            };
        }
    }
}