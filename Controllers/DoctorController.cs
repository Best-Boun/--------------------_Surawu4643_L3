using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;
using ระบบจองคิวโรงพยาบาล.ViewModels;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class DoctorController : Controller
{
    private readonly Csi402dbContext _db;
    public DoctorController(Csi402dbContext db)
    {
        _db = db;
    }

    private Patient? GetPatient(int UserId)
    {
        return (from p in _db.Patients
                where p.UserId == UserId
                select p).FirstOrDefault();
    }

    // ข้อมูลที่หน้า Confirm / Summary ต้องใช้
    private void LoadScheduleInfo(DoctorSchedule schedule)
    {
        var doctor = (from d in _db.Doctors
                      where d.DoctorId == schedule.DoctorId
                      select d).FirstOrDefault();

        ViewBag.Schedule = schedule;
        ViewBag.DoctorInfo = doctor;
        ViewBag.DoctorUser = (from u in _db.Users
                              where u.UserId == doctor!.UserId
                              select u).FirstOrDefault();
        ViewBag.Dept = (from d in _db.Departments
                        where d.DepartmentId == doctor!.DepartmentId
                        select d).FirstOrDefault();
        ViewBag.ServiceList = (from s in _db.Services
                               where s.DepartmentId == doctor!.DepartmentId && s.Status == "ACTIVE"
                               select s).ToList();

        var today = DateOnly.FromDateTime(DateTime.Now);
        ViewBag.PromoList = (from p in _db.Promotions
                             where p.Status == "ACTIVE"
                                && (p.StartDate == null || p.StartDate <= today)
                                && (p.EndDate == null || p.EndDate >= today)
                             select p).ToList();
    }

    // คำนวณราคา + ตรวจสิทธิ์โปรโมชั่น
    private void Calculate(BookingViewModel data, Patient patient, DoctorSchedule schedule)
    {
        var doctor = (from d in _db.Doctors
                      where d.DoctorId == schedule.DoctorId
                      select d).FirstOrDefault();

        var ids = data.ServiceIds;
        data.Services = (from s in _db.Services
                         where ids.Contains(s.ServiceId)
                            && s.DepartmentId == doctor!.DepartmentId
                            && s.Status == "ACTIVE"
                         select s).ToList();

        data.Total = 0;
        bool hasPack = false;
        foreach (var s in data.Services)
        {
            data.Total = data.Total + s.Price;
            if (s.ServiceType == "HEALTH_PACKAGE")
            {
                hasPack = true;
            }
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        int age = today.Year - patient.BirthDate.Year;
        if (patient.BirthDate.AddYears(age) > today)
        {
            age--;
        }

        var promos = (from p in _db.Promotions
                      where p.Status == "ACTIVE"
                         && (p.StartDate == null || p.StartDate <= today)
                         && (p.EndDate == null || p.EndDate >= today)
                      select p).ToList();

        data.DiscountPercent = 0;
        data.UsedPromotions = new List<Promotion>();

        foreach (var p in promos)
        {
            bool ok = false;

            if (p.PromotionType == "AGE" && age >= (p.MinimumAge ?? 60))
            {
                ok = true;
            }
            else if (p.PromotionType == "HEALTH_PACKAGE" && hasPack)
            {
                ok = true;
            }
            else if (p.PromotionType == "FAMILY" && data.PersonCount >= (p.MinimumPerson ?? 3))
            {
                ok = true;
            }
            else if (p.PromotionType == "WEB")
            {
                ok = true;   // จองผ่านเว็บเสมอ
            }
            else if (p.PromotionType == "BIRTH_MONTH" && patient.BirthDate.Month == schedule.ScheduleDate.Month)
            {
                ok = true;
            }

            if (ok)
            {
                data.DiscountPercent = data.DiscountPercent + p.DiscountPercent;
                data.UsedPromotions.Add(p);
            }
        }

        if (data.DiscountPercent > 100)
        {
            data.DiscountPercent = 100;
        }

        data.DiscountAmount = Math.Round(data.Total * data.DiscountPercent / 100, 2);
        data.NetPrice = data.Total - data.DiscountAmount;
    }

    private string NextQueueNo(DoctorSchedule schedule)
    {
        int n = (from a in _db.Appointments
                 where a.ScheduleId == schedule.ScheduleId
                 select a).Count() + 1;

        string qn = "S" + schedule.ScheduleId + "-" + schedule.ScheduleDate.ToString("yyyyMMdd") + "-" + n.ToString("000");

        // กันเลขซ้ำ (queue_no ต้อง unique ทั้งตาราง)
        while ((from a in _db.Appointments where a.QueueNo == qn select a).Count() > 0)
        {
            n++;
            qn = "S" + schedule.ScheduleId + "-" + schedule.ScheduleDate.ToString("yyyyMMdd") + "-" + n.ToString("000");
        }
        return qn;
    }

    // ขั้น 1: เลือกแผนก (แสดงแผนก + บริการ + ราคา)
    public IActionResult Index(int UserId)
    {
        if (GetPatient(UserId) == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        ViewBag.UserId = UserId;
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();
        ViewBag.ServiceList = (from s in _db.Services
                               where s.Status == "ACTIVE"
                               select s).ToList();
        return View();
    }

    // ขั้น 2: เลือกแพทย์ (ค้นหาตามชื่อ / แผนก / ความเชี่ยวชาญ)
    public IActionResult SelectDoctor(int UserId, int? DepartmentId, string? Keyword, string? Specialization)
    {
        if (GetPatient(UserId) == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var doctors = (from d in _db.Doctors
                       join u in _db.Users on d.UserId equals u.UserId
                       where d.DoctorStatus == "ACTIVE"
                          && (DepartmentId == null || d.DepartmentId == DepartmentId)
                          && (string.IsNullOrEmpty(Specialization) || d.Specialization == Specialization)
                          && (string.IsNullOrEmpty(Keyword)
                              || u.FirstName.Contains(Keyword)
                              || u.LastName.Contains(Keyword)
                              || (u.FirstName + " " + u.LastName).Contains(Keyword)
                              || (d.Specialization != null && d.Specialization.Contains(Keyword)))
                       select d).ToList();

        ViewBag.UserId = UserId;
        ViewBag.DoctorList = doctors;
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();
        ViewBag.DepartmentId = DepartmentId;
        ViewBag.Keyword = Keyword;
        ViewBag.Specialization = Specialization;
        ViewBag.SpecializationList = (from d in _db.Doctors
                                      where d.DoctorStatus == "ACTIVE" && d.Specialization != null
                                      select d.Specialization!)
                                     .Distinct()
                                     .OrderBy(s => s)
                                     .ToList();

        var doctorIds = doctors.Select(d => d.DoctorId).ToList();
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);
        ViewBag.ScheduleList = (from s in _db.DoctorSchedules
                                where doctorIds.Contains(s.DoctorId)
                                   && s.Status == "OPEN"
                                   && s.AvailableQueue > 0
                                   && (s.ScheduleDate > today || (s.ScheduleDate == today && s.StartTime > nowTime))
                                orderby s.ScheduleDate, s.StartTime
                                select s).ToList();
        return View();
    }
    // ขั้น 3: เลือกวันและเวลา
    public IActionResult SelectDate(int UserId, int DoctorId)
    {
        if (GetPatient(UserId) == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var doctor = (from d in _db.Doctors
                      where d.DoctorId == DoctorId
                      select d).FirstOrDefault();
        if (doctor == null)
        {
            return RedirectToAction("Index", new { UserId = UserId });
        }

        ViewBag.UserId = UserId;
        ViewBag.DoctorUser = (from u in _db.Users
                              where u.UserId == doctor.UserId
                              select u).FirstOrDefault();

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);
        var schedules = (from s in _db.DoctorSchedules
                         where s.DoctorId == DoctorId
                            && s.Status == "OPEN"
                            && s.AvailableQueue > 0
                            && (s.ScheduleDate > today || (s.ScheduleDate == today && s.StartTime > nowTime))
                         orderby s.ScheduleDate, s.StartTime
                         select s).ToList();

        ViewBag.ScheduleList = schedules;
        return View();
    }

    // ขั้น 4: เลือกบริการ + จำนวนคน
    [HttpGet]
    public IActionResult Confirm(int UserId, int ScheduleId)
    {
        if (GetPatient(UserId) == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = UserId });
        }

        var schedule = (from s in _db.DoctorSchedules
                        where s.ScheduleId == ScheduleId
                        select s).FirstOrDefault();

        if (schedule == null || schedule.AvailableQueue <= 0)
        {
            TempData["Error"] = "คิวเต็มแล้วหรือไม่พบตารางเวลา กรุณาเลือกใหม่";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        ViewBag.UserId = UserId;
        LoadScheduleInfo(schedule);

        BookingViewModel vm = new BookingViewModel();
        vm.UserId = UserId;
        vm.ScheduleId = ScheduleId;
        return View(vm);
    }

    // ดูสรุปค่าใช้จ่าย + ส่วนลด + ยอดสุทธิ ก่อนยืนยัน
    [HttpPost]
    public IActionResult Summary(BookingViewModel data)
    {
        var patient = GetPatient(data.UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = data.UserId });
        }

        var schedule = (from s in _db.DoctorSchedules
                        where s.ScheduleId == data.ScheduleId
                        select s).FirstOrDefault();
        if (schedule == null || schedule.AvailableQueue <= 0)
        {
            TempData["Error"] = "คิวเต็มแล้วหรือไม่พบตารางเวลา กรุณาเลือกใหม่";
            return RedirectToAction("Index", new { UserId = data.UserId });
        }

        ViewBag.UserId = data.UserId;
        LoadScheduleInfo(schedule);

        if (data.PersonCount < 1)
        {
            data.PersonCount = 1;
        }

        if (data.ServiceIds == null || data.ServiceIds.Count == 0)
        {
            ViewBag.Status = "กรุณาเลือกบริการอย่างน้อย 1 รายการ";
            return View("Confirm", data);
        }

        Calculate(data, patient, schedule);
        return View(data);
    }

    // บันทึกการจองจริง
    [HttpPost]
    public IActionResult Book(BookingViewModel data)
    {
        var patient = GetPatient(data.UserId);
        if (patient == null)
        {
            return RedirectToAction("Index", "Profile", new { UserId = data.UserId });
        }

        var schedule = (from s in _db.DoctorSchedules
                        where s.ScheduleId == data.ScheduleId
                        select s).FirstOrDefault();

        // ตรวจจำนวนคิวว่างก่อนยืนยัน
        if (schedule == null || schedule.Status != "OPEN" || schedule.AvailableQueue <= 0)
        {
            TempData["Error"] = "คิวเต็มแล้วหรือไม่พบตารางเวลา กรุณาเลือกใหม่";
            return RedirectToAction("Index", new { UserId = data.UserId });
        }

        if (data.ServiceIds == null || data.ServiceIds.Count == 0)
        {
            TempData["Error"] = "กรุณาเลือกบริการอย่างน้อย 1 รายการ";
            return RedirectToAction("Confirm", new { UserId = data.UserId, ScheduleId = data.ScheduleId });
        }

        var duplicate = (from a in _db.Appointments
                         where a.PatientId == patient.PatientId
                            && a.ScheduleId == schedule.ScheduleId
                            && a.Status != "CANCELLED"
                         select a).FirstOrDefault();
        if (duplicate != null)
        {
            TempData["Error"] = "คุณจองคิวในรอบเวลานี้ไปแล้ว";
            return RedirectToAction("MyAppointments", "Appointment", new { UserId = data.UserId });
        }

        if (data.PersonCount < 1)
        {
            data.PersonCount = 1;
        }

        // คำนวณใหม่ที่ฝั่ง server เสมอ (ไม่เชื่อค่าราคาจากฟอร์ม)
        Calculate(data, patient, schedule);

        if (data.Services.Count == 0)
        {
            TempData["Error"] = "ไม่พบบริการที่เลือก";
            return RedirectToAction("Confirm", new { UserId = data.UserId, ScheduleId = data.ScheduleId });
        }

        Appointment ap = new Appointment();
        ap.QueueNo = NextQueueNo(schedule);
        ap.PatientId = patient.PatientId;
        ap.DoctorId = schedule.DoctorId;
        ap.ScheduleId = schedule.ScheduleId;
        ap.AppointmentDate = schedule.ScheduleDate;
        ap.AppointmentTime = schedule.StartTime;
        ap.BookingChannel = "WEB";
        ap.Status = "WAITING";
        ap.TotalPrice = data.Total;
        ap.DiscountAmount = data.DiscountAmount;
        ap.NetPrice = data.NetPrice;
        ap.CreatedAt = DateTime.Now;
        _db.Add(ap);

        foreach (var s in data.Services)
        {
            AppointmentService line = new AppointmentService();
            line.Appointment = ap;
            line.ServiceId = s.ServiceId;
            line.Quantity = 1;
            line.UnitPrice = s.Price;
            line.Subtotal = s.Price;
            _db.Add(line);
        }

        foreach (var p in data.UsedPromotions)
        {
            AppointmentPromotion usedPromo = new AppointmentPromotion();
            usedPromo.Appointment = ap;
            usedPromo.PromotionId = p.PromotionId;
            usedPromo.DiscountPercent = p.DiscountPercent;
            usedPromo.DiscountAmount = Math.Round(data.Total * p.DiscountPercent / 100, 2);
            _db.Add(usedPromo);
        }

        AppointmentStatusLog log = new AppointmentStatusLog();
        log.Appointment = ap;
        log.OldStatus = null;
        log.NewStatus = "WAITING";
        log.Note = "ผู้ป่วยจองคิวผ่านเว็บ";
        log.ChangedBy = data.UserId;
        log.ChangedAt = DateTime.Now;
        _db.Add(log);

        schedule.AvailableQueue = schedule.AvailableQueue - 1;

        _db.SaveChanges();   // บันทึกทุกอย่างพร้อมกันในครั้งเดียว

        TempData["Success"] = "จองคิวสำเร็จ หมายเลขคิวของคุณคือ " + ap.QueueNo;
        return RedirectToAction("MyAppointments", "Appointment", new { UserId = data.UserId });
    }
}