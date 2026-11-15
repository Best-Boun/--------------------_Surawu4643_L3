using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class PaymentController : Controller
{
    private readonly Csi402dbContext _db;
    public PaymentController(Csi402dbContext db)
    {
        _db = db;
    }

    private Patient? GetPatient(int UserId)
    {
        return (from p in _db.Patients
                where p.UserId == UserId
                select p).FirstOrDefault();
    }

    // หน้าสรุปค่าใช้จ่าย + เลือกวิธีชำระ
    [HttpGet]
    public IActionResult Pay(int UserId, int AppointmentId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.PatientId == patient.PatientId
                  select a).FirstOrDefault();
        if (ap == null || ap.Status == "CANCELLED")
        {
            TempData["Error"] = "ไม่พบนัดหมายหรือนัดหมายถูกยกเลิกแล้ว";
            return RedirectToAction("MyAppointments", "Appointment", new { UserId = UserId });
        }

        var existing = (from p in _db.Payments
                        where p.AppointmentId == AppointmentId
                           && (p.PaymentStatus == "PENDING" || p.PaymentStatus == "PAID")
                        select p).FirstOrDefault();
        if (existing != null)
        {
            TempData["Error"] = "นัดนี้มีรายการชำระเงินแล้ว";
            return RedirectToAction("History", new { UserId = UserId });
        }

        ViewBag.UserId = UserId;
        ViewBag.Lines = (from l in _db.AppointmentServices
                         where l.AppointmentId == AppointmentId
                         select l).ToList();
        ViewBag.ServiceList = (from s in _db.Services select s).ToList();
        ViewBag.PromoLines = (from l in _db.AppointmentPromotions
                              where l.AppointmentId == AppointmentId
                              select l).ToList();
        ViewBag.PromoList = (from p in _db.Promotions select p).ToList();
        return View(ap);
    }

    // บันทึกการชำระเงิน (สถานะ PENDING รอเจ้าหน้าที่การเงินตรวจสอบ)
    [HttpPost]
    public IActionResult Pay(int UserId, int AppointmentId, string PaymentMethod, string? ReferenceNo)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.PatientId == patient.PatientId
                  select a).FirstOrDefault();
        if (ap == null || ap.Status == "CANCELLED")
        {
            TempData["Error"] = "ไม่พบนัดหมายหรือนัดหมายถูกยกเลิกแล้ว";
            return RedirectToAction("MyAppointments", "Appointment", new { UserId = UserId });
        }

        var existing = (from p in _db.Payments
                        where p.AppointmentId == AppointmentId
                           && (p.PaymentStatus == "PENDING" || p.PaymentStatus == "PAID")
                        select p).FirstOrDefault();
        if (existing != null)
        {
            TempData["Error"] = "นัดนี้มีรายการชำระเงินแล้ว";
            return RedirectToAction("History", new { UserId = UserId });
        }

        if (string.IsNullOrWhiteSpace(PaymentMethod))
        {
            TempData["Error"] = "กรุณาเลือกวิธีชำระเงิน";
            return RedirectToAction("Pay", new { UserId = UserId, AppointmentId = AppointmentId });
        }

        Payment pay = new Payment();
        pay.AppointmentId = ap.AppointmentId;
        pay.PatientId = patient.PatientId;
        pay.Amount = ap.NetPrice ?? 0;
        pay.PaymentMethod = PaymentMethod;
        pay.PaymentStatus = "PENDING";
        pay.PaymentDate = DateTime.Now;
        pay.ReferenceNo = ReferenceNo;
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

        _db.SaveChanges();

        TempData["Success"] = "บันทึกการชำระเงินแล้ว รอเจ้าหน้าที่การเงินตรวจสอบ";
        return RedirectToAction("History", new { UserId = UserId });
    }

    public IActionResult History(int UserId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var payments = (from p in _db.Payments
                        where p.PatientId == patient.PatientId
                        orderby p.PaymentId descending
                        select p).ToList();

        var payIds = payments.Select(x => x.PaymentId).ToList();

        ViewBag.UserId = UserId;
        ViewBag.PaymentList = payments;
        ViewBag.AppointmentList = (from a in _db.Appointments
                                   where a.PatientId == patient.PatientId
                                   select a).ToList();
        ViewBag.ReceiptList = (from r in _db.Receipts
                               where payIds.Contains(r.PaymentId)
                               select r).ToList();
        return View();
    }

    public IActionResult Receipt(int UserId, int PaymentId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var pay = (from p in _db.Payments
                   where p.PaymentId == PaymentId && p.PatientId == patient.PatientId
                   select p).FirstOrDefault();
        if (pay == null)
        {
            TempData["Error"] = "ไม่พบรายการชำระเงิน";
            return RedirectToAction("History", new { UserId = UserId });
        }

        ViewBag.UserId = UserId;
        ViewBag.Payment = pay;
        ViewBag.ReceiptInfo = (from r in _db.Receipts
                               where r.PaymentId == PaymentId
                               select r).FirstOrDefault();
        ViewBag.Appointment = (from a in _db.Appointments
                               where a.AppointmentId == pay.AppointmentId
                               select a).FirstOrDefault();
        ViewBag.Details = (from d in _db.PaymentDetails
                           where d.PaymentId == PaymentId
                           select d).ToList();
        ViewBag.PatientUser = (from u in _db.Users
                               where u.UserId == UserId
                               select u).FirstOrDefault();
        return View();
    }
}