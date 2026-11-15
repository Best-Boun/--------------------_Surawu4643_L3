using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class AppointmentController : Controller
{
    private readonly Csi402dbContext _db;
    public AppointmentController(Csi402dbContext db)
    {
        _db = db;
    }

    private Patient? GetPatient(int UserId)
    {
        return (from p in _db.Patients
                where p.UserId == UserId
                select p).FirstOrDefault();
    }

    // คืนค่าข้อความผิดพลาด ถ้าว่างแปลว่าทำรายการได้
    private string CheckCanChange(Appointment ap)
    {
        if (ap.Status != "WAITING")
        {
            return "ไม่สามารถทำรายการได้ เพราะสถานะนัดหมายคือ " + ap.Status;
        }

        DateTime appointmentTime = ap.AppointmentDate.ToDateTime(ap.AppointmentTime);
        if ((appointmentTime - DateTime.Now).TotalHours < 2)
        {
            return "ต้องยกเลิก/เลื่อนนัดล่วงหน้าอย่างน้อย 2 ชั่วโมง";
        }
        return "";
    }

    private string NextQueueNo(DoctorSchedule schedule)
    {
        int n = (from a in _db.Appointments
                 where a.ScheduleId == schedule.ScheduleId
                 select a).Count() + 1;

        string qn = "S" + schedule.ScheduleId + "-" + schedule.ScheduleDate.ToString("yyyyMMdd") + "-" + n.ToString("000");

        while ((from a in _db.Appointments where a.QueueNo == qn select a).Count() > 0)
        {
            n++;
            qn = "S" + schedule.ScheduleId + "-" + schedule.ScheduleDate.ToString("yyyyMMdd") + "-" + n.ToString("000");
        }
        return qn;
    }

    public IActionResult MyAppointments(int UserId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var list = (from a in _db.Appointments
                    where a.PatientId == patient.PatientId
                    orderby a.AppointmentDate descending, a.AppointmentTime descending
                    select a).ToList();

        ViewBag.UserId = UserId;
        ViewBag.DoctorList = (from d in _db.Doctors select d).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.PaymentList = (from p in _db.Payments
                               where p.PatientId == patient.PatientId
                               select p).ToList();
        return View(list);
    }

    [HttpPost]
    public IActionResult Cancel(int UserId, int AppointmentId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.PatientId == patient.PatientId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมาย";
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        string msg = CheckCanChange(ap);

        var paid = (from p in _db.Payments
                    where p.AppointmentId == ap.AppointmentId && p.PaymentStatus == "PAID"
                    select p).FirstOrDefault();
        if (msg == "" && paid != null)
        {
            msg = "นัดนี้ชำระเงินแล้ว กรุณาติดต่อเจ้าหน้าที่การเงินเพื่อดำเนินการยกเลิก";
        }

        if (msg != "")
        {
            TempData["Error"] = msg;
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        var schedule = (from s in _db.DoctorSchedules
                        where s.ScheduleId == ap.ScheduleId
                        select s).FirstOrDefault();
        if (schedule != null && schedule.AvailableQueue < schedule.MaxQueue)
        {
            schedule.AvailableQueue = schedule.AvailableQueue + 1;   // คืนคิว
        }

        var pendings = (from p in _db.Payments
                        where p.AppointmentId == ap.AppointmentId && p.PaymentStatus == "PENDING"
                        select p).ToList();
        foreach (var p in pendings)
        {
            p.PaymentStatus = "CANCELLED";
        }

        AppointmentStatusLog log = new AppointmentStatusLog();
        log.AppointmentId = ap.AppointmentId;
        log.OldStatus = ap.Status;
        log.NewStatus = "CANCELLED";
        log.Note = "ผู้ป่วยยกเลิกนัด";
        log.ChangedBy = UserId;
        log.ChangedAt = DateTime.Now;
        _db.Add(log);

        ap.Status = "CANCELLED";
        _db.SaveChanges();

        TempData["Success"] = "ยกเลิกนัดหมาย " + ap.QueueNo + " เรียบร้อยแล้ว";
        return RedirectToAction("MyAppointments", new { UserId = UserId });
    }

    [HttpGet]
    public IActionResult Reschedule(int UserId, int AppointmentId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.PatientId == patient.PatientId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมาย";
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        string msg = CheckCanChange(ap);
        if (msg != "")
        {
            TempData["Error"] = msg;
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var schedules = (from s in _db.DoctorSchedules
                         where s.DoctorId == ap.DoctorId
                            && s.ScheduleId != ap.ScheduleId
                            && s.Status == "OPEN"
                            && s.AvailableQueue > 0
                            && s.ScheduleDate >= today
                         orderby s.ScheduleDate, s.StartTime
                         select s).ToList();

        ViewBag.UserId = UserId;
        ViewBag.Appointment = ap;
        ViewBag.ScheduleList = schedules;
        return View();
    }

    [HttpPost]
    public IActionResult DoReschedule(int UserId, int AppointmentId, int NewScheduleId)
    {
        var patient = GetPatient(UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.PatientId == patient.PatientId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมาย";
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        string msg = CheckCanChange(ap);
        if (msg != "")
        {
            TempData["Error"] = msg;
            return RedirectToAction("MyAppointments", new { UserId = UserId });
        }

        var newSchedule = (from s in _db.DoctorSchedules
                           where s.ScheduleId == NewScheduleId
                           select s).FirstOrDefault();
        if (newSchedule == null || newSchedule.DoctorId != ap.DoctorId
            || newSchedule.Status != "OPEN" || newSchedule.AvailableQueue <= 0)
        {
            TempData["Error"] = "รอบเวลาที่เลือกไม่ว่างแล้ว กรุณาเลือกใหม่";
            return RedirectToAction("Reschedule", new { UserId = UserId, AppointmentId = AppointmentId });
        }

        var oldSchedule = (from s in _db.DoctorSchedules
                           where s.ScheduleId == ap.ScheduleId
                           select s).FirstOrDefault();

        string oldText = ap.AppointmentDate.ToString("dd/MM/yyyy") + " " + ap.AppointmentTime.ToString("HH:mm");

        if (oldSchedule != null && oldSchedule.AvailableQueue < oldSchedule.MaxQueue)
        {
            oldSchedule.AvailableQueue = oldSchedule.AvailableQueue + 1;   // คืนคิวเก่า
        }
        newSchedule.AvailableQueue = newSchedule.AvailableQueue - 1;       // ตัดคิวใหม่

        ap.ScheduleId = newSchedule.ScheduleId;
        ap.AppointmentDate = newSchedule.ScheduleDate;
        ap.AppointmentTime = newSchedule.StartTime;
        ap.QueueNo = NextQueueNo(newSchedule);

        AppointmentStatusLog log = new AppointmentStatusLog();
        log.AppointmentId = ap.AppointmentId;
        log.OldStatus = "WAITING";
        log.NewStatus = "WAITING";
        log.Note = "เลื่อนนัดจาก " + oldText + " เป็น "
                   + newSchedule.ScheduleDate.ToString("dd/MM/yyyy") + " " + newSchedule.StartTime.ToString("HH:mm");
        log.ChangedBy = UserId;
        log.ChangedAt = DateTime.Now;
        _db.Add(log);

        _db.SaveChanges();

        TempData["Success"] = "เลื่อนนัดเรียบร้อยแล้ว หมายเลขคิวใหม่คือ " + ap.QueueNo;
        return RedirectToAction("MyAppointments", new { UserId = UserId });
    }
}