namespace ระบบจองคิวโรงพยาบาล.ViewModels
{
    public class ScheduleViewModel
    {
        public int UserId { get; set; }          // ผู้ดูแลตารางที่ล็อกอินอยู่
        public int ScheduleId { get; set; }
        public int DoctorId { get; set; }
        public DateOnly? ScheduleDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public int MaxQueue { get; set; } = 10;
        public string? Status { get; set; }
    }
}