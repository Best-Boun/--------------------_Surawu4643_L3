using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = null!;

    public string PromotionType { get; set; } = null!;

    public decimal DiscountPercent { get; set; }

    public int? MinimumAge { get; set; }

    public int? MinimumPerson { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<AppointmentPromotion> AppointmentPromotions { get; set; } = new List<AppointmentPromotion>();
}
