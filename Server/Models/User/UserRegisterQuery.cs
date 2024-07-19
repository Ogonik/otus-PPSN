using System.ComponentModel.DataAnnotations;

namespace Server.Models.User
{
    public class UserRegisterQuery
    {
        /// <summary>
        /// Имя
        /// </summary>
        [StringLength(100, ErrorMessage = "Значение поля \"Имя пользователя\" не может быть больше {0} символов")]
        [Required(ErrorMessage = "Имя пользователя не может быть пустым")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Фамилия
        /// </summary>
        [StringLength(100, ErrorMessage = "Значение поля \"Фамилия пользователя\" не может быть больше {0} символов")]
        [Required(ErrorMessage = "Фамилия пользователя не может быть пустой")]
        public string SecondName { get; set; } = string.Empty;

        /// <summary>
        /// Дата рождения YYYY-MM-DD
        /// </summary>
        [Required(ErrorMessage = "Дата рождения пользователя не может быть пустой")]
        public DateOnly BirthDate { get; set; } = DateOnly.MinValue;

        /// <summary>
        /// Хобби, интересы и т.п.
        /// </summary>
        [StringLength(1050, ErrorMessage = "Значение поля \"Хобби, интересы и т.п.\" не может быть больше {0} символов")]
        public string Biography { get; set; } = string.Empty;

        /// <summary>
        /// Город местонахождения
        /// </summary>
        [StringLength(50, ErrorMessage = "Значение поля \"Город\" не может быть больше {0} символов")]
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Электронная почта
        /// </summary>
        [EmailAddress(ErrorMessage = "Введите корректный адрес электронной почты")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Пароль
        /// </summary>
        /// 
        [StringLength(40, ErrorMessage = "Значение поля \"Пароль\" не может быть больше {0} символов")]
        [Required(ErrorMessage = "Пароль пользователя не может быть пустым")]
        public string Password { get; set; } = string.Empty;


        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            var minBirthDate = DateOnly.ParseExact("1900-01-01", "yyyy-MM-dd");
            var maxBirthDate = DateOnly.FromDateTime(DateTime.Now);
            if (BirthDate < minBirthDate && BirthDate > maxBirthDate)
            {
                errorMessage = $"Значение поля \"Дата рождения\" должно быть между {minBirthDate} and {maxBirthDate}";
                return false;
            }
            return true;
        }
    }
}