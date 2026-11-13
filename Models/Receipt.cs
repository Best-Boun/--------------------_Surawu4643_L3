using System;
using System.Collections.Generic;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

public partial class Receipt
{
    public int ReceiptId { get; set; }

    public int PaymentId { get; set; }

    public string ReceiptNo { get; set; } = null!;

    public DateTime? IssuedDate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal NetAmount { get; set; }

    public int? IssuedBy { get; set; }

    public virtual User? IssuedByNavigation { get; set; }

    public virtual Payment Payment { get; set; } = null!;
}
