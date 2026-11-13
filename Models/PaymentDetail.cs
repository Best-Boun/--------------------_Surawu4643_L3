using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class PaymentDetail
{
    public int PaymentDetailId { get; set; }

    public int PaymentId { get; set; }

    public string Description { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? DetailType { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
