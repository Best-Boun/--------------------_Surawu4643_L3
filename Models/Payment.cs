using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public int? VerifiedBy { get; set; }

    public string? ReferenceNo { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();

    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    public virtual User? VerifiedByNavigation { get; set; }
}
