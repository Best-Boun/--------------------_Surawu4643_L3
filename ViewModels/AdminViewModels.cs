namespace ระบบจองคิวโรงพยาบาล.ViewModels
{
    public class UserFormViewModel
    {
        public int AdminId { get; set; }      // Admin ที่ล็อกอินอยู่
        public int UserId { get; set; }       // ผู้ใช้ที่กำลังแก้ไข (0 = เพิ่มใหม่)
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Password { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int RoleId { get; set; }
        public string Status { get; set; } = "ACTIVE";

        // เฉพาะ Role = Doctor
        public int? DepartmentId { get; set; }
        public string? LicenseNo { get; set; }
        public string? Specialization { get; set; }
        public string? Biography { get; set; }
    }

    public class PromotionFormViewModel
    {
        public int AdminId { get; set; }
        public int PromotionId { get; set; }
        public string PromotionName { get; set; } = "";
        public string PromotionType { get; set; } = "";
        public decimal DiscountPercent { get; set; }
        public int? MinimumAge { get; set; }
        public int? MinimumPerson { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }

    public class DepartmentFormViewModel
    {
        public int AdminId { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public string? Description { get; set; }
    }

    public class ServiceFormViewModel
    {
        public int AdminId { get; set; }
        public int ServiceId { get; set; }
        public int DepartmentId { get; set; }
        public string ServiceName { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string ServiceType { get; set; } = "CONSULT";
        public string Status { get; set; } = "ACTIVE";
    }
}