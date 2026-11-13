using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class AppointmentPromotion
{
    public int AppointmentPromotionId { get; set; }

    public int AppointmentId { get; set; }

    public int PromotionId { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? DiscountAmount { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Promotion Promotion { get; set; } = null!;
}
