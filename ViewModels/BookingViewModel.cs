using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยาบาล.ViewModels
{
    public class BookingViewModel
    {
        public int UserId { get; set; }
        public int ScheduleId { get; set; }
        public int PersonCount { get; set; } = 1;
        public List<int> ServiceIds { get; set; } = new List<int>();

        // ผลคำนวณ
        public decimal Total { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetPrice { get; set; }
        public List<Service> Services { get; set; } = new List<Service>();
        public List<Promotion> UsedPromotions { get; set; } = new List<Promotion>();
    }
}