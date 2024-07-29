using System.ComponentModel.DataAnnotations;

namespace Server.Models.User
{
    public class User : Entity // TODO: при переезде на EF поменять на IdentityUser 
    {
        public User()
        { }

        public User(UserRegisterQuery userRegisterQuery)
        {
            FirstName = userRegisterQuery.FirstName;
            LastName = userRegisterQuery.SecondName;
            BirthDate = userRegisterQuery.BirthDate;
            City = userRegisterQuery.City;
            Password = userRegisterQuery.Password;
            Interests = new List<string> { userRegisterQuery.Biography };
            Email = userRegisterQuery.Email;
        }

        [StringLength(256, ErrorMessage = "Значение поля \"Имя пользователя\" не может быть больше {0} символов")]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(256, ErrorMessage = "Значение поля \"Фамилия пользователя\" не может быть больше {0} символов")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(128, ErrorMessage = "Значение поля \"PhotoLink\" не может быть больше {0} символов")]
        public string PhotoLink { get; set; } = string.Empty;

        public DateOnly BirthDate { get; set; } = DateOnly.MinValue;

        public Sex Sex { get; set; } = Sex.Undefined;

        public string City { get; set; } = string.Empty;

        public List<string> Interests { get; set; } = [];

        public string Email { get; set; } = string.Empty;

        public bool EmailConfirmed { get; set; } = false;

        public string Phone { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }

    public enum Sex
    {
        Male = 1,
        Female = 2,
        Undefined = 0


    }
}
