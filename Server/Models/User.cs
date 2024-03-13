namespace Server.Models
{
    public class User: Entity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string PhotoLink { get; set; } = string.Empty;

        public DateOnly BirthDate { get; set; } = DateOnly.MinValue;

        public Sex Sex { get; set; }

        public string City { get; set; } = string.Empty;

        public List<string> Interests { get; set; } = [];

        public string Email { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; } = false;

        public string Phone { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;     

       
    }

    public enum Sex
    {
        Male,
        Female,
        Undefined
    }
}
