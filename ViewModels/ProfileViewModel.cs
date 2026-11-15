using System.ComponentModel.DataAnnotations;

namespace ระบบจองคิวโรงพยาบาล.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อ")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
        public string LastName { get; set; } = "";

        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }
    }
}