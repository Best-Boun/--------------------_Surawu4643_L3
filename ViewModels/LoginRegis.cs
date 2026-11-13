using System.ComponentModel.DataAnnotations;

namespace ระบบจองคิวโรงพยาบาล.ViewModels
{
    public class LoginRegisViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        public string FirstName { get; set; } = "";

        public string LastName { get; set; } = "";

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? RoleName { get; set; }
    }
}