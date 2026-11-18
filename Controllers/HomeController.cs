using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;
using ระบบจองคิวโรงพยาบาล.ViewModels;
namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class HomeController : Controller
{
    public IActionResult Index(string Username, string Password)
    {

        ViewBag.Username = Username;
        ViewBag.Password = Password;
        return View();
    }

    public IActionResult Doctor()
    {
        return View();
    }

    public IActionResult HospitalQueue()
    {

        string Name, Lastname, Room, Year, Language;
        Name = "Surawut";
        Lastname = "Boonyu";
        Room = "L3";
        Language = "HTML";
        Year = "Year 3";
        int score1 = 5;
        int score2 = 8;
        int score3 = 12;
        int score4 = 10;
        int score5 = 10;
        int score6 = 7;
        int score7 = 9;
        int score8 = 4;
        int score9 = 6;
        int score10 = 10;

        int Sum = score1 + score2 + score3 + score4 + score5 + score6 + score7 + score8 + score9 + score10;

        string Grade;

        if (Sum >= 80)
        {
            Grade = " Grade A";
        }
        else if (Sum >= 76)
        {
            Grade = " Grade B+";
        }
        else if (Sum >= 70)
        {
            Grade = " Grade B";
        }
        else if (Sum >= 66)
        {
            Grade = " Grade C+";
        }
        else if (Sum >= 60)
        {
            Grade = " Grade C";
        }
        else if (Sum >= 56)
        {
            Grade = " Grade D+";
        }
        else if (Sum >= 50)
        {
            Grade = " Grade D";
        }
        else
        {
            Grade = " Grade F";
        }

        ViewBag.Name = Name;
        ViewBag.Lastname = Lastname;
        ViewBag.Room = Room;
        ViewBag.Language = Language;
        ViewBag.Year = Year;
        ViewBag.Grade = Grade;
        ViewBag.Sum = Sum;
        ViewBag.score1 = score1;
        ViewBag.score2 = score2;
        ViewBag.score3 = score3;
        ViewBag.score4 = score4;
        ViewBag.score5 = score5;
        ViewBag.score6 = score6;
        ViewBag.score7 = score7;
        ViewBag.score8 = score8;
        ViewBag.score9 = score9;
        ViewBag.score10 = score10;

        return View();
    }
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Login(LoginRegisViewModel data)
    {
        if (!ModelState.IsValid)
        {
            return View(data);
        }

        var user = (from u in _db.Users
                    where u.Email == data.Email
                    select u).FirstOrDefault();

        if (user == null || user.PasswordHash != data.Password)
        {
            ViewBag.Status = "อีเมลหรือรหัสผ่านไม่ถูกต้อง";
            return View(data);
        }

        TempData["LoginSuccess"] = user.FirstName + " " + user.LastName;

        var role = (from r in _db.Roles
                    where r.RoleId == user.RoleId
                    select r).FirstOrDefault();

        if (role != null && role.RoleName == "Patient")
        {
            var patient = (from p in _db.Patients
                           where p.UserId == user.UserId
                           select p).FirstOrDefault();

            // บัญชีเก่าที่ยังไม่มีข้อมูล patient ให้ไปกรอกโปรไฟล์ก่อน
            if (patient == null)
            {
                return RedirectToAction("Index", "Profile", new { UserId = user.UserId });
            }
            return RedirectToAction("Index", "Doctor", new { UserId = user.UserId });
        }

        if (role != null && role.RoleName == "FinanceStaff")
        {
            return RedirectToAction("Index", "Finance", new { UserId = user.UserId });
        }
        if (role != null && role.RoleName == "ScheduleManager")
        {
            return RedirectToAction("Index", "Schedule", new { UserId = user.UserId });
        }
        if (role != null && role.RoleName == "Doctor")
        {
            return RedirectToAction("Index", "DoctorPortal", new { UserId = user.UserId });
        }
        if (role != null && role.RoleName == "Admin")
        {
            return RedirectToAction("Index", "Admin", new { UserId = user.UserId });
        }
        if (role != null && role.RoleName == "Manager")
        {
            return RedirectToAction("Index", "Manager", new { UserId = user.UserId });
        }
        if (user.Status != null && !string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Status = "บัญชีนี้ถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ";
            return View(data);
        }

        return RedirectToAction("Index", "Home");
    }
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Register(LoginRegisViewModel data)
    {
        if (string.IsNullOrWhiteSpace(data.FirstName) ||
            string.IsNullOrWhiteSpace(data.LastName))
        {
            ViewBag.Status = "กรุณากรอกชื่อและนามสกุล";
            return View(data);
        }

        if (data.BirthDate == null)
        {
            ViewBag.Status = "กรุณาระบุวันเกิด";
            return View(data);
        }

        if (data.BirthDate.Value > DateOnly.FromDateTime(DateTime.Now))
        {
            ViewBag.Status = "วันเกิดต้องไม่เป็นวันในอนาคต";
            return View(data);
        }

        if (!ModelState.IsValid)
        {
            return View(data);
        }

        var existingUser = (from u in _db.Users
                            where u.Email == data.Email
                            select u).FirstOrDefault();

        if (existingUser != null)
        {
            ViewBag.Status = "อีเมลนี้ถูกใช้แล้ว";
            return View(data);
        }

        var role = (from r in _db.Roles
                    where r.RoleName == "Patient"
                    select r).FirstOrDefault();

        if (role == null)
        {
            ViewBag.Status = "ไม่พบ Role Patient ในฐานข้อมูล";
            return View(data);
        }

        User user = new User();
        user.RoleId = role.RoleId;
        user.Email = data.Email;
        user.PasswordHash = data.Password;
        user.FirstName = data.FirstName;
        user.LastName = data.LastName;
        user.Phone = data.Phone;
        user.Address = data.Address;
        user.Birthday = data.BirthDate;
        user.Status = "ACTIVE";
        user.CreatedAt = DateTime.Now;
        _db.Add(user);

        Patient patient = new Patient();
        patient.User = user;                       // ผูกกับ user ข้างบน (บันทึกครั้งเดียวได้ทั้งคู่)
        patient.BirthDate = data.BirthDate.Value;
        patient.Address = data.Address;
        _db.Add(patient);

        _db.SaveChanges();

        return RedirectToAction("Login");
    }

    public IActionResult Logout()
    {
        return RedirectToAction("Login", "Home");
    }

    public IActionResult DeleteUser(int UserId)
    {
        var check = (from u in _db.Users
                     where u.UserId == UserId
                     select u).FirstOrDefault();

        if (check != null)
        {
            _db.Remove(check);
            _db.SaveChanges();
            return RedirectToAction("UserList", "Home", new { status = "ลบสำเร็จ" });
        }
        else
        {
            return RedirectToAction("UserList", "Home", new { status = "ลบไม่สำเร็จ" });
        }
    }

    private readonly Csi402dbContext _db;
    public HomeController(Csi402dbContext db)
    {
        _db = db;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
