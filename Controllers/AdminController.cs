using Microsoft.AspNetCore.Mvc;
using ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Models;
using ระบบจองคิวโรงพยาบาล.ViewModels;

namespace ระบบจองคิวโรงพยายาบา_Surawu4643_L3.Controllers;

public class AdminController : Controller
{
    private readonly Csi402dbContext _db;
    public AdminController(Csi402dbContext db)
    {
        _db = db;
    }

    private string GetRoleName(int RoleId)
    {
        var role = (from r in _db.Roles
                    where r.RoleId == RoleId
                    select r).FirstOrDefault();
        return role != null ? role.RoleName : "";
    }

    // เช็คว่า UserId นี้เป็น Admin จริงไหม
    private bool IsAdmin(int UserId)
    {
        var user = (from u in _db.Users
                    where u.UserId == UserId
                    select u).FirstOrDefault();
        if (user == null)
        {
            return false;
        }
        return GetRoleName(user.RoleId) == "Admin";
    }

    private void LoadFormLists()
    {
        ViewBag.RoleList = (from r in _db.Roles select r).ToList();
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();
    }

    // ---------------- ภาพรวม ----------------
    public IActionResult Index(int UserId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        ViewBag.RoleList = (from r in _db.Roles select r).ToList();
        ViewBag.UserList = (from u in _db.Users select u).ToList();
        ViewBag.DoctorCount = (from d in _db.Doctors select d).Count();
        ViewBag.PatientCount = (from p in _db.Patients select p).Count();
        ViewBag.ActivePromoCount = (from p in _db.Promotions where p.Status == "ACTIVE" select p).Count();
        ViewBag.DeptCount = (from d in _db.Departments select d).Count();
        ViewBag.ServiceCount = (from s in _db.Services select s).Count();
        return View();
    }

    // ---------------- ผู้ใช้และสิทธิ์ ----------------
    public IActionResult UserList(int UserId, string? Keyword, int? RoleId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var users = (from u in _db.Users
                     where (RoleId == null || u.RoleId == RoleId)
                        && (string.IsNullOrEmpty(Keyword)
                            || u.FirstName.Contains(Keyword)
                            || u.LastName.Contains(Keyword)
                            || u.Email.Contains(Keyword))
                     orderby u.UserId
                     select u).ToList();

        ViewBag.UserId = UserId;
        ViewBag.UserList = users;
        ViewBag.RoleList = (from r in _db.Roles select r).ToList();
        ViewBag.Keyword = Keyword;
        ViewBag.FilterRoleId = RoleId;
        return View();
    }

