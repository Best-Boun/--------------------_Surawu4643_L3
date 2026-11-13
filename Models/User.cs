using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Address { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? Gender { get; set; }

    public virtual ICollection<AppointmentStatusLog> AppointmentStatusLogs { get; set; } = new List<AppointmentStatusLog>();

    public virtual Doctor? Doctor { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual Role Role { get; set; } = null!;
}
