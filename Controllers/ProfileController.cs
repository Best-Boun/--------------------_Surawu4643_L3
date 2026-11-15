using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;
using ระบบจองคิวโรงพยาบาล.ViewModels;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class ProfileController : Controller
{
    private readonly Csi402dbContext _db;
    public ProfileController(Csi402dbContext db)
    {
        _db = db;
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

    [HttpGet]
    public IActionResult Index(int UserId)
    {
        var user = (from u in _db.Users
                    where u.UserId == UserId
                    select u).FirstOrDefault();
        if (user == null)
        {
            return RedirectToAction("Login", "Home");
        }

        var patient = (from p in _db.Patients
                       where p.UserId == UserId
                       select p).FirstOrDefault();

        ProfileViewModel vm = new ProfileViewModel();
        vm.UserId = user.UserId;
        vm.FirstName = user.FirstName;
        vm.LastName = user.LastName;
        vm.Email = user.Email;
        vm.Phone = user.Phone;
        vm.Address = user.Address;

        if (patient != null)
        {
            vm.BirthDate = patient.BirthDate;
            vm.Gender = patient.Gender;
            vm.EmergencyContact = patient.EmergencyContact;
            vm.EmergencyPhone = patient.EmergencyPhone;
            ViewBag.Age = CalcAge(patient.BirthDate);
        }
        else
        {
            ViewBag.Status = "กรุณากรอกข้อมูลโปรไฟล์ให้ครบ (โดยเฉพาะวันเกิด) ก่อนจองคิว";
        }

        ViewBag.UserId = UserId;
        ViewBag.Email = user.Email;
        return View(vm);
    }

    [HttpPost]
    public IActionResult Index(ProfileViewModel data)
    {
        ViewBag.UserId = data.UserId;

        var user = (from u in _db.Users
                    where u.UserId == data.UserId
                    select u).FirstOrDefault();
        if (user == null)
        {
            return RedirectToAction("Login", "Home");
        }
        ViewBag.Email = user.Email;

        if (!ModelState.IsValid)
        {
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

        user.FirstName = data.FirstName;
        user.LastName = data.LastName;
        user.Phone = data.Phone;
        user.Address = data.Address;
        user.Birthday = data.BirthDate;
        user.Gender = data.Gender;

        var patient = (from p in _db.Patients
                       where p.UserId == data.UserId
                       select p).FirstOrDefault();

        if (patient == null)
        {
            patient = new Patient();
            patient.UserId = user.UserId;
            _db.Add(patient);
        }

        patient.BirthDate = data.BirthDate.Value;
        patient.Gender = data.Gender;
        patient.Address = data.Address;
        patient.EmergencyContact = data.EmergencyContact;
        patient.EmergencyPhone = data.EmergencyPhone;

        _db.SaveChanges();

        ViewBag.Success = "บันทึกข้อมูลเรียบร้อยแล้ว";
        ViewBag.Age = CalcAge(data.BirthDate.Value);
        return View(data);
    }
}