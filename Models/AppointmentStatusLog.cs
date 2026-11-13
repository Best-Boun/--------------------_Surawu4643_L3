using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class AppointmentStatusLog
{
    public int LogId { get; set; }

    public int AppointmentId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Note { get; set; }

    public int ChangedBy { get; set; }

    public DateTime? ChangedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual User ChangedByNavigation { get; set; } = null!;
}
