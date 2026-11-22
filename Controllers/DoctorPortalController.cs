using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class DoctorPortalController : Controller
{
    private readonly Csi402dbContext _db;
    public DoctorPortalController(Csi402dbContext db)
    {
        _db = db;
    }

    // คืนข้อมูลแพทย์ของ UserId นี้ ถ้าไม่ใช่แพทย์ (หรือไม่มีแถวใน doctors) คืน null
    private Doctor? GetDoctor(int UserId)
    {
        var user = (from u in _db.Users
                    where u.UserId == UserId
                    select u).FirstOrDefault();
        if (user == null)
        {
            return null;
        }

        var role = (from r in _db.Roles
                    where r.RoleId == user.RoleId
                    select r).FirstOrDefault();
        if (role == null || role.RoleName != "Doctor")
        {
            return null;
        }

        return (from d in _db.Doctors
                where d.UserId == UserId
                select d).FirstOrDefault();
    }

    private int CalcAge(DateOnly birth)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        int age = today.Year - birth.Year;
        if (birth.AddYears(age) > today)
        {
            age--;
        }
        return age;
    }

    // คิวผู้ป่วยของแพทย์ตามวันที่ (ค่าเริ่มต้น = วันนี้)
    public IActionResult Index(int UserId, DateOnly? Date, string? Status)
    {
        var doctor = GetDoctor(UserId);
        if (doctor == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var date = Date ?? DateOnly.FromDateTime(DateTime.Now);
        if (string.IsNullOrEmpty(Status))
        {
            Status = "ALL";
        }

        var dayAll = (from a in _db.Appointments
                      where a.DoctorId == doctor.DoctorId && a.AppointmentDate == date
                      orderby a.AppointmentTime, a.QueueNo
                      select a).ToList();

        var list = dayAll.Where(x => Status == "ALL" || x.Status == Status).ToList();

        var ids = list.Select(x => x.AppointmentId).ToList();

        ViewBag.UserId = UserId;
        ViewBag.Date = date;
        ViewBag.FilterStatus = Status;
        ViewBag.AppointmentList = list;
        ViewBag.PatientList = (from p in _db.Patients select p).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.ServiceLines = (from l in _db.AppointmentServices
                                where ids.Contains(l.AppointmentId)
                                select l).ToList();
        ViewBag.ServiceList = (from s in _db.Services select s).ToList();
        ViewBag.PaidIds = (from p in _db.Payments
                           where p.PaymentStatus == "PAID" && ids.Contains(p.AppointmentId)
                           select p.AppointmentId).ToList();
        ViewBag.WaitingCount = dayAll.Count(x => x.Status == "WAITING");
        ViewBag.InProgressCount = dayAll.Count(x => x.Status == "IN_PROGRESS");
        ViewBag.CompletedCount = dayAll.Count(x => x.Status == "COMPLETED");
        ViewBag.DayTotal = dayAll.Count;
        ViewBag.DoctorInfo = doctor;
        ViewBag.DoctorUser = (from u in _db.Users where u.UserId == UserId select u).FirstOrDefault();
        ViewBag.DoctorDept = (from d in _db.Departments where d.DepartmentId == doctor.DepartmentId select d).FirstOrDefault();
        return View();
    }

    // ตารางนัดหมาย: รอบเวลาของแพทย์ตั้งแต่วันนี้เป็นต้นไป
    public IActionResult Schedule(int UserId)
    {
        var doctor = GetDoctor(UserId);
        if (doctor == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var schedules = (from s in _db.DoctorSchedules
                         where s.DoctorId == doctor.DoctorId && s.ScheduleDate >= today
                         orderby s.ScheduleDate, s.StartTime
                         select s).ToList();

        ViewBag.UserId = UserId;
        ViewBag.ScheduleList = schedules;
        ViewBag.AppointmentList = (from a in _db.Appointments
                                   where a.DoctorId == doctor.DoctorId && a.Status != "CANCELLED"
                                   select a).ToList();
        return View();
    }

    // ข้อมูลผู้ป่วยของนัดหมายนี้ + ประวัติการนัดกับแพทย์ท่านนี้
    public IActionResult PatientInfo(int UserId, int AppointmentId)
    {
        var doctor = GetDoctor(UserId);
        if (doctor == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.DoctorId == doctor.DoctorId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมายนี้ในคิวของคุณ";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        var patient = (from p in _db.Patients
                       where p.PatientId == ap.PatientId
                       select p).FirstOrDefault();
        if (patient == null)
        {
            TempData["Error"] = "ไม่พบข้อมูลผู้ป่วย";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        var pu = (from u in _db.Users
                  where u.UserId == patient.UserId
                  select u).FirstOrDefault();

        ViewBag.UserId = UserId;
        ViewBag.Appointment = ap;
        ViewBag.Patient = patient;
        ViewBag.PatientUser = pu;
        ViewBag.Age = CalcAge(patient.BirthDate);
        ViewBag.IsPaid = (from p in _db.Payments
                          where p.AppointmentId == AppointmentId && p.PaymentStatus == "PAID"
                          select p).Any();
        ViewBag.ServiceLines = (from l in _db.AppointmentServices
                                where l.AppointmentId == AppointmentId
                                select l).ToList();
        ViewBag.ServiceList = (from s in _db.Services select s).ToList();
        ViewBag.History = (from a in _db.Appointments
                           where a.PatientId == patient.PatientId && a.DoctorId == doctor.DoctorId
                           orderby a.AppointmentDate descending, a.AppointmentTime descending
                           select a).ToList();
        return View();
    }

    // อัปเดตสถานะการให้บริการ
    [HttpPost]
    public IActionResult UpdateStatus(int UserId, int AppointmentId, string NewStatus)
    {
        var doctor = GetDoctor(UserId);
        if (doctor == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var ap = (from a in _db.Appointments
                  where a.AppointmentId == AppointmentId && a.DoctorId == doctor.DoctorId
                  select a).FirstOrDefault();
        if (ap == null)
        {
            TempData["Error"] = "ไม่พบนัดหมายนี้ในคิวของคุณ";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        bool ok = (ap.Status == "WAITING" && (NewStatus == "IN_PROGRESS" || NewStatus == "NO_SHOW"))
               || (ap.Status == "IN_PROGRESS" && NewStatus == "COMPLETED");
        if (!ok)
        {
            TempData["Error"] = "ไม่สามารถเปลี่ยนสถานะจาก " + ap.Status + " เป็น " + NewStatus + " ได้";
            return RedirectToAction("Index", new { UserId = UserId, Date = ap.AppointmentDate.ToString("yyyy-MM-dd") });
        }
        if (NewStatus == "IN_PROGRESS")
        {
            var paid = (from p in _db.Payments
                        where p.AppointmentId == ap.AppointmentId && p.PaymentStatus == "PAID"
                        select p).FirstOrDefault();
            if (paid == null)
            {
                TempData["Error"] = "ผู้ป่วยยังไม่ได้ชำระเงิน หรือการเงินยังไม่อนุมัติ จึงยังเริ่มตรวจไม่ได้";
                return RedirectToAction("Index", new { UserId = UserId, Date = ap.AppointmentDate.ToString("yyyy-MM-dd") });
            }
        }

        string note = "";
        if (NewStatus == "IN_PROGRESS")
        {
            note = "แพทย์เริ่มตรวจ";
        }
        else if (NewStatus == "COMPLETED")
        {
            note = "แพทย์ตรวจเสร็จสิ้น";
        }
        else
        {
            note = "ผู้ป่วยไม่มาตามนัด";
        }

        AppointmentStatusLog log = new AppointmentStatusLog();
        log.AppointmentId = ap.AppointmentId;
        log.OldStatus = ap.Status;
        log.NewStatus = NewStatus;
        log.Note = note;
        log.ChangedBy = UserId;
        log.ChangedAt = DateTime.Now;
        _db.Add(log);

        ap.Status = NewStatus;
        _db.SaveChanges();

        TempData["Success"] = "อัปเดตสถานะคิว " + ap.QueueNo + " เป็น " + NewStatus + " แล้ว";
        return RedirectToAction("Index", new { UserId = UserId, Date = ap.AppointmentDate.ToString("yyyy-MM-dd") });
    }
}