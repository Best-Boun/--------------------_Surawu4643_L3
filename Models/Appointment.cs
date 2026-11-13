using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public string QueueNo { get; set; } = null!;

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int ScheduleId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public string? BookingChannel { get; set; }

    public string? Status { get; set; }

    public decimal? TotalPrice { get; set; }

    public decimal? DiscountAmount { get; set; }

    public decimal? NetPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AppointmentPromotion> AppointmentPromotions { get; set; } = new List<AppointmentPromotion>();

    public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();

    public virtual ICollection<AppointmentStatusLog> AppointmentStatusLogs { get; set; } = new List<AppointmentStatusLog>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual DoctorSchedule Schedule { get; set; } = null!;
}
