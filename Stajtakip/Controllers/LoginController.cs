using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Stajtakip.Models.EntityFramework;

namespace StajTakip.Controllers
{
    public class LoginController : Controller
    {
        private StajTakipEntities db = new StajTakipEntities();

        // GET: Login
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult DefaultPage()
        {
            return View();
        }

        // GET: Login/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (string.IsNullOrEmpty(HttpContext.User.Identity.Name))
            {
                FormsAuthentication.SignOut();
                List<Semester> allSems = (from x in db.Semester where x.Approve == true select x).ToList(); //Biten semester ı kapatma
                for (int i = 0; i < allSems.Count; i++)
                {
                    if (allSems[i].EndDate <= dateTimeNow())
                    {
                        allSems[i].Approve = false;
                        db.SaveChanges();
                    }
                }
                clearSemester();
                return View();
            }

            return View();
        }
        public ActionResult CaptchaImage(string prefix, bool noisy = true)
        {
            int i, r, x, y;
            var rand = new Random((int)DateTime.Now.Ticks);
            int a = rand.Next(10, 99);
            int b = rand.Next(0, 9);
            var captcha = string.Format("{0} + {1} = ?", a, b);
            Session["Captcha"] = a + b;
            FileContentResult img = null;
            using (var mem = new MemoryStream())
            using (var bmp = new Bitmap(130, 30))
            using (var gfx = Graphics.FromImage((Image)bmp))
            {
                gfx.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                gfx.SmoothingMode = SmoothingMode.AntiAlias;
                gfx.FillRectangle(Brushes.DimGray, new Rectangle(0, 0, bmp.Width, bmp.Height));
                if (noisy)
                {
                    var pen = new Pen(Color.Yellow);
                    for (i = 1; i < 10; i++)
                    {
                        pen.Color = Color.FromArgb((rand.Next(0, 255)), (rand.Next(0, 255)), (rand.Next(0, 255)));
                        r = rand.Next(0, (130 / 3));
                        x = rand.Next(0, 130);
                        y = rand.Next(0, 30);
                    }
                }
                gfx.DrawString(captcha, new Font("Tahoma", 15), Brushes.White, 2, 3);
                bmp.Save(mem, System.Drawing.Imaging.ImageFormat.Jpeg);
                img = this.File(mem.GetBuffer(), "image/Jpeg");

            }
            return img;

        }
        public ActionResult SessionControl(string StudentEmail)
        {
           

            return View();
        }
      
