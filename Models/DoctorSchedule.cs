using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class DoctorSchedule
{
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    public DateOnly ScheduleDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int MaxQueue { get; set; }

    public int AvailableQueue { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Doctor Doctor { get; set; } = null!;
}
