using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class FinanceController : Controller
{
    private readonly Csi402dbContext _db;
    public FinanceController(Csi402dbContext db)
    {
        _db = db;
    }

    // เช็คว่า UserId นี้เป็นเจ้าหน้าที่การเงินจริงไหม
    private bool IsFinance(int UserId)
    {
        var user = (from u in _db.Users
                    where u.UserId == UserId
                    select u).FirstOrDefault();
        if (user == null)
        {
            return false;
        }

        var role = (from r in _db.Roles
                    where r.RoleId == user.RoleId
                    select r).FirstOrDefault();
        return role != null && role.RoleName == "FinanceStaff";
    }

    // เลขใบเสร็จ เช่น RC20261115-0001 (receipt_no ต้อง unique)
    private string NextReceiptNo()
    {
        string prefix = "RC" + DateTime.Now.ToString("yyyyMMdd") + "-";
        int n = (from r in _db.Receipts
                 where r.ReceiptNo.StartsWith(prefix)
                 select r).Count() + 1;

        string no = prefix + n.ToString("0000");
        while ((from r in _db.Receipts where r.ReceiptNo == no select r).Count() > 0)
        {
            n++;
            no = prefix + n.ToString("0000");
        }
        return no;
    }

    // ออกใบเสร็จ (ยังไม่ SaveChanges ให้ผู้เรียกบันทึกพร้อมกัน)
    private void IssueReceipt(Payment pay, Appointment ap, int UserId)
    {
        Receipt rc = new Receipt();
        rc.Payment = pay;
        rc.ReceiptNo = NextReceiptNo();
        rc.IssuedDate = DateTime.Now;
        rc.TotalAmount = ap.TotalPrice ?? 0;
        rc.DiscountAmount = ap.DiscountAmount ?? 0;
        rc.NetAmount = ap.NetPrice ?? 0;
        rc.IssuedBy = UserId;
        _db.Add(rc);
    }

    // หน้าหลัก: สรุปตัวเลข + รายการชำระเงิน (กรองตามสถานะ)
    public IActionResult Index(int UserId, string? Status)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        if (string.IsNullOrEmpty(Status))
        {
            Status = "PENDING";
        }

        var payments = (from p in _db.Payments
                        where Status == "ALL" || p.PaymentStatus == Status
                        orderby p.PaymentId descending
                        select p).ToList();

        // ตัวเลขสรุป
        DateTime todayStart = DateTime.Today;
        var paidToday = (from p in _db.Payments
                         where p.PaymentStatus == "PAID" && p.PaymentDate >= todayStart
                         select p).ToList();

        ViewBag.UserId = UserId;
        ViewBag.FilterStatus = Status;
        ViewBag.PaymentList = payments;
        ViewBag.AppointmentList = (from a in _db.Appointments select a).ToList();
        ViewBag.PatientList = (from p in _db.Patients select p).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.PendingCount = (from p in _db.Payments
                                where p.PaymentStatus == "PENDING"
                                select p).Count();
        ViewBag.PaidTodayCount = paidToday.Count;
        ViewBag.PaidTodayTotal = paidToday.Sum(x => x.Amount);
        return View();
    }

    // รายละเอียดการชำระเงิน + ตรวจสอบยอด
    public IActionResult Detail(int UserId, int PaymentId)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var pay = (from p in _db.Payments
                   where p.PaymentId == PaymentId
                   select p).FirstOrDefault();
        if (pay == null)
        {
            TempData["Error"] = "ไม่พบรายการชำระเงิน";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == pay.AppointmentId
                  select a).FirstOrDefault();
        var details = (from d in _db.PaymentDetails
                       where d.PaymentId == PaymentId
                       select d).ToList();

        // ตรวจสอบยอดเงิน
        decimal expected = ap != null ? (ap.NetPrice ?? 0) : 0;
        decimal detailSum = details.Sum(x => x.Amount);

        ViewBag.UserId = UserId;
        ViewBag.Payment = pay;
        ViewBag.Appointment = ap;
        ViewBag.Details = details;
        ViewBag.Expected = expected;
        ViewBag.DetailSum = detailSum;
        ViewBag.AmountOk = (pay.Amount == expected && detailSum == expected);
        ViewBag.ReceiptInfo = (from r in _db.Receipts
                               where r.PaymentId == PaymentId
                               select r).FirstOrDefault();
        ViewBag.PatientUser = (from pt in _db.Patients
                               join u in _db.Users on pt.UserId equals u.UserId
                               where pt.PatientId == pay.PatientId
                               select u).FirstOrDefault();
        return View();
    }

    // ยืนยันการชำระเงิน -> PAID + ออกใบเสร็จ
    [HttpPost]
    public IActionResult Verify(int UserId, int PaymentId)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var pay = (from p in _db.Payments
                   where p.PaymentId == PaymentId
                   select p).FirstOrDefault();
        if (pay == null || pay.PaymentStatus != "PENDING")
        {
            TempData["Error"] = "รายการนี้ไม่อยู่ในสถานะรอตรวจสอบ";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == pay.AppointmentId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมายของรายการนี้";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        if (pay.Amount != (ap.NetPrice ?? 0))
        {
            TempData["Error"] = "ยอดที่ชำระไม่ตรงกับยอดสุทธิของนัดหมาย กรุณาตรวจสอบหรือปฏิเสธรายการ";
            return RedirectToAction("Detail", new { UserId = UserId, PaymentId = PaymentId });
        }

        pay.PaymentStatus = "PAID";
        pay.VerifiedBy = UserId;
        IssueReceipt(pay, ap, UserId);

        _db.SaveChanges();

        TempData["Success"] = "ยืนยันการชำระเงินและออกใบเสร็จเรียบร้อยแล้ว";
        return RedirectToAction("Detail", new { UserId = UserId, PaymentId = PaymentId });
    }

    // ปฏิเสธ (เช่น สลิปไม่ถูกต้อง) -> ผู้ป่วยจ่ายใหม่ได้
    [HttpPost]
    public IActionResult Reject(int UserId, int PaymentId)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var pay = (from p in _db.Payments
                   where p.PaymentId == PaymentId
                   select p).FirstOrDefault();
        if (pay == null || pay.PaymentStatus != "PENDING")
        {
            TempData["Error"] = "รายการนี้ไม่อยู่ในสถานะรอตรวจสอบ";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        pay.PaymentStatus = "REJECTED";
        pay.VerifiedBy = UserId;
        _db.SaveChanges();

        TempData["Success"] = "ปฏิเสธรายการชำระเงินแล้ว ผู้ป่วยสามารถชำระใหม่ได้";
        return RedirectToAction("Index", new { UserId = UserId });
    }

    // รับชำระที่เคาน์เตอร์: แสดงนัดที่ยังไม่มีรายการชำระเงิน
    public IActionResult Counter(int UserId)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var usedIds = (from p in _db.Payments
                       where p.PaymentStatus == "PENDING" || p.PaymentStatus == "PAID"
                       select p.AppointmentId).ToList();

        var aps = (from a in _db.Appointments
                   where a.Status != "CANCELLED" && !usedIds.Contains(a.AppointmentId)
                   orderby a.AppointmentDate, a.AppointmentTime
                   select a).ToList();

        ViewBag.UserId = UserId;
        ViewBag.AppointmentList = aps;
        ViewBag.PatientList = (from p in _db.Patients select p).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        return View();
    }

    // บันทึกรับชำระที่เคาน์เตอร์ -> PAID ทันที + ออกใบเสร็จ
    [HttpPost]
    public IActionResult ReceiveCash(int UserId, int AppointmentId, string PaymentMethod)
    {
        if (!IsFinance(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId
                  select a).FirstOrDefault();
        if (ap == null || ap.Status == "CANCELLED")
        {
            TempData["Error"] = "ไม่พบนัดหมายหรือนัดหมายถูกยกเลิกแล้ว";
            return RedirectToAction("Counter", new { UserId = UserId });
        }

        var existing = (from p in _db.Payments
                        where p.AppointmentId == AppointmentId
                           && (p.PaymentStatus == "PENDING" || p.PaymentStatus == "PAID")
                        select p).FirstOrDefault();
        if (existing != null)
        {
            TempData["Error"] = "นัดนี้มีรายการชำระเงินแล้ว";
            return RedirectToAction("Counter", new { UserId = UserId });
        }

        if (string.IsNullOrWhiteSpace(PaymentMethod))
        {
            TempData["Error"] = "กรุณาเลือกวิธีชำระเงิน";
            return RedirectToAction("Counter", new { UserId = UserId });
        }

        Payment pay = new Payment();
        pay.AppointmentId = ap.AppointmentId;
        pay.PatientId = ap.PatientId;
        pay.Amount = ap.NetPrice ?? 0;
        pay.PaymentMethod = PaymentMethod;
        pay.PaymentStatus = "PAID";
        pay.PaymentDate = DateTime.Now;
        pay.VerifiedBy = UserId;
        _db.Add(pay);

        // รายละเอียด: บริการ (+) และส่วนลด (-)
        var lines = (from l in _db.AppointmentServices
                     where l.AppointmentId == AppointmentId
                     select l).ToList();
        var services = (from s in _db.Services select s).ToList();
        foreach (var l in lines)
        {
            var sv = services.FirstOrDefault(x => x.ServiceId == l.ServiceId);
            PaymentDetail d = new PaymentDetail();
            d.Payment = pay;
            d.Description = sv != null ? sv.ServiceName : "บริการ";
            d.Amount = l.Subtotal;
            d.DetailType = "SERVICE";
            _db.Add(d);
        }

        var promoLines = (from l in _db.AppointmentPromotions
                          where l.AppointmentId == AppointmentId
                          select l).ToList();
        var promos = (from p in _db.Promotions select p).ToList();
        foreach (var l in promoLines)
        {
            var pm = promos.FirstOrDefault(y => y.PromotionId == l.PromotionId);
            PaymentDetail d = new PaymentDetail();
            d.Payment = pay;
            d.Description = "ส่วนลด: " + (pm != null ? pm.PromotionName : "โปรโมชั่น");
            d.Amount = -(l.DiscountAmount ?? 0);
            d.DetailType = "DISCOUNT";
            _db.Add(d);
        }

        IssueReceipt(pay, ap, UserId);

        _db.SaveChanges();

        TempData["Success"] = "รับชำระเงินและออกใบเสร็จเรียบร้อยแล้ว (คิว " + ap.QueueNo + ")";
        return RedirectToAction("Counter", new { UserId = UserId });
    }
}