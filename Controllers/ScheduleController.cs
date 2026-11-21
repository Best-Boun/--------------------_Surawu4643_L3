using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;
using ระบบจองคิวโรงพยาบาล.ViewModels;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class ScheduleController : Controller
{
    private readonly Csi402dbContext _db;
    public ScheduleController(Csi402dbContext db)
    {
        _db = db;
    }

    // เช็คว่า UserId นี้เป็นผู้ดูแลตารางแพทย์จริงไหม
    private bool IsScheduleManager(int UserId)
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
        return role != null && role.RoleName == "ScheduleManager";
    }

    // จำนวนคนที่จองรอบนี้อยู่ (ไม่นับที่ยกเลิก)
    private int CountBooked(int ScheduleId)
    {
        return (from a in _db.Appointments
                where a.ScheduleId == ScheduleId && a.Status != "CANCELLED"
                select a).Count();
    }

    // รายชื่อแพทย์ใช้ใน dropdown
    private void LoadDoctors()
    {
        ViewBag.DoctorList = (from d in _db.Doctors
                              where d.DoctorStatus == "ACTIVE"
                              select d).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
    }

    // ตรวจข้อมูลฟอร์ม คืนข้อความผิดพลาด ถ้าว่างแปลว่าผ่าน
    private string CheckForm(ScheduleViewModel data)
    {
        if (data.ScheduleDate == null || data.StartTime == null || data.EndTime == null)
        {
            return "กรุณาระบุวันที่ เวลาเริ่ม และเวลาสิ้นสุด";
        }

        if (data.EndTime.Value <= data.StartTime.Value)
        {
            return "เวลาสิ้นสุดต้องมากกว่าเวลาเริ่ม";
        }

        if (data.MaxQueue < 1)
        {
            return "จำนวนคิวที่รองรับต้องมากกว่า 0";
        }

        // แพทย์คนเดียวกัน วันเดียวกัน เวลาห้ามซ้อนกัน
        var date = data.ScheduleDate.Value;
        var start = data.StartTime.Value;
        var end = data.EndTime.Value;
        var overlap = (from s in _db.DoctorSchedules
                       where s.DoctorId == data.DoctorId
                          && s.ScheduleDate == date
                          && s.ScheduleId != data.ScheduleId
                          && s.StartTime < end
                          && s.EndTime > start
                       select s).FirstOrDefault();
        if (overlap != null)
        {
            return "แพทย์ท่านนี้มีรอบเวลาซ้อนกับรอบ "
                   + overlap.StartTime.ToString("HH:mm") + " - " + overlap.EndTime.ToString("HH:mm")
                   + " ในวันเดียวกัน";
        }
        return "";
    }

    // หน้าหลัก: รายการตารางแพทย์ (กรองตามแพทย์ / ช่วงเวลา)
    public IActionResult Index(int UserId, int? DoctorId, string? Period)
    {
        if (!IsScheduleManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        if (string.IsNullOrEmpty(Period))
        {
            Period = "UPCOMING";
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var schedules = (from s in _db.DoctorSchedules
                         where (DoctorId == null || s.DoctorId == DoctorId)
                            && (Period == "ALL" || s.ScheduleDate >= today)
                         orderby s.ScheduleDate, s.StartTime
                         select s).ToList();

        ViewBag.UserId = UserId;
        ViewBag.ScheduleList = schedules;
        ViewBag.AppointmentList = (from a in _db.Appointments
                                   where a.Status != "CANCELLED"
                                   select a).ToList();
        ViewBag.FilterDoctorId = DoctorId;
        ViewBag.Period = Period;
        LoadDoctors();
        return View();
    }

    [HttpGet]
    public IActionResult Create(int UserId)
    {
        if (!IsScheduleManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        LoadDoctors();

        ScheduleViewModel vm = new ScheduleViewModel();
        vm.UserId = UserId;
        return View(vm);
    }

    [HttpPost]
    public IActionResult Create(ScheduleViewModel data)
    {
        if (!IsScheduleManager(data.UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = data.UserId;
        LoadDoctors();

        var doctor = (from d in _db.Doctors
                      where d.DoctorId == data.DoctorId
                      select d).FirstOrDefault();
        if (doctor == null)
        {
            ViewBag.Status = "กรุณาเลือกแพทย์";
            return View(data);
        }

        string msg = CheckForm(data);
        if (msg != "")
        {
            ViewBag.Status = msg;
            return View(data);
        }

        DoctorSchedule sc = new DoctorSchedule();
        sc.DoctorId = data.DoctorId;
        sc.ScheduleDate = data.ScheduleDate.Value;
        sc.StartTime = data.StartTime.Value;
        sc.EndTime = data.EndTime!.Value;
        sc.MaxQueue = data.MaxQueue;
        sc.AvailableQueue = data.MaxQueue;
        sc.Status = "OPEN";
        _db.Add(sc);
        _db.SaveChanges();

        TempData["Success"] = "เพิ่มรอบเวลาเรียบร้อยแล้ว";
        return RedirectToAction("Index", new { UserId = data.UserId });
    }

    [HttpGet]
    public IActionResult Edit(int UserId, int ScheduleId)
    {
        if (!IsScheduleManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var sc = (from s in _db.DoctorSchedules
                  where s.ScheduleId == ScheduleId
                  select s).FirstOrDefault();
        if (sc == null)
        {
            TempData["Error"] = "ไม่พบรอบเวลา";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        ScheduleViewModel vm = new ScheduleViewModel();
        vm.UserId = UserId;
        vm.ScheduleId = sc.ScheduleId;
        vm.DoctorId = sc.DoctorId;
        vm.ScheduleDate = sc.ScheduleDate;
        vm.StartTime = sc.StartTime;
        vm.EndTime = sc.EndTime;
        vm.MaxQueue = sc.MaxQueue;
        vm.Status = sc.Status;

        ViewBag.UserId = UserId;
        ViewBag.Booked = CountBooked(ScheduleId);
        LoadDoctors();
        return View(vm);
    }

    [HttpPost]
    public IActionResult Edit(ScheduleViewModel data)
    {
        if (!IsScheduleManager(data.UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var sc = (from s in _db.DoctorSchedules
                  where s.ScheduleId == data.ScheduleId
                  select s).FirstOrDefault();
        if (sc == null)
        {
            TempData["Error"] = "ไม่พบรอบเวลา";
            return RedirectToAction("Index", new { UserId = data.UserId });
        }

        int booked = CountBooked(sc.ScheduleId);
        ViewBag.UserId = data.UserId;
        ViewBag.Booked = booked;
        LoadDoctors();

        // แพทย์ของรอบนี้เปลี่ยนไม่ได้
        data.DoctorId = sc.DoctorId;

        string msg = CheckForm(data);
        if (msg != "")
        {
            ViewBag.Status = msg;
            return View(data);
        }

        if (data.MaxQueue < booked)
        {
            ViewBag.Status = "ลดจำนวนคิวต่ำกว่าจำนวนที่จองไปแล้วไม่ได้ (จองแล้ว " + booked + " คิว)";
            return View(data);
        }

        // ถ้ามีคนจองแล้ว ห้ามเปลี่ยนวัน/เวลา เพราะนัดหมายเก็บวัน/เวลาเดิมไว้
        bool timeChanged = data.ScheduleDate!.Value != sc.ScheduleDate
                        || data.StartTime!.Value != sc.StartTime
                        || data.EndTime!.Value != sc.EndTime;
        if (booked > 0 && timeChanged)
        {
            ViewBag.Status = "รอบนี้มีผู้จองแล้ว ไม่สามารถเปลี่ยนวัน/เวลาได้ (ถ้าจำเป็นให้ปิดรอบแล้วสร้างรอบใหม่)";
            return View(data);
        }

        sc.ScheduleDate = data.ScheduleDate.Value;
        sc.StartTime = data.StartTime.Value;
        sc.EndTime = data.EndTime.Value;
        sc.MaxQueue = data.MaxQueue;
        sc.AvailableQueue = data.MaxQueue - booked;   // คำนวณคิวว่างใหม่
        sc.Status = (data.Status == "CLOSED") ? "CLOSED" : "OPEN";
        _db.SaveChanges();

        TempData["Success"] = "บันทึกการแก้ไขรอบเวลาเรียบร้อยแล้ว";
        return RedirectToAction("Index", new { UserId = data.UserId });
    }

    // เปิด/ปิดรอบ (ปิดแล้วผู้ป่วยจะไม่เห็นรอบนี้)
    [HttpPost]
    public IActionResult ToggleStatus(int UserId, int ScheduleId)
    {
        if (!IsScheduleManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var sc = (from s in _db.DoctorSchedules
                  where s.ScheduleId == ScheduleId
                  select s).FirstOrDefault();
        if (sc == null)
        {
            TempData["Error"] = "ไม่พบรอบเวลา";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        if (sc.Status == "OPEN")
        {
            sc.Status = "CLOSED";
            TempData["Success"] = "ปิดรอบเวลาแล้ว (คนที่จองไว้แล้วยังคงมีนัดตามเดิม)";
        }
        else
        {
            sc.Status = "OPEN";
            TempData["Success"] = "เปิดรอบเวลาให้จองแล้ว";
        }
        _db.SaveChanges();
        return RedirectToAction("Index", new { UserId = UserId });
    }

    // ลบรอบ (ลบได้เฉพาะรอบที่ไม่เคยมีการจอง)
    [HttpPost]
    public IActionResult Delete(int UserId, int ScheduleId)
    {
        if (!IsScheduleManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var sc = (from s in _db.DoctorSchedules
                  where s.ScheduleId == ScheduleId
                  select s).FirstOrDefault();
        if (sc == null)
        {
            TempData["Error"] = "ไม่พบรอบเวลา";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        int all = (from a in _db.Appointments
                   where a.ScheduleId == ScheduleId
                   select a).Count();
        if (all > 0)
        {
            TempData["Error"] = "รอบนี้เคยมีการจองแล้ว ลบไม่ได้ กรุณาปิดรอบแทน";
            return RedirectToAction("Index", new { UserId = UserId });
        }

        _db.Remove(sc);
        _db.SaveChanges();

        TempData["Success"] = "ลบรอบเวลาเรียบร้อยแล้ว";
        return RedirectToAction("Index", new { UserId = UserId });
    }
}