    [HttpGet]
    public IActionResult UserForm(int UserId, int? TargetId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        LoadFormLists();

        UserFormViewModel vm = new UserFormViewModel();
        vm.AdminId = UserId;

        if (TargetId != null)
        {
            var target = (from u in _db.Users
                          where u.UserId == TargetId
                          select u).FirstOrDefault();
            if (target == null)
            {
                TempData["Error"] = "ไม่พบผู้ใช้";
                return RedirectToAction("UserList", new { UserId = UserId });
            }

            vm.UserId = target.UserId;
            vm.FirstName = target.FirstName;
            vm.LastName = target.LastName;
            vm.Email = target.Email;
            vm.Phone = target.Phone;
            vm.Address = target.Address;
            vm.RoleId = target.RoleId;
            vm.Status = string.Equals(target.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";

            var doctor = (from d in _db.Doctors
                          where d.UserId == target.UserId
                          select d).FirstOrDefault();
            if (doctor != null)
            {
                vm.DepartmentId = doctor.DepartmentId;
                vm.LicenseNo = doctor.LicenseNo;
                vm.Specialization = doctor.Specialization;
                vm.Biography = doctor.Biography;
            }

            ViewBag.IsPatient = GetRoleName(target.RoleId) == "Patient";
            ViewBag.IsSelf = target.UserId == UserId;
            ViewBag.TargetRoleName = GetRoleName(target.RoleId);
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult UserForm(UserFormViewModel data)
    {
        if (!IsAdmin(data.AdminId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = data.AdminId;
        LoadFormLists();

        User? target = null;
        Doctor? doctor = null;
        if (data.UserId > 0)
        {
            target = (from u in _db.Users
                      where u.UserId == data.UserId
                      select u).FirstOrDefault();
            if (target == null)
            {
                TempData["Error"] = "ไม่พบผู้ใช้";
                return RedirectToAction("UserList", new { UserId = data.AdminId });
            }
            doctor = (from d in _db.Doctors
                      where d.UserId == target.UserId
                      select d).FirstOrDefault();
        }

        bool isPatient = target != null && GetRoleName(target.RoleId) == "Patient";
        bool isSelf = target != null && target.UserId == data.AdminId;
        ViewBag.IsPatient = isPatient;
        ViewBag.IsSelf = isSelf;
        if (target != null)
        {
            ViewBag.TargetRoleName = GetRoleName(target.RoleId);
        }

        // ผู้ป่วย และตัว Admin เอง เปลี่ยน Role ไม่ได้ / Admin ระงับตัวเองไม่ได้
        if (isPatient || isSelf)
        {
            data.RoleId = target!.RoleId;
        }
        if (isSelf)
        {
            data.Status = "ACTIVE";
        }
        if (data.Status != "INACTIVE")
        {
            data.Status = "ACTIVE";
        }

        string roleName = GetRoleName(data.RoleId);
        string msg = "";

        if (string.IsNullOrWhiteSpace(data.FirstName) || string.IsNullOrWhiteSpace(data.LastName))
        {
            msg = "กรุณากรอกชื่อและนามสกุล";
        }
        else if (string.IsNullOrWhiteSpace(data.Email))
        {
            msg = "กรุณากรอกอีเมล";
        }
        else if ((from u in _db.Users
                  where u.Email == data.Email.Trim() && u.UserId != data.UserId
                  select u).FirstOrDefault() != null)
        {
            msg = "อีเมลนี้ถูกใช้แล้ว";
        }
        else if (target == null && string.IsNullOrWhiteSpace(data.Password))
        {
            msg = "กรุณากำหนดรหัสผ่าน";
        }
        else if (roleName == "")
        {
            msg = "กรุณาเลือก Role";
        }
        else if (roleName == "Patient" && !isPatient)
        {
            msg = "บัญชีผู้ป่วยต้องสมัครผ่านหน้าลงทะเบียนเท่านั้น";
        }
        else if (roleName == "Doctor")
        {
            if (data.DepartmentId == null
                || (from d in _db.Departments where d.DepartmentId == data.DepartmentId select d).FirstOrDefault() == null)
            {
                msg = "กรุณาเลือกแผนกของแพทย์";
            }
            else if (string.IsNullOrWhiteSpace(data.LicenseNo))
            {
                msg = "กรุณากรอกเลขใบอนุญาตประกอบวิชาชีพ";
            }
            else if ((from d in _db.Doctors
                      where d.LicenseNo == data.LicenseNo.Trim() && d.UserId != data.UserId
                      select d).FirstOrDefault() != null)
            {
                msg = "เลขใบอนุญาตนี้ถูกใช้แล้ว";
            }
        }
        else if (doctor != null)
        {
            // เปลี่ยนจากแพทย์เป็น Role อื่น: ต้องไม่มีตารางหรือนัดหมายค้างอยู่
            int used = (from s in _db.DoctorSchedules where s.DoctorId == doctor.DoctorId select s).Count()
                     + (from a in _db.Appointments where a.DoctorId == doctor.DoctorId select a).Count();
            if (used > 0)
            {
                msg = "แพทย์ท่านนี้มีตารางหรือนัดหมายแล้ว เปลี่ยน Role ไม่ได้ ให้ตั้งสถานะเป็น INACTIVE แทน";
            }
        }

        if (msg != "")
        {
            ViewBag.Status = msg;
            return View(data);
        }

        User user;
        if (target == null)
        {
            user = new User();
            user.PasswordHash = data.Password!;
            user.CreatedAt = DateTime.Now;
            _db.Add(user);
        }
        else
        {
            user = target;
            if (!string.IsNullOrWhiteSpace(data.Password))
            {
                user.PasswordHash = data.Password;
            }
        }

        user.RoleId = data.RoleId;
        user.Email = data.Email.Trim();
        user.FirstName = data.FirstName.Trim();
        user.LastName = data.LastName.Trim();
        user.Phone = data.Phone;
        user.Address = data.Address;
        user.Status = data.Status;

        if (roleName == "Doctor")
        {
            if (doctor == null)
            {
                doctor = new Doctor();
                doctor.User = user;
                _db.Add(doctor);
            }
            doctor.DepartmentId = data.DepartmentId!.Value;
            doctor.LicenseNo = data.LicenseNo!.Trim();
            doctor.Specialization = data.Specialization;
            doctor.Biography = data.Biography;
            doctor.DoctorStatus = data.Status;   // ระงับบัญชี = แพทย์หายจากหน้าค้นหา
        }
        else if (doctor != null)
        {
            _db.Remove(doctor);
        }

        _db.SaveChanges();

        TempData["Success"] = "บันทึกข้อมูลผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction("UserList", new { UserId = data.AdminId });
    }

    [HttpPost]
    public IActionResult DeleteUser(int UserId, int TargetId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var target = (from u in _db.Users
                      where u.UserId == TargetId
                      select u).FirstOrDefault();
        if (target == null)
        {
            TempData["Error"] = "ไม่พบผู้ใช้";
            return RedirectToAction("UserList", new { UserId = UserId });
        }

        if (target.UserId == UserId)
        {
            TempData["Error"] = "ไม่สามารถลบบัญชีของตัวเองได้";
            return RedirectToAction("UserList", new { UserId = UserId });
        }

        var patient = (from p in _db.Patients where p.UserId == TargetId select p).FirstOrDefault();
        var doctor = (from d in _db.Doctors where d.UserId == TargetId select d).FirstOrDefault();

        // นับข้อมูลที่ผูกกับผู้ใช้นี้อยู่
        int used = 0;
        if (patient != null)
        {
            used = used + (from a in _db.Appointments where a.PatientId == patient.PatientId select a).Count();
            used = used + (from p in _db.Payments where p.PatientId == patient.PatientId select p).Count();
        }
        if (doctor != null)
        {
            used = used + (from a in _db.Appointments where a.DoctorId == doctor.DoctorId select a).Count();
            used = used + (from s in _db.DoctorSchedules where s.DoctorId == doctor.DoctorId select s).Count();
        }
        used = used + (from l in _db.AppointmentStatusLogs where l.ChangedBy == TargetId select l).Count();
        used = used + (from p in _db.Payments where p.VerifiedBy == TargetId select p).Count();
        used = used + (from r in _db.Receipts where r.IssuedBy == TargetId select r).Count();

        if (used > 0)
        {
            TempData["Error"] = "ผู้ใช้นี้มีข้อมูลการใช้งานในระบบแล้ว ลบไม่ได้ กรุณาตั้งสถานะเป็น INACTIVE แทน";
            return RedirectToAction("UserList", new { UserId = UserId });
        }

        if (patient != null)
        {
            _db.Remove(patient);
        }
        if (doctor != null)
        {
            _db.Remove(doctor);
        }
        _db.Remove(target);
        _db.SaveChanges();

        TempData["Success"] = "ลบผู้ใช้เรียบร้อยแล้ว";
        return RedirectToAction("UserList", new { UserId = UserId });
    }

    // ---------------- โปรโมชั่น ----------------
    public IActionResult PromotionList(int UserId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        ViewBag.PromoList = (from p in _db.Promotions orderby p.PromotionId select p).ToList();
        ViewBag.UsedList = (from l in _db.AppointmentPromotions select l).ToList();
        return View();
    }

    [HttpGet]
    public IActionResult PromotionForm(int UserId, int? PromotionId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        PromotionFormViewModel vm = new PromotionFormViewModel();
        vm.AdminId = UserId;

        if (PromotionId != null)
        {
            var pm = (from p in _db.Promotions
                      where p.PromotionId == PromotionId
                      select p).FirstOrDefault();
            if (pm == null)
            {
                TempData["Error"] = "ไม่พบโปรโมชั่น";
                return RedirectToAction("PromotionList", new { UserId = UserId });
            }

            vm.PromotionId = pm.PromotionId;
            vm.PromotionName = pm.PromotionName;
            vm.PromotionType = pm.PromotionType;
            vm.DiscountPercent = pm.DiscountPercent;
            vm.MinimumAge = pm.MinimumAge;
            vm.MinimumPerson = pm.MinimumPerson;
            vm.StartDate = pm.StartDate;
            vm.EndDate = pm.EndDate;
            vm.Description = pm.Description;
            vm.Status = string.Equals(pm.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult PromotionForm(PromotionFormViewModel data)
    {
        if (!IsAdmin(data.AdminId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = data.AdminId;

        // ชนิดที่ระบบคำนวณส่วนลดรองรับ (ตรงกับ Calculate() ใน DoctorController)
        string[] types = { "AGE", "HEALTH_PACKAGE", "FAMILY", "WEB", "BIRTH_MONTH" };
        string msg = "";

        if (string.IsNullOrWhiteSpace(data.PromotionName))
        {
            msg = "กรุณากรอกชื่อโปรโมชั่น";
        }
        else if (!types.Contains(data.PromotionType))
        {
            msg = "กรุณาเลือกประเภทโปรโมชั่น";
        }
        else if (data.DiscountPercent <= 0 || data.DiscountPercent > 100)
        {
            msg = "เปอร์เซ็นต์ส่วนลดต้องมากกว่า 0 และไม่เกิน 100";
        }
        else if (data.PromotionType == "AGE" && (data.MinimumAge == null || data.MinimumAge < 1))
        {
            msg = "โปรโมชั่นผู้สูงอายุต้องระบุอายุขั้นต่ำ";
        }
        else if (data.PromotionType == "FAMILY" && (data.MinimumPerson == null || data.MinimumPerson < 2))
        {
            msg = "โปรโมชั่นครอบครัวต้องระบุจำนวนคนขั้นต่ำ (อย่างน้อย 2)";
        }
        else if (data.StartDate != null && data.EndDate != null && data.EndDate < data.StartDate)
        {
            msg = "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม";
        }

        if (msg != "")
        {
            ViewBag.Status = msg;
            return View(data);
        }

        Promotion? pm;
        if (data.PromotionId == 0)
        {
            pm = new Promotion();
            _db.Add(pm);
        }
        else
        {
            pm = (from p in _db.Promotions
                  where p.PromotionId == data.PromotionId
                  select p).FirstOrDefault();
            if (pm == null)
            {
                TempData["Error"] = "ไม่พบโปรโมชั่น";
                return RedirectToAction("PromotionList", new { UserId = data.AdminId });
            }
        }

        pm.PromotionName = data.PromotionName.Trim();
        pm.PromotionType = data.PromotionType;
        pm.DiscountPercent = data.DiscountPercent;
        pm.MinimumAge = (data.PromotionType == "AGE") ? data.MinimumAge : null;
        pm.MinimumPerson = (data.PromotionType == "FAMILY") ? data.MinimumPerson : null;
        pm.StartDate = data.StartDate;
        pm.EndDate = data.EndDate;
        pm.Description = data.Description;
        pm.Status = (data.Status == "INACTIVE") ? "INACTIVE" : "ACTIVE";
        _db.SaveChanges();

        TempData["Success"] = "บันทึกโปรโมชั่นเรียบร้อยแล้ว";
        return RedirectToAction("PromotionList", new { UserId = data.AdminId });
    }

    [HttpPost]
    public IActionResult PromotionToggle(int UserId, int PromotionId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var pm = (from p in _db.Promotions
                  where p.PromotionId == PromotionId
                  select p).FirstOrDefault();
        if (pm == null)
        {
            TempData["Error"] = "ไม่พบโปรโมชั่น";
            return RedirectToAction("PromotionList", new { UserId = UserId });
        }

        if (string.Equals(pm.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            pm.Status = "INACTIVE";
            TempData["Success"] = "ปิดใช้งานโปรโมชั่นแล้ว";
        }
        else
        {
            pm.Status = "ACTIVE";
            TempData["Success"] = "เปิดใช้งานโปรโมชั่นแล้ว";
        }
        _db.SaveChanges();
        return RedirectToAction("PromotionList", new { UserId = UserId });
    }

    [HttpPost]
    public IActionResult PromotionDelete(int UserId, int PromotionId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var pm = (from p in _db.Promotions
                  where p.PromotionId == PromotionId
                  select p).FirstOrDefault();
        if (pm == null)
        {
            TempData["Error"] = "ไม่พบโปรโมชั่น";
            return RedirectToAction("PromotionList", new { UserId = UserId });
        }

        int used = (from l in _db.AppointmentPromotions
                    where l.PromotionId == PromotionId
                    select l).Count();
        if (used > 0)
        {
            TempData["Error"] = "โปรโมชั่นนี้ถูกใช้ในนัดหมายแล้ว " + used + " รายการ ลบไม่ได้ กรุณาปิดใช้งานแทน";
            return RedirectToAction("PromotionList", new { UserId = UserId });
        }

        _db.Remove(pm);
        _db.SaveChanges();

        TempData["Success"] = "ลบโปรโมชั่นเรียบร้อยแล้ว";
        return RedirectToAction("PromotionList", new { UserId = UserId });
    }

    // ---------------- แผนก ----------------
    public IActionResult DepartmentList(int UserId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        ViewBag.DeptList = (from d in _db.Departments orderby d.DepartmentId select d).ToList();
        ViewBag.DoctorList = (from d in _db.Doctors select d).ToList();
        ViewBag.ServiceList = (from s in _db.Services select s).ToList();
        return View();
    }

    [HttpGet]
    public IActionResult DepartmentForm(int UserId, int? DepartmentId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        DepartmentFormViewModel vm = new DepartmentFormViewModel();
        vm.AdminId = UserId;

        if (DepartmentId != null)
        {
            var dp = (from d in _db.Departments
                      where d.DepartmentId == DepartmentId
                      select d).FirstOrDefault();
            if (dp == null)
            {
                TempData["Error"] = "ไม่พบแผนก";
                return RedirectToAction("DepartmentList", new { UserId = UserId });
            }
            vm.DepartmentId = dp.DepartmentId;
            vm.DepartmentName = dp.DepartmentName;
            vm.Description = dp.Description;
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult DepartmentForm(DepartmentFormViewModel data)
    {
        if (!IsAdmin(data.AdminId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = data.AdminId;

        if (string.IsNullOrWhiteSpace(data.DepartmentName))
        {
            ViewBag.Status = "กรุณากรอกชื่อแผนก";
            return View(data);
        }

        var dup = (from d in _db.Departments
                   where d.DepartmentName == data.DepartmentName.Trim() && d.DepartmentId != data.DepartmentId
                   select d).FirstOrDefault();
        if (dup != null)
        {
            ViewBag.Status = "มีแผนกชื่อนี้อยู่แล้ว";
            return View(data);
        }

        Department? dp;
        if (data.DepartmentId == 0)
        {
            dp = new Department();
            _db.Add(dp);
        }
        else
        {
            dp = (from d in _db.Departments
                  where d.DepartmentId == data.DepartmentId
                  select d).FirstOrDefault();
            if (dp == null)
            {
                TempData["Error"] = "ไม่พบแผนก";
                return RedirectToAction("DepartmentList", new { UserId = data.AdminId });
            }
        }

        dp.DepartmentName = data.DepartmentName.Trim();
        dp.Description = data.Description;
        _db.SaveChanges();

        TempData["Success"] = "บันทึกแผนกเรียบร้อยแล้ว";
        return RedirectToAction("DepartmentList", new { UserId = data.AdminId });
    }

    [HttpPost]
    public IActionResult DepartmentDelete(int UserId, int DepartmentId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var dp = (from d in _db.Departments
                  where d.DepartmentId == DepartmentId
                  select d).FirstOrDefault();
        if (dp == null)
        {
            TempData["Error"] = "ไม่พบแผนก";
            return RedirectToAction("DepartmentList", new { UserId = UserId });
        }

        int used = (from d in _db.Doctors where d.DepartmentId == DepartmentId select d).Count()
                 + (from s in _db.Services where s.DepartmentId == DepartmentId select s).Count();
        if (used > 0)
        {
            TempData["Error"] = "แผนกนี้ยังมีแพทย์หรือบริการอยู่ ลบไม่ได้";
            return RedirectToAction("DepartmentList", new { UserId = UserId });
        }

        _db.Remove(dp);
        _db.SaveChanges();

        TempData["Success"] = "ลบแผนกเรียบร้อยแล้ว";
        return RedirectToAction("DepartmentList", new { UserId = UserId });
    }

    // ---------------- บริการ ----------------
    public IActionResult ServiceList(int UserId, int? DepartmentId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        ViewBag.ServiceList = (from s in _db.Services
                               where DepartmentId == null || s.DepartmentId == DepartmentId
                               orderby s.DepartmentId, s.ServiceId
                               select s).ToList();
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();
        ViewBag.FilterDeptId = DepartmentId;
        return View();
    }

    [HttpGet]
    public IActionResult ServiceForm(int UserId, int? ServiceId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = UserId;
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();

        ServiceFormViewModel vm = new ServiceFormViewModel();
        vm.AdminId = UserId;

        if (ServiceId != null)
        {
            var sv = (from s in _db.Services
                      where s.ServiceId == ServiceId
                      select s).FirstOrDefault();
            if (sv == null)
            {
                TempData["Error"] = "ไม่พบบริการ";
                return RedirectToAction("ServiceList", new { UserId = UserId });
            }
            vm.ServiceId = sv.ServiceId;
            vm.DepartmentId = sv.DepartmentId;
            vm.ServiceName = sv.ServiceName;
            vm.Description = sv.Description;
            vm.Price = sv.Price;
            vm.ServiceType = sv.ServiceType ?? "CONSULT";
            vm.Status = string.Equals(sv.Status, "INACTIVE", StringComparison.OrdinalIgnoreCase) ? "INACTIVE" : "ACTIVE";
        }
        return View(vm);
    }

    [HttpPost]
    public IActionResult ServiceForm(ServiceFormViewModel data)
    {
        if (!IsAdmin(data.AdminId))
        {
            return RedirectToAction("Login", "Home");
        }

        ViewBag.UserId = data.AdminId;
        ViewBag.DeptList = (from d in _db.Departments select d).ToList();

        string[] types = { "CONSULT", "TREATMENT", "HEALTH_PACKAGE" };
        string msg = "";

        if ((from d in _db.Departments where d.DepartmentId == data.DepartmentId select d).FirstOrDefault() == null)
        {
            msg = "กรุณาเลือกแผนก";
        }
        else if (string.IsNullOrWhiteSpace(data.ServiceName))
        {
            msg = "กรุณากรอกชื่อบริการ";
        }
        else if (data.Price <= 0)
        {
            msg = "ราคาต้องมากกว่า 0";
        }
        else if (!types.Contains(data.ServiceType))
        {
            msg = "กรุณาเลือกประเภทบริการ";
        }

        if (msg != "")
        {
            ViewBag.Status = msg;
            return View(data);
        }

        Service? sv;
        if (data.ServiceId == 0)
        {
            sv = new Service();
            _db.Add(sv);
        }
        else
        {
            sv = (from s in _db.Services
                  where s.ServiceId == data.ServiceId
                  select s).FirstOrDefault();
            if (sv == null)
            {
                TempData["Error"] = "ไม่พบบริการ";
                return RedirectToAction("ServiceList", new { UserId = data.AdminId });
            }
        }

        sv.DepartmentId = data.DepartmentId;
        sv.ServiceName = data.ServiceName.Trim();
        sv.Description = data.Description;
        sv.Price = data.Price;
        sv.ServiceType = data.ServiceType;
        sv.Status = (data.Status == "INACTIVE") ? "INACTIVE" : "ACTIVE";
        _db.SaveChanges();

        TempData["Success"] = "บันทึกบริการเรียบร้อยแล้ว";
        return RedirectToAction("ServiceList", new { UserId = data.AdminId });
    }

    [HttpPost]
    public IActionResult ServiceDelete(int UserId, int ServiceId)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var sv = (from s in _db.Services
                  where s.ServiceId == ServiceId
                  select s).FirstOrDefault();
        if (sv == null)
        {
            TempData["Error"] = "ไม่พบบริการ";
            return RedirectToAction("ServiceList", new { UserId = UserId });
        }

        int used = (from l in _db.AppointmentServices
                    where l.ServiceId == ServiceId
                    select l).Count();
        if (used > 0)
        {
            TempData["Error"] = "บริการนี้ถูกใช้ในนัดหมายแล้ว ลบไม่ได้ กรุณาตั้งสถานะเป็น INACTIVE แทน";
            return RedirectToAction("ServiceList", new { UserId = UserId });
        }

        _db.Remove(sv);
        _db.SaveChanges();

        TempData["Success"] = "ลบบริการเรียบร้อยแล้ว";
        return RedirectToAction("ServiceList", new { UserId = UserId });
    }
    // ---------------- Dashboard ผู้ป่วยและการจอง ----------------
    public IActionResult Dashboard(int UserId, DateOnly? From, DateOnly? To)
    {
        if (!IsAdmin(UserId))
        {
            return RedirectToAction("Login", "Home");
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var fromDate = From ?? new DateOnly(today.Year, today.Month, 1);
        var toDate = To ?? today;

        if (toDate < fromDate)
        {
            ViewBag.Status = "วันที่สิ้นสุดต้องไม่ก่อนวันที่เริ่ม ระบบสลับวันที่ให้แล้ว";
            var tmp = fromDate;
            fromDate = toDate;
            toDate = tmp;
        }

        // การจองที่วันนัดอยู่ในช่วงที่เลือก
        var aps = (from a in _db.Appointments
                   where a.AppointmentDate >= fromDate && a.AppointmentDate <= toDate
                   orderby a.AppointmentDate
                   select a).ToList();

        // ผู้ป่วยที่สมัครในช่วงที่เลือก
        DateTime fromTime = fromDate.ToDateTime(TimeOnly.MinValue);
        DateTime toTime = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        int newPatients = (from p in _db.Patients
                           join u in _db.Users on p.UserId equals u.UserId
                           where u.CreatedAt >= fromTime && u.CreatedAt < toTime
                           select p).Count();

        ViewBag.UserId = UserId;
        ViewBag.From = fromDate;
        ViewBag.To = toDate;
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