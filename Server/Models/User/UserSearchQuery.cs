using System.ComponentModel.DataAnnotations;

namespace Server.Models.User
{
    public class UserSearchQuery
    {
        /// <summary>
        /// Часть имени для поиска
        /// </summary>
        [StringLength(100, ErrorMessage = "Значение поля \"Имя пользователя\" не может быть больше {0} символов")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Часть фамилии для поиска
        /// </summary>
        [StringLength(100, ErrorMessage = "Значение поля \"Фамилия пользователя\" не может быть больше {0} символов")]
        public string LastName { get; set; } = string.Empty;

    }
}