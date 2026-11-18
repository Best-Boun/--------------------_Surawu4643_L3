using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class ManagerController : Controller
{
    private readonly Csi402dbContext _db;
    public ManagerController(Csi402dbContext db)
    {
        _db = db;
    }

    // เช็คว่า UserId นี้เป็น Manager จริงไหม
    private bool IsManager(int UserId)
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
        return role != null && role.RoleName == "Manager";
    }

    // Dashboard: จำนวนผู้ป่วย + จำนวนการจอง ตามช่วงวันที่ (ค่าเริ่มต้น = ตั้งแต่ต้นเดือนถึงวันนี้)
    public IActionResult Index(int UserId, DateOnly? From, DateOnly? To)
    {
        if (!IsManager(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var from = From ?? new DateOnly(today.Year, today.Month, 1);
        var to = To ?? today;

        if (to < from)
        {
            ViewBag.Status = "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม ระบบสลับวันที่ให้แล้ว";
            var tmp = from;
            from = to;
            to = tmp;
        }

        // การจองที่วันนัดอยู่ในช่วงที่เลือก
        var aps = (from a in _db.Appointments
                   where a.AppointmentDate >= from && a.AppointmentDate <= to
                   orderby a.AppointmentDate
                   select a).ToList();

        // ผู้ป่วยที่สมัครในช่วงที่เลือก
        DateTime fromTime = from.ToDateTime(TimeOnly.MinValue);
        DateTime toTime = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        int newPatients = (from p in _db.Patients
                           join u in _db.Users on p.UserId equals u.UserId
                           where u.CreatedAt >= fromTime && u.CreatedAt < toTime
                           select p).Count();

        ViewBag.UserId = UserId;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.AppointmentList = aps;
        ViewBag.PatientTotal = (from p in _db.Patients select p).Count();
        ViewBag.NewPatients = newPatients;
        ViewBag.BookedPatients = aps.Select(x => x.PatientId).Distinct().Count();
        ViewBag.DoctorList = (from d in _db.Doctors select d).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();
        return View();
    }
}