        public ActionResult RedirectToAdmin()
        {
            if (Session["AdminTeacherEmail"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            string email = Session["AdminTeacherEmail"].ToString();
           

            AdminUserInfo adminInfo = (from x in db.AdminUserInfo where x.AdminEmail == email && x.Approve == true select x).SingleOrDefault();

            Session["AdminID"] = adminInfo.AdminID;
            Session["DepartmentID"] = adminInfo.DepartmentID;
            Session["EnteredAdmin"] = adminInfo.AdminName + " " + adminInfo.AdminSurname;
            Department thisDep = (from x in db.Department where x.DepartmentID == adminInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
            Semester startedSem = (from x in db.Semester where x.DepartmentID == adminInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
            Session["DepartmentName"] = thisDep.DepartmentName;

            if (startedSem != null)
            {
                return RedirectToAction("AdminMainPage", "AdminMain");
            }
            else
            {
                return RedirectToAction("StartSemester", "AdminMain");
            }
        }
        
        public ActionResult RedirectToTeacher()
        {
            if (Session["AdminTeacherEmail"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            string email = Session["AdminTeacherEmail"].ToString();
           
            TeacherUserInfo teacherInfo = (from x in db.TeacherUserInfo where x.TeacherEmail == email && x.Approve == true select x).SingleOrDefault();

            Session["TeacherID"] = teacherInfo.TeacherID;
            Session["EnteredTeacher"] = teacherInfo.TeacherName + " " + teacherInfo.TeacherSurname;
            Session["DepartmentID"] = teacherInfo.DepartmentID;
            Session["TeacherName"] = teacherInfo.TeacherName;
            Session["TeacherSurname"] = teacherInfo.TeacherSurname;
            Session["councilID"] = teacherInfo.CouncilID;
            Department thisDep = (from x in db.Department where x.DepartmentID == teacherInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
            Session["DepartmentName"] = thisDep.DepartmentName;

            //sömestr kontrolü yapıyor hocaya
            TeacherUserInfo enteredTeacher = (from x in db.TeacherUserInfo where x.Approve == true && x.TeacherEmail == email select x).SingleOrDefault();
            if (enteredTeacher != null)
            {
                Semester thisTchSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == enteredTeacher.DepartmentID select x).SingleOrDefault();

                if (thisTchSemester == null)
                {
                    ViewBag.finishSemesterForTch = true;
                    ViewBag.finishSemesterForTchError = "The semester has not started yet !";
                    return View("Login");
                }
            }

            return RedirectToAction("TeacherMainPage", "TeacherMain");
        }
        [HttpPost]
        public ActionResult CaptchaImage(string StudentEmail, string StudentPassword, StudentUserInfo s, string Captcha)
        {
            if (Session["Captcha"] == null || Session["Captcha"].ToString() != Captcha)
            {
                //hata mesajı
                return RedirectToAction("./Login");
            }
            else
            {

                using (StajTakipEntities db = new StajTakipEntities())
                {
                    string encryptedPassword = MD5Hash(StudentPassword);
                    var studentInfo = db.StudentUserInfo.Where(x => x.StudentEmail == StudentEmail && x.StudentPassword == encryptedPassword && x.Approve == true).FirstOrDefault();
                    var adminInfo = db.AdminUserInfo.Where(x => x.AdminEmail == StudentEmail && x.AdminPassword == encryptedPassword && x.Approve == true).FirstOrDefault();
                    var teacherInfo = db.TeacherUserInfo.Where(x => x.TeacherEmail == StudentEmail && x.TeacherPassword == encryptedPassword && x.Approve == true).FirstOrDefault();
                    if (studentInfo == null && adminInfo == null && teacherInfo == null)
                    {
                        ViewBag.Error = "Email or Password is not correct!";
                        ViewBag.control = true;
                        return View("Login");
                    }
                    else if (teacherInfo != null && adminInfo != null && studentInfo == null)
                    {
                        Session["AdminTeacherEmail"] = adminInfo.AdminEmail;
                        return RedirectToAction("SessionControl");
                    }
                    else if (studentInfo != null)
                    {
                        ViewBag.control = false;
                        Session["SchoolID"] = studentInfo.StudentSchoolID;
                        Session["Name"] = studentInfo.StudentName.ToString() + " " + studentInfo.StudentSurname.ToString();
                        Session["StudentID"] = studentInfo.StudentID;
                        Session["DepartmentID"] = studentInfo.DepartmentID;
                        Session["Name1"] = studentInfo.StudentName.ToString();
                        Session["Surname"] = studentInfo.StudentSurname.ToString();
                        Session["councilID"] = studentInfo.CouncilID;
                        Department thisDep = (from x in db.Department where x.DepartmentID == studentInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
                        Session["DepartmentName"] = thisDep.DepartmentName;

                        StudentUserInfo enteredStudent = (from x in db.StudentUserInfo where x.Approve == true && x.StudentEmail == StudentEmail && x.StudentPassword == encryptedPassword select x).SingleOrDefault();
                        if (enteredStudent != null)
                        {
                            if (enteredStudent.SemesterID == null)
                            {
                                ViewBag.finishSemesterForTch = true;
                                ViewBag.finishSemesterForTchError = "The semester has not started yet !";
                                return View("Login");
                            }
                        }

                        return RedirectToAction("MainPage", "StudentMain");
                    }
                    else if (adminInfo != null)
                    {

                        Session["AdminID"] = adminInfo.AdminID;
                        Session["DepartmentID"] = adminInfo.DepartmentID;
                        Session["EnteredAdmin"] = adminInfo.AdminName + " " + adminInfo.AdminSurname;
                        Department thisDep = (from x in db.Department where x.DepartmentID == adminInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
                        Semester startedSem = (from x in db.Semester where x.DepartmentID == adminInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
                        Session["DepartmentName"] = thisDep.DepartmentName;

                        if (startedSem != null)
                        {
                            return RedirectToAction("AdminMainPage", "AdminMain");
                        }
                        else
                        {
                            return RedirectToAction("StartSemester", "AdminMain");
                        }

                    }
                    else if (teacherInfo != null)
                    {
                        Session["TeacherID"] = teacherInfo.TeacherID;
                        Session["EnteredTeacher"] = teacherInfo.TeacherName + " " + teacherInfo.TeacherSurname;
                        Session["DepartmentID"] = teacherInfo.DepartmentID;
                        Session["TeacherName"] = teacherInfo.TeacherName;
                        Session["TeacherSurname"] = teacherInfo.TeacherSurname;
                        Session["councilID"] = teacherInfo.CouncilID;
                        Department thisDep = (from x in db.Department where x.DepartmentID == teacherInfo.DepartmentID && x.Approve == true select x).SingleOrDefault();
                        Session["DepartmentName"] = thisDep.DepartmentName;

                        //sömestr kontrolü yapıyor hocaya
                        TeacherUserInfo enteredTeacher = (from x in db.TeacherUserInfo where x.Approve == true && x.TeacherEmail == StudentEmail && x.TeacherPassword == encryptedPassword select x).SingleOrDefault();
                        if (enteredTeacher != null)
                        {
                            Semester thisTchSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == enteredTeacher.DepartmentID select x).SingleOrDefault();

                            if (thisTchSemester == null)
                            {
                                ViewBag.finishSemesterForTch = true;
                                ViewBag.finishSemesterForTchError = "The semester has not started yet !";
                                return View("Login");
                            }
                        }

                        return RedirectToAction("TeacherMainPage", "TeacherMain");
                    }
                }
                return RedirectToAction("./Login");
            }
        }
        //[AllowAnonymous]
        //Bu fonk. yaptığı işi CaptchaImage üstlendi
        //[HttpPost]
        //public ActionResult Login(string StudentEmail,string StudentPassword,bool? Benihatirla)
        //{
        //    using (DBDataContext db = new DBDataContext())
        //    {

        //        var studentInfo = db.StudentUserInfo.Where(x => x.StudentEmail == StudentEmail && x.StudentPassword == StudentPassword && x.Approve==true).FirstOrDefault();
        //        var adminInfo = db.AdminUserInfo.Where(x => x.AdminEmail == StudentEmail && x.AdminPassword == StudentPassword && x.Approve == true).FirstOrDefault();
        //        var teacherInfo = db.TeacherUserInfo.Where(x => x.TeacherEmail == StudentEmail && x.TeacherPassword == StudentPassword && x.Approve == true).FirstOrDefault();
        //        if (studentInfo== null && adminInfo == null && teacherInfo==null)
        //        {
        //            ViewBag.Error = "Wrong ID or Password!";
        //            ViewBag.control = true;
        //        }
        //        else if(studentInfo!=null)
        //        {
        //            ViewBag.control = false;
        //            Session["Name"] = studentInfo.StudentName.ToString() +" "+ studentInfo.StudentSurname.ToString();
        //            Session["StudentID"] = studentInfo.StudentID;
        //            Session["DepartmentID"] = studentInfo.DepartmentID;
        //            Session["FacultyID"] = studentInfo.FacultyID;
        //            Session["Name1"] = studentInfo.StudentName.ToString();
        //            Session["Surname"] = studentInfo.StudentSurname.ToString();
        //            Session["councilID"] = studentInfo.CouncilID;

        //            return RedirectToAction("MainPage", "StudentMain");                    
        //        }
        //        else if (adminInfo!=null)
        //        {
        //            Session["AdminID"] = adminInfo.AdminID;
        //            Session["EnteredAdmin"] = adminInfo.AdminName + " " + adminInfo.AdminSurname;
        //            return RedirectToAction("AdminMainPage", "AdminMain");
        //        }
        //        else if (teacherInfo != null)
        //        {
        //            Session["TeacherID"] = teacherInfo.TeacherID;
        //            Session["EnteredTeacher"] = teacherInfo.TeacherName + " " + teacherInfo.TeacherSurname;
        //            Session["DepartmentID"] = teacherInfo.DepartmentID;
        //            Session["councilID"] = teacherInfo.CouncilID;
        //            return RedirectToAction("TeacherMainPage", "TeacherMain");
        //        }
        //        //if (Benihatirla == true)
        //        //{
        //        //    HttpCookie cerez = new HttpCookie("cerezim");
        //        //    cerez.Values.Add("eposta", StudentEmail);
        //        //    cerez.Values.Add("sifre", StudentPassword);
        //        //    cerez.Expires = DateTime.Now.AddDays(30);
        //        //    Response.Cookies.Add(cerez);
        //        //}
        //    }
        //    return View();
        //}


        // GET: Login/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Login/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Login/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Login/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Login/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Login/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //DÜZ METHODS
        public static DateTime dateTimeNow()
        {
            var myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.microsoft.com");
            var response = myHttpWebRequest.GetResponse();
            string todaysDates = response.Headers["date"];
            return DateTime.ParseExact(todaysDates,
                                       "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                       CultureInfo.InvariantCulture.DateTimeFormat,
                                       DateTimeStyles.AssumeUniversal);
        }
        public ActionResult RegisterFacultyStep()
        {
            List<SelectListItem> facultyList = new List<SelectListItem>();
            foreach (var item in (from x in db.Faculty where x.Approve == true select x).ToList())
            {
                facultyList.Add(new SelectListItem { Text = item.FacultyName, Value = item.FacultyID.ToString() });
            }
            ViewBag.ddlFaculty = facultyList;
            return View();
        }
        [HttpPost]
        public ActionResult RegisterFacultyStep(int? FacultyID)
        {

            if (FacultyID == null)
            {
                List<SelectListItem> facultyList = new List<SelectListItem>();
                foreach (var item in (from x in db.Faculty where x.Approve == true select x).ToList())
                {
                    facultyList.Add(new SelectListItem { Text = item.FacultyName, Value = item.FacultyID.ToString() });
                }
                ViewBag.ddlFaculty = facultyList;

                ViewBag.FacError = true;
                ViewBag.FacMessage = "Please Select a Faculty!";

                return View();
            }
            else
            {
                int? ddlFacultyID = FacultyID;
                Session["selectedFaculty"] = ddlFacultyID;

            }


            return RedirectToAction("RegisterDepartmentStep");

        }
        public ActionResult RegisterDepartmentStep()
        {


            int FacultyID = Convert.ToInt32(Session["selectedFaculty"]);
            List<SelectListItem> departmentList = new List<SelectListItem>();
            foreach (var item in (from x in db.Department where x.FacultyID == FacultyID select x).ToList())
            {
                if (item.Approve == true)
                {
                    departmentList.Add(new SelectListItem { Text = item.DepartmentName, Value = item.DepartmentID.ToString() });

                }
            }
            ViewBag.ddlDepartment = departmentList;

            return View();
        }
        [HttpPost]
        public ActionResult RegisterDepartmentStep(int? DepartmentID)
        {
            if (DepartmentID == null)
            {
                int FacultyID = Convert.ToInt32(Session["selectedFaculty"]);
                List<SelectListItem> departmentList = new List<SelectListItem>();
                foreach (var item in (from x in db.Department where x.FacultyID == FacultyID select x).ToList())
                {
                    if (item.Approve == true)
                    {
                        departmentList.Add(new SelectListItem { Text = item.DepartmentName, Value = item.DepartmentID.ToString() });

                    }
                }
                ViewBag.ddlDepartment = departmentList;
                ViewBag.DepartmentControl = true;

                return View();
            }
            int? ddlDepartmentID = DepartmentID;
            Session["selectedDepartment"] = ddlDepartmentID;
            return RedirectToAction("Register");
        }
        public string GeneratePassword()
        {
            string PasswordLength = "8";
            string NewPassword = "";

            string allowedChars = "";
            allowedChars = "1,2,3,4,5,6,7,8,9,0";
            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";
            allowedChars += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z";
            allowedChars += ".,?";

            char[] sep = { ',' };
            string[] arr = allowedChars.Split(sep);
            string IDString = "";
            string temp = "";
            Random rand = new Random();
            for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)
            {
                temp = arr[rand.Next(0, arr.Length)];
                IDString += temp;
                NewPassword = IDString;
            }
            return NewPassword;
        }
        //public string ConfirmationCode()
        //{
        //    string PasswordLength = "5";
        //    string NewPassword = "";

        //    string allowedChars = "";
        //    allowedChars = "1,2,3,4,5,6,7,8,9,0";
        //    allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";
        //    allowedChars += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";

        //    char[] sep = { ',' };
        //    string[] arr = allowedChars.Split(sep);
        //    string IDString = "";
        //    string temp = "";
        //    Random rand = new Random();
        //    for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)
        //    {
        //        temp = arr[rand.Next(0, arr.Length)];
        //        IDString += temp;
        //        NewPassword = IDString;
        //    }
        //    return NewPassword;
        //}
        [HttpPost]
        public ActionResult Register(StudentUserInfo s)
        {
            if (s.StudentName == null || s.StudentSurname == null || s.StudentSchoolID == null || s.StudentPhoneNumber == null || s.StudentEmail == null)
            {
                ViewBag.RegisterControlFillAreas = true;
            }
            else
            {

                if (ModelState.IsValid)
                {
                    string email = s.StudentEmail;
                    string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.StudentName);
                    string surname = s.StudentSurname.ToUpper();
                    int departmentID = Convert.ToInt32(Session["selectedDepartment"]);
                    Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == departmentID select x).SingleOrDefault();
                    if (thisSemester != null)
                    {
                        Session["thisSemes"] = thisSemester.SemesterID;
                    }


                    var existingStudent = (from x in db.StudentUserInfo where x.StudentEmail == email && x.Approve == true select x).SingleOrDefault();
                    if (existingStudent == null)
                    {

                        string newPass = GeneratePassword();
                        StudentUserInfo newStd = new StudentUserInfo();
                        newStd.StudentName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.StudentName);
                        newStd.StudentSurname = s.StudentSurname.ToUpper();
                        newStd.StudentEmail = s.StudentEmail;
                        newStd.StudentSchoolID = s.StudentSchoolID;
                        newStd.StudentPassword = MD5Hash(newPass);
                        newStd.StudentPhoneNumber = s.StudentPhoneNumber;
                        //newStd.FacultyID = Convert.ToInt32(Session["selectedFaculty"]);
                        newStd.DepartmentID = Convert.ToInt32(Session["selectedDepartment"]);
                        if (thisSemester != null)
                        {
                            newStd.SemesterID = Convert.ToInt32(Session["thisSemes"]);
                            db.SaveChanges();
                        }
                        else if (thisSemester == null)
                        {
                            newStd.SemesterID = null;
                            db.SaveChanges();
                        }

                        newStd.Approve = true;
                        db.StudentUserInfo.Add(newStd);
                        db.SaveChanges();


                        var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                        var toAddress = new MailAddress(email);
                        const string subject = "Welcome To HKU INTERNSHIP PROGRAM";

                        string body = "Dear, " + name + " " + surname + ";" + "\n\nWelcome to internship system." + "Your account has been successfully created.Now you can sign in with your Email and Password.\n\nYour log-in email is :" + email + "\n\nYour temporary password is : " + newPass + ""
                                    + "\n\nPlease change it as soon as possible." + "\n\n" + "http://mfstaj.hku.edu.tr/";
                        using (var smtp = new SmtpClient
                        {
                            Host = "smtp.gmail.com",
                            Port = 587,
                            EnableSsl = true,
                            DeliveryMethod = SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential(fromAddress.Address, "7ju&XYYR")
                        })
                        {
                            using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
                            {
                                smtp.Send(message);
                            }
                        }

                        ViewBag.RegisterControl = true;
                        ModelState.Clear();
                    }
                    else
                    {
                        ViewBag.AlreadyInSystem = false;
                    }
                }
                else
                {
                    ViewBag.RegisterControl = false;
                }
            }

            return View();
        }
        public ActionResult Register()
        {
            Session["RegisterError"] = "";
            Session["depError"] = "";
            int faculty = Convert.ToInt32(Session["selectedFaculty"]);
            Faculty selectedFac = (from x in db.Faculty where x.FacultyID == faculty && x.Approve == true select x).SingleOrDefault();
            if (selectedFac != null)
            {
                string facName = selectedFac.FacultyName;
                Session["facultyName"] = facName;
                int department = Convert.ToInt32(Session["selectedDepartment"]);
                Department selectedDep = (from x in db.Department where x.DepartmentID == department select x).SingleOrDefault();
                string depName = selectedDep.DepartmentName;
                Session["depName"] = depName;
            }
            return View();
        }
        public void clearSemester()
        {

            List<StudentUserInfo> willBeDeletedStd = (from x in db.StudentUserInfo where x.Approve == true && x.SemesterID != null select x).ToList();
            List<StudentOverallGrade> allStudentOverallGrades = (from x in db.StudentOverallGrade where x.Approve == true select x).ToList();
            List<StudentGrade> allStudentGrades = (from x in db.StudentGrade where x.Approve == true select x).ToList();
            List<InternInfo> allInternInfos = (from x in db.InternInfo where x.Approve == true select x).ToList();
            List<GradeTypeAvarage> allGradeTypeAvarages = (from x in db.GradeTypeAvarage where x.Approve == true select x).ToList();
            List<GradeCategory> allGradeCategories = (from x in db.GradeCategory where x.Approve == true select x).ToList();
            List<Document> allDocuments = (from x in db.Document where x.Approve == true select x).ToList();
            List<Council> allCouncils = (from x in db.Council where x.Approve == true select x).ToList();
            List<Comment> allComments = (from x in db.Comment where x.Approve == true select x).ToList();
            List<TeacherUserInfo> allTeachers = (from x in db.TeacherUserInfo where x.Approve == true select x).ToList();
            List<RejectedApplications> allRejectedApp = (from x in db.RejectedApplications where x.Approve == true select x).ToList();

            for (int i = 0; i < willBeDeletedStd.Count; i++)
            {
                if (willBeDeletedStd != null)
                {
                    int? tempData = willBeDeletedStd[i].DepartmentID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.DepartmentID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        willBeDeletedStd[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allStudentOverallGrades.Count; i++)
            {
                if (allStudentOverallGrades != null)
                {
                    int? tempData = allStudentOverallGrades[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allStudentOverallGrades[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allStudentGrades.Count; i++)
            {
                if (allStudentGrades != null)
                {
                    int? tempData = allStudentGrades[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allStudentGrades[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allInternInfos.Count; i++)
            {
                if (allInternInfos != null)
                {
                    int? tempData = allInternInfos[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allInternInfos[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allGradeTypeAvarages.Count; i++)
            {
                if (allGradeTypeAvarages != null)
                {
                    int? tempData = allGradeTypeAvarages[i].DepartmentID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.DepartmentID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allGradeTypeAvarages[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allGradeCategories.Count; i++)
            {
                if (allGradeCategories != null)
                {
                    int? tempData = allGradeCategories[i].DepartmentID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.DepartmentID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allGradeCategories[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allDocuments.Count; i++)
            {
                if (allDocuments != null)
                {
                    int? tempData = allDocuments[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allDocuments[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allCouncils.Count; i++)
            {
                if (allCouncils != null)
                {
                    int? tempData = allCouncils[i].DepartmentID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.DepartmentID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allCouncils[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allComments.Count; i++)
            {
                if (allComments != null)
                {
                    int? tempData = allComments[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allComments[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allTeachers.Count; i++)
            {
                if (allTeachers != null)
                {
                    int? tempData = allTeachers[i].DepartmentID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.DepartmentID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allTeachers[i].CouncilID = null;
                        db.SaveChanges();
                    }
                }
            }
            for (int i = 0; i < allRejectedApp.Count; i++)
            {
                if (allRejectedApp != null)
                {
                    int? tempData = allRejectedApp[i].SemesterID;
                    Semester willBeDeletedSem = (from x in db.Semester where x.SemesterID == tempData && x.Approve == true select x).SingleOrDefault();
                    if (willBeDeletedSem == null)
                    {
                        allRejectedApp[i].Approve = false;
                        db.SaveChanges();
                    }
                }
            }

        }
        [HttpPost]
        public string ActivationCode()
        {
            string PasswordLength = "5";
            string NewPassword = "";

            string allowedChars = "";
            allowedChars = "1,2,3,4,5,6,7,8,9,0";
            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";
            allowedChars += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";

            char[] sep = { ',' };
            string[] arr = allowedChars.Split(sep);
            string IDString = "";
            string temp = "";
            Random rand = new Random();
            for (int i = 0; i < Convert.ToInt32(PasswordLength); i++)
            {
                temp = arr[rand.Next(0, arr.Length)];
                IDString += temp;
                NewPassword = IDString;
            }
            return NewPassword;
        }
        [HttpPost]
        public ActionResult ResetPassword(string Checkmail)
        {
            if (Checkmail == null)
            {
                ViewBag.mustFillError = true;
            }

            int controlExtensionUntillEt = Checkmail.IndexOf('@');
            string lastPositionMail = Checkmail.Substring(controlExtensionUntillEt + 1);

            if (lastPositionMail == "hku.edu.tr" || lastPositionMail == "std.hku.edu.tr")
            {
                string Activationcode = ActivationCode();
                AdminUserInfo admin = (from x in db.AdminUserInfo where x.AdminEmail == Checkmail && x.Approve == true select x).SingleOrDefault();

                StudentUserInfo std = (from x in db.StudentUserInfo where x.StudentEmail == Checkmail && x.Approve == true select x).SingleOrDefault();

                TeacherUserInfo teacher = (from x in db.TeacherUserInfo where x.TeacherEmail == Checkmail && x.Approve == true select x).SingleOrDefault();


                if (admin != null)
                {
                    Session["Activationcode"] = Activationcode;
                    Session["thisAdminMail"] = admin.AdminEmail;
                    admin.AdminPassword = Activationcode;
                    db.SaveChanges();
                    var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                    var toAddress = new MailAddress(admin.AdminEmail);
                    const string subject = "Reset Password";

                    string body = "Dear Admin, " + admin.AdminName + " " + admin.AdminSurname + ";" + "\n\nYour activation code is : " + Session["Activationcode"]
                                + "\n\nPlease enter this code to 'Confirmation Code' section" + "\n\n" + "http://mfstaj.hku.edu.tr/Login/NewPasswordPage";
                    using (var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, "7ju&XYYR")
                    })
                    {
                        using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
                        {
                            smtp.Send(message);
                        }
                        Session["success"] = "true";
                        Session["successMessage"] = "Successful! Your activation code has been send your email.";
                        ModelState.Clear();
                        return RedirectToAction("NewPasswordPage");


                    }

                }
                if (std != null)
                {
                    Session["Activationcode"] = Activationcode;
                    Session["thisStdEmail"] = std.StudentEmail;

                    var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                    var toAddress = new MailAddress(std.StudentEmail);
                    const string subject = "HKU INTERNSHIP PROGRAM - Reset Password";

                    string body = "Dear Student, " + std.StudentName + " " + std.StudentSurname + ";" + "\n\nYour activiton code : " + Session["Activationcode"] + "\n\nPlease enter this code to 'Confirmation Code' section" +
                                 "\n\nPlease go to http://mfstaj.hku.edu.tr/Login/NewPasswordPage this page. ";
                    using (var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, "7ju&XYYR")
                    })
                    {
                        using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
                        {
                            smtp.Send(message);
                        }



                        Session["success"] = "true";
                        Session["successMessage"] = "Your activation code has been send your email.";
                        ModelState.Clear();
                        return RedirectToAction("NewPasswordPage");



                    }
                }
                if (teacher != null)
                {
                    Session["Activationcode"] = Activationcode;
                    Session["thisTeacherMail"] = teacher.TeacherEmail;
                    var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                    var toAddress = new MailAddress(teacher.TeacherEmail);
                    const string subject = "HKU INTERNSHIP PROGRAM - Reset Password";

                    string body = "Dear Lecturer, " + teacher.TeacherName + " " + teacher.TeacherSurname + ";" + "\n\nYour activation code is : " + Session["Activationcode"]
                                + "\n\nPlease enter this code to 'Confirmation Code' section" + "\n\n" + "http://mfstaj.hku.edu.tr/Login/NewPasswordPage";
                    using (var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, "7ju&XYYR")
                    })
                    {
                        using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
                        {
                            smtp.Send(message);
                        }
                        Session["success"] = "true";
                        Session["successMessage"] = "Successful! Your activation code has been send your email.";
                        ModelState.Clear();
                        return RedirectToAction("NewPasswordPage");


                    }

                }

                if (std == null && teacher == null && admin == null)
                {
                    ViewBag.NotRegisteredError = true;
                }
            }
            else
            {
                ViewBag.othermailtype = true;
            }
            return View();
        }
        public ActionResult ResetPassword()
        {
            return View();
        }

        


        [HttpPost]
        public ActionResult NewPasswordPage(string ConfirmationCode, string NewPassword, string ConfirmPassword)
        {
            string ProducedConfirmationCode = Session["Activationcode"].ToString();
            if (ProducedConfirmationCode != ConfirmationCode)
            {
                ViewBag.ProducedConfirmationCodeError = true;
            }
            else
            {
                string Email = null;
                string WhoamI = null;
                string ControlMail = null;

                if (Session["thisAdminMail"] != null)
                {
                    ControlMail = Session["thisAdminMail"].ToString();
                    Email = ControlMail;
                }
                if (Session["thisStdEmail"] != null)
                {
                    ControlMail = Session["thisStdEmail"].ToString();
                    Email = ControlMail;
                }
                if (Session["thisTeacherMail"] != null)
                {
                    ControlMail = Session["thisTeacherMail"].ToString();
                    Email = ControlMail;
                }



                AdminUserInfo isAdmin = (from x in db.AdminUserInfo where x.AdminEmail == ControlMail && x.Approve == true select x).SingleOrDefault();
                StudentUserInfo isStudent = (from x in db.StudentUserInfo where x.StudentEmail == ControlMail && x.Approve == true select x).SingleOrDefault();
                TeacherUserInfo isTeacher = (from x in db.TeacherUserInfo where x.TeacherEmail == ControlMail && x.Approve == true select x).SingleOrDefault();
                if (isAdmin != null)
                {

                    WhoamI = "Admin, " + isAdmin.AdminName + " " + isAdmin.AdminSurname;
                    if (NewPassword != "" && ConfirmPassword != "" && ConfirmationCode != "")
                    {
                        if (NewPassword == ConfirmPassword && ModelState.IsValid)
                        {
                            if (ValidatePassword(NewPassword) == true && checkIfSpace(NewPassword) == true)
                            {
                                isAdmin.AdminPassword = MD5Hash(NewPassword);
                                db.SaveChanges();
                                ViewBag.success = true;
                            }
                            else
                            {
                                ViewBag.ValidatePassError = true;
                            }

                        }
                        else
                        {
                            ViewBag.NotMatchNPCPError = true;
                        }
                    }
                    else
                    {
                        ViewBag.EmptyFieldsError = true;
                    }

                }
                if (isStudent != null)
                {
                    WhoamI = "Student, " + isStudent.StudentName + " " + isStudent.StudentSurname;
                    if (NewPassword != "" && ConfirmPassword != "" && ConfirmationCode != "")
                    {
                        if (NewPassword == ConfirmPassword && ModelState.IsValid)
                        {
                            if (ValidatePassword(NewPassword) == true && checkIfSpace(NewPassword) == true)
                            {
                                isStudent.StudentPassword = MD5Hash(NewPassword);
                                db.SaveChanges();
                                ViewBag.success = true;
                            }
                            else
                            {
                                ViewBag.ValidatePassError = true;
                            }

                        }
                        else
                        {
                            ViewBag.NotMatchNPCPError = true;
                        }
                    }
                    else
                    {
                        ViewBag.EmptyFieldsError = true;
                    }
                }
                if (isTeacher != null)
                {
                    WhoamI = "Lecturer, " + isTeacher.TeacherName + " " + isTeacher.TeacherSurname;
                    if (NewPassword != "" && ConfirmPassword != "" && ConfirmationCode != "")
                    {
                        if (NewPassword == ConfirmPassword && ModelState.IsValid)
                        {
                            if (ValidatePassword(NewPassword) == true && checkIfSpace(NewPassword) == true)
                            {

                                isTeacher.TeacherPassword = MD5Hash(NewPassword);
                                db.SaveChanges();
                                ViewBag.success = true;


                            }
                            else
                            {
                                ViewBag.ValidatePassError = true;
                            }

                        }
                        else
                        {
                            ViewBag.NotMatchNPCPError = true;
                        }
                    }
                    else
                    {
                        ViewBag.EmptyFieldsError = true;
                    }
                }

                var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                var toAddress = new MailAddress(Email);
                const string subject = "HKU INTERNSHIP PROGRAM";

                string body = "Dear, " + WhoamI + ";" + "\n\nYour password has been changed "
                            + "\n\n http://mfstaj.hku.edu.tr/ ";
                using (var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, "7ju&XYYR")
                })
                {
                    using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
                    {
                        smtp.Send(message);
                    }

                }

            }
            ModelState.Clear();
            return View();
        }



        public ActionResult NewPasswordPage()
        {

            return View();
        }
        public static string MD5Hash(string text)
        {
            MD5 md5 = new MD5CryptoServiceProvider();

            //metnin boyutuna göre hash hesaplar
            md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(text));

            //hesapladıktan sonra hashi alır
            byte[] result = md5.Hash;

            StringBuilder strBuilder = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                //her baytı 2 hexadecimal hane olarak değiştirir
                strBuilder.Append(result[i].ToString("x2"));
            }

            return strBuilder.ToString();
        }
        private bool ValidatePassword(string password)
        {
            var input = password;


            //if (string.IsNullOrWhiteSpace(input))
            //{
            //    throw new Exception("Password should not be empty");
            //}

            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMiniMaxChars = new Regex(@".{6,15}");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

            if (!hasLowerChar.IsMatch(input))
            {

                ViewBag.PassForm = false;
                return false;
            }
            else if (!hasUpperChar.IsMatch(input))
            {

                ViewBag.PassForm = false;
                return false;
            }
            else if (!hasMiniMaxChars.IsMatch(input))
            {

                ViewBag.PassForm = false;
                return false;
            }
            else if (!hasNumber.IsMatch(input))
            {

                ViewBag.PassForm = false;
                return false;
            }

            else if (!hasSymbols.IsMatch(input))
            {

                ViewBag.PassForm = false;
                return false;
            }
            else
            {
                ViewBag.PassForm = true;
                return true;
            }
        }
        public bool checkIfSpace(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    return false;
                }
            }

            return true;
        }

    }
}
