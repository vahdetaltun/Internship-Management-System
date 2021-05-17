using Stajtakip.Models.EntityFramework;
using StajTakip.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace StajTakip.Controllers
{
    public class StudentMainController : Controller
    {
        private StajTakipEntities db = new StajTakipEntities();

        // GET: StudentMain
        //GET Methods
        public ActionResult NotFoundStd()
        {
            return View();
        }
        public ActionResult NotFoundStdWOButton()
        {
            return View();
        }
        public ActionResult MainPage()
        {
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            openFileuploadDatePicker();
            clearGrade();
            clearUnsaved();
            int stdID = Convert.ToInt32(Session["StudentID"]);
            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            //int mesID = Convert.ToInt32(Session["message1"]);
            //int mesID2 = Convert.ToInt32(Session["message2"]);
            //int mesID3 = Convert.ToInt32(Session["message3"]);
            var existingStudent = db.StudentUserInfo.Where(s => s.StudentID == stdID && s.Approve == true).FirstOrDefault();
            var faculty = db.Faculty.Where(f => f.FacultyID == facID).FirstOrDefault();
            var department = db.Department.Where(d => d.DepartmentID == depID).FirstOrDefault();
            string depinfo = department.DepartmentName.ToString();
            Session["DepAndFac"] = depinfo;

            StudentUserInfo thisstd = (from x in db.StudentUserInfo where x.StudentID == stdID && x.DepartmentID == depID && x.Approve == true select x).SingleOrDefault();
            if (thisstd != null)
            {
                Semester isEndInternship = (from x in db.Semester where x.Approve == true && x.DepartmentID == thisstd.DepartmentID select x).SingleOrDefault();
                if (isEndInternship != null)
                {
                    string date = (isEndInternship.EndDateInternship).ToString().Substring(0, 10);
                    ViewBag.InternLastDate = date;
                    ViewBag.Semestername = isEndInternship.SemesterName;
                }

                int SemID = semester();
                //staj durumu ne alemde
                InternInfo InternStatusNow = (from x in db.InternInfo where x.StudentID == stdID && x.SemesterID == SemID select x).SingleOrDefault();
                if (InternStatusNow != null)
                {
                    if (InternStatusNow.Approve == false && InternStatusNow.isDelete == false)
                    {
                        //staj beklemede - admin onay vermedi 
                        ViewBag.internStatusNow = "Pending";
                    }
                    else if (InternStatusNow.Approve == false && InternStatusNow.isDelete == true)
                    {
                        //staj reject
                        ViewBag.internStatusNow = "Rejected";
                    }
                    else if (InternStatusNow.Approve == true && InternStatusNow.isDelete == false)
                    {
                        //staj onaylandı
                        ViewBag.internStatusNow = "Confirmed";
                    }

                }
                else
                {
                    //hiçbir şey
                    ViewBag.internStatusNow = "-";
                }
                Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                List<StudentUserInfo> CountofStd = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
                Department thisDepartment = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();

                string fileName = thisDepartment.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year.ToString() + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString() + ".pdf";

                string embed = "<object data=\"{0}\" type=\"application/pdf\" width=\"100%\" height=\"100%\">";
                embed += "If you are unable to view file, you can download from <a href = \"{0}\">here</a>";
                embed += " or download <a target = \"_blank\" href = \"http://get.adobe.com/reader/\">Adobe PDF Reader</a> to view the file.";
                embed += "</object>";

                ViewBag.Embed = string.Format(embed, VirtualPathUtility.ToAbsolute("~/Internship Calendar/" + fileName));
            }

            return View();
        }

        public ActionResult AddInternInfoAbroad()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            int id = Convert.ToInt32(Session["StudentID"]);

            if (id == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty");
                return RedirectToAction("NotFoundStd");
            }

            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();

            string surname = Session["Surname"].ToString();
            var existingIntern = db.InternInfo.Where(s => s.StudentID == id).FirstOrDefault();
            if (existingIntern != null)
            {
                ViewBag.Control = false;
            }
            else
            {
                ViewBag.Control = true;
            }

            return View();
        }

        public ActionResult UpdateInfo()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            clearUnsaved();
            return View();
        }

        public ActionResult UploadFiles(int? id)
        {
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }

            CommentDocumentViewModel cdvm = new CommentDocumentViewModel();

            int tempID = 0;
            if (Session["id"] != null)
            {
                tempID = (int)Session["id"];
                if (id != (int)Session["id"] && id != null)
                {
                    return RedirectToAction("NotFoundStd");
                }
            }
            int studentID = (int)Session["StudentID"];
            openFileuploadDatePicker();
            if (tempID == 0)
            {
                GradeCategory thisCat = (from x in db.GradeCategory where x.GradeCategoryID == id select x).SingleOrDefault();
                if (thisCat.isGrading == false)
                {
                    return RedirectToAction("NotFoundStd");
                }
            }
            else
            {
                GradeCategory thisCat = (from x in db.GradeCategory where x.GradeCategoryID == tempID select x).SingleOrDefault();
                if (thisCat.isGrading == false)
                {
                    return RedirectToAction("NotFoundStd");
                }
            }
            clearGrade();
            if (Session["id"] == null)
            {
                Session["id"] = id;
            }
            int depID = (int)Session["DepartmentID"];
            clearUnsaved();
            if (id != null)
            {
                string name = Session["Name"].ToString();
                string schID = Session["SchoolID"].ToString();
                int nameLength = name.Length;
                int schIDLength = schID.Length;
                int willRemove = nameLength + schIDLength;
                List<Document> thisStd = (from x in db.Document where x.StudentID == studentID && x.Approve == true && x.GradeCategoryID == id select x).ToList();
                var listStdPDF = thisStd;
                for (int i = 0; i < listStdPDF.Count; i++)
                {
                    int index = listStdPDF[i].FileLocation.IndexOf("$$");
                    listStdPDF[i].FileLocation = listStdPDF[i].FileLocation.Substring(index + 2);
                }

                List<Comment> comments = (from x in db.Comment where x.StudentID == studentID && x.GradeCategoryID == id select x).ToList();


                cdvm.tupCD = new Tuple<List<Document>, List<Comment>, Comment>(listStdPDF, comments, null);



                return View(cdvm);
            }
            else
            {
                int newID = (int)Session["id"];
                List<Document> thisStd1 = (from x in db.Document where x.StudentID == studentID && x.Approve == true && x.GradeCategoryID == newID select x).ToList();
                var listStdPDF1 = thisStd1;
                string name = Session["Name"].ToString();
                string schID = Session["SchoolID"].ToString();
                for (int i = 0; i < listStdPDF1.Count; i++)
                {
                    int index = listStdPDF1[i].FileLocation.IndexOf("$$");
                    listStdPDF1[i].FileLocation = listStdPDF1[i].FileLocation.Substring(index + 2);
                }
                List<Comment> comments = (from x in db.Comment where x.StudentID == studentID && x.GradeCategoryID == newID select x).ToList();
                cdvm.tupCD = new Tuple<List<Document>, List<Comment>, Comment>(listStdPDF1, comments, null);


                return View(cdvm);
            }
        }

        public ActionResult AddInternInfo()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            clearUnsaved();
            int id = Convert.ToInt32(Session["StudentID"]);

            if (id == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty");
                return RedirectToAction("NotFoundStd");
            }

            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();
            int stdID = Convert.ToInt32(Session["StudentID"]);
            int semID = semester();
            //InternInfo isReject = (from x in db.InternInfo where x.StudentID == id && x.isDelete == true select x).SingleOrDefault();
            //if(isReject != null)
            //{
            //    ViewBag.Control = true;
            //}
            //else
            //{
            //    ViewBag.Control = true;
            //}

            string surname = Session["Surname"].ToString();
            var existingIntern = db.InternInfo.Where(s => s.StudentID == id && s.SemesterID == semID).FirstOrDefault();
            StudentUserInfo thisstd = (from x in db.StudentUserInfo where x.StudentID == id && x.DepartmentID == depID && x.Approve == true select x).SingleOrDefault();
            Semester isEndInternship = (from x in db.Semester where x.Approve == true && x.DepartmentID == thisstd.DepartmentID select x).SingleOrDefault();

            if (isEndInternship.EndDateInternship > DateTime.Now)
            {

                if (existingIntern != null)
                {
                    ViewBag.Control = false;
                    if (existingIntern.Approve == false && existingIntern.isDelete == false)
                    {
                        ViewBag.internAppSuccess = true;
                    }
                }
                else
                {
                    ViewBag.Control = true;
                }

            }
            else
            {
                ViewBag.appDateIntern = (isEndInternship.EndDateInternship).ToString();
                ViewBag.FinishInternshipDate = true;
                ViewBag.ControlLastDateIntern = false;
            }

            //staj rejected olursa
            InternInfo internInfoStatus = (from x in db.InternInfo where x.StudentID == stdID && x.SemesterID == semID select x).SingleOrDefault();
            if (internInfoStatus != null)
            {
                if (internInfoStatus.Approve == false && internInfoStatus.isDelete == true)
                {
                    ViewBag.internshipRejected = true;
                }
            }

            return View();
        }

        public ActionResult UpdateInternInfo()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            int? id = Convert.ToInt32(Session["StudentID"]);
            clearUnsaved();
            if (id == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty");
                return RedirectToAction("NotFoundStd");
            }

            int? depID = Convert.ToInt32(Session["DepartmentID"]);
            int? facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();
            string surname = Session["Surname"].ToString();

            InternInfo slcCity = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            if (slcCity != null)
            {
                Session["slcCity"] = slcCity.CompanyCity;
            }
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
            if (thisStd.isAbroad == true)
            {
                ViewBag.isAbroad = true;
            }
            else
            {
                ViewBag.isAbroad = false;
            }
            var confirmedIntern = db.InternInfo.Where(s => s.StudentID == id && s.Approve == false).FirstOrDefault();
            if (confirmedIntern != null)
            {
                ViewBag.Control = true;
            }
            else if (confirmedIntern == null)
            {
                ViewBag.Control = false;
            }
            InternInfo isConfirmed = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            if (isConfirmed != null)
            {
                if (isConfirmed.Approve == true)
                {
                    ViewBag.isConfirmedIntern = true;
                }
            }
            if (isConfirmed == null)
            {
                ViewBag.notHere = true;
            }


            return View(confirmedIntern);
        }

        public ActionResult GradeCategoryList()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int stdID = (int)Session["StudentID"];
            int depID = (int)Session["DepartmentID"];
            int semID = semester();
            openFileuploadDatePicker();
            Session["id"] = null;
            clearGrade();
            clearUnsaved();
            InternInfo isApplied = (from x in db.InternInfo where x.StudentID == stdID && x.Approve == true select x).SingleOrDefault();
            if (isApplied != null)
            {
                ViewBag.ShowList = 1;
            }
            else if (isApplied == null)
            {
                ViewBag.ShowList = 2;
            }
            InternInfo isAppliedAccepted = (from x in db.InternInfo where x.StudentID == stdID && x.Approve == false && x.isDelete == false && x.SemesterID == semID select x).SingleOrDefault();

            if (isAppliedAccepted != null)
            {
                ViewBag.ShowList = 3;
            }

            List<GradeCategory> gradeCategories = (from x in db.GradeCategory where x.isGrading == true && x.DepartmentID == depID && x.Approve == true select x).ToList();
            return View(gradeCategories);
        }

        public ActionResult ViewPDF(int? id)
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int thisStdID = (int)Session["StudentID"];
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == thisStdID && x.Approve == true select x).SingleOrDefault();
            Document thisStdDoc = (from x in db.Document where x.StudentID == thisStd.StudentID && x.DocumentID == id && x.Approve == true select x).SingleOrDefault();

            if (thisStdDoc == null)
            {
                return RedirectToAction("NotFoundStdWOButton");
            }
            clearUnsaved();
            clearGrade();



            Document slcDocument = (from x in db.Document where x.DocumentID == id select x).SingleOrDefault();
            string filename = slcDocument.FileLocation;
            String test = filename;
            String number = test.Substring(test.LastIndexOf("Upload\\") + 1);
            var file = filename;
            file = Path.GetFullPath(file);


            return File(file, number);
        }

        public ActionResult DeleteUploadedFile(int? idDelete)
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            clearGrade();
            clearUnsaved();
            int depID = (int)Session["DepartmentID"];
            Document slcDocument = (from x in db.Document where x.DocumentID == idDelete select x).SingleOrDefault();
            Department depname = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
            Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
            int? uploadID = slcDocument.GradeCategoryID;
            Session["uploadID"] = uploadID;
            slcDocument.Approve = false;
            db.SaveChanges();

            string path = slcDocument.FileLocation;

            System.IO.File.Delete(path);

            return RedirectToAction("UploadFiles", uploadID);
        }

        public ActionResult ContactTeachers()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            int depID = (int)Session["DepartmentID"];
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            clearUnsaved();
            List<TeacherUserInfo> teacherlist = (from x in db.TeacherUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
            return View(teacherlist);
        }

        public ActionResult InternStatu()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearGrade();
            clearUnsaved();
            int gradeCount;
            int stdGradeCount;
            int id = Convert.ToInt32(Session["StudentID"]);
            InternInfo ıt = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            if (ıt == null)
            {
                ViewBag.Control2 = true;
                ViewBag.Fail2 = "You have not applied for an internship yet!";
            }
            if (id == 0)
            {
                //return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty");
                return RedirectToAction("NotFoundStd");
            }
            var student = db.InternInfo.Where(s => s.StudentID == id).ToList();
            InternInfo stdGrade = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
            Council stdCouncil = (from x in db.Council where x.CouncilID == thisStd.CouncilID select x).SingleOrDefault();
            List<StudentGrade> thisGrade = (from x in db.StudentGrade where x.StudentID == id select x).ToList();
            List<GradeCategory> currentCategory = (from x in db.GradeCategory where x.Approve == true select x).ToList();
            if (stdCouncil != null)
            {
                ViewBag.CouncilNull = true;
                List<TeacherUserInfo> teacherStd = (from x in db.TeacherUserInfo where x.CouncilID == stdCouncil.CouncilID select x).ToList();
                if (teacherStd != null)
                {
                    gradeCount = teacherStd.Count * currentCategory.Count;
                    stdGradeCount = thisGrade.Count;
                    if (gradeCount == stdGradeCount)
                    {
                        ViewBag.vfi = true;
                        ViewBag.lg = true;
                    }
                    else
                    {
                        ViewBag.vfi = false;
                    }

                }
            }

            return View(student);

        }

        public ActionResult MyGrades()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int myID = (int)Session["StudentID"];
            List<GradeTypeAvarage> myGrades = (from x in db.GradeTypeAvarage where x.StudentID == myID && x.Approve == true select x).ToList();


            return View(myGrades);
        }

        public ActionResult SeeGrade(int? id)
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            Session["shwGrade"] = 3;
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Please re-login! Vaho & Jaho & İbo");
            }

            int? stdID = (int)Session["StudentID"];
            double? res = 0;
            List<StudentGrade> thisGrade = (from x in db.StudentGrade where x.GradeCategoryID == id && x.StudentID == stdID select x).ToList();
            GradeCategory thisGradeCategory = (from x in db.GradeCategory where x.GradeCategoryID == id && x.Approve == true select x).SingleOrDefault();
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == stdID && x.Approve == true select x).SingleOrDefault();
            List<TeacherUserInfo> councilOfStd = (from x in db.TeacherUserInfo where x.CouncilID == thisStd.CouncilID select x).ToList();
            if (thisGrade.Count == councilOfStd.Count)
            {
                Session["Grade1"] = (Math.Round((double)res, 2));
                Session["shwGrade"] = 1;
            }
            else
            {
                Session["shwGrade"] = 2;
                ViewBag.deneme = "-";
            }


            return RedirectToAction("MyGrades");
        }

        public ActionResult MyCouncil()
        {
            Session["ShowErrorMessage"] = null;//Upload filesda pdf yükleme zorunluluğu mesajı
            if (Session["StudentID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int stdID = (int)Session["StudentID"];
            InternInfo thisStd = (from x in db.InternInfo where x.StudentID == stdID select x).SingleOrDefault();
            if (thisStd != null)
            {
                if (thisStd.isDelete == true)
                {
                    ViewBag.RejectedControl = true;
                    ViewBag.Rejected = "Your intern application has been rejected !";
                }
                else
                {
                    ViewBag.RejectedControl = false;
                }
            }
            else
            {
                ViewBag.RejectedControl = false;
            }
            int councilID = Convert.ToInt32(Session["councilID"]);
            if (councilID <= 0)
            {
                ViewBag.Control = true;
                ViewBag.Fail = "Council has not assigned yet!";
            }
            else
            {
                List<TeacherUserInfo> ListTeacherInSameCouncil = (from x in db.TeacherUserInfo where x.CouncilID == councilID && x.Approve == true select x).ToList();
                ViewBag.ControlCard = true;
                return View(ListTeacherInSameCouncil);
            }

            return View();

        }

        //POST Methods
        [HttpPost]

        public ActionResult UpdateInfo(StudentUserInfo std, string Currentpass)
        {

            int id = Convert.ToInt32(Session["StudentID"]);
            string encryptedCurrentPasswordStd = MD5Hash(Currentpass);
            var existingStudent = db.StudentUserInfo.Where(s => s.StudentID == id && s.Approve == true).FirstOrDefault();

            if (existingStudent.StudentPassword == encryptedCurrentPasswordStd && std.NewPassword == std.ConfirmPassword && std.NewPassword != null && ValidatePassword(std.NewPassword)==true)
            {
                if(chkSpace(std.NewPassword) != true)
                {
                    existingStudent.StudentPassword = MD5Hash(std.NewPassword);
                    db.SaveChanges();
                    return RedirectToAction("Login", "Login");
                }
                else
                {
                    ViewBag.ThereIsSpaceInPass = true;
                    return View();
                }
               
            }
            //else if(std.NewPassword != null && std.NewPassword.Length < 6)
            //{
            //    ViewBag.PassError = "New password must be at least 6 characters !";
            //    ViewBag.Pass = true;
            //}
            else if (existingStudent.StudentPassword != encryptedCurrentPasswordStd && std.NewPassword == std.ConfirmPassword)
            {
                ViewBag.PassError = "Your 'Current Password' is wrong !";
                ViewBag.Pass = true;
            }
            else if (existingStudent.StudentPassword == encryptedCurrentPasswordStd && std.NewPassword != std.ConfirmPassword)
            {
                ViewBag.PassError = "'Confirm Password' and 'New Password' are not matching !";
                ViewBag.Pass = true;
            }
            else if (existingStudent.StudentPassword != encryptedCurrentPasswordStd && std.NewPassword != std.ConfirmPassword)
            {
                ViewBag.PassError = "Your 'Current Password' is wrong also 'Confirm Password' and 'New Password' are not matching !";
                ViewBag.Pass = true;
            }
            else if (std.NewPassword == null || std.ConfirmPassword == null || Currentpass == null)
            {
                ViewBag.PassError = "Please fill all areas !";
                ViewBag.Pass = true;
            }
            return View();
        }

        [HttpPost]
        public ActionResult UploadFiles(HttpPostedFileBase file, int? id)
        {
            int depID = (int)Session["DepartmentID"];

            if (file != null && file.ContentLength > 0)
            {
                int? gradeID;
                GradeCategory thisGradeCat = (from x in db.GradeCategory where x.Approve == true && x.GradeCategoryID == id select x).SingleOrDefault();
                if (thisGradeCat != null)
                {
                    Session["GradeCatNameExist"] = thisGradeCat.GradeName;
                }
                // extract only the filename
                string fileName = Session["SchoolID"] + "$" + Session["Name"] + "$$" + Path.GetFileName(file.FileName);
                // store the file inside ~/App_Data/uploads folder
                Department depname = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                int stdid = Convert.ToInt32(Session["StudentID"]);

                string path2 = depname.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString() + "/" +
                               Session["GradeCatNameExist"];
                var path = Path.Combine(Server.MapPath("~/Archives/" + path2), fileName);
                Document thisStdDoc = (from x in db.Document where x.StudentID == stdid && x.Approve == true && x.FileLocation == path select x).SingleOrDefault();
                if (thisStdDoc == null)
                {
                    file.SaveAs(path);

                    string flocation = "PDF File/";
                    string PathString = System.IO.Path.Combine(flocation, fileName);
                    string fileExtend = System.IO.Path.GetExtension(fileName);

                    if (id != (int)Session["id"] && id != null)
                    {
                        return RedirectToAction("GradeCategoryList");
                    }

                    if (fileExtend.ToLower() == ".pdf" && thisStdDoc == null)
                    {
                        var st = new Document
                        {
                            FileLocation = path,
                        };
                        if (id != null)
                        {
                            gradeID = (int)Session["id"];
                            st.GradeCategoryID = gradeID;
                        }
                        else if (id == null)
                        {
                            gradeID = (int)Session["uploadID"];
                            st.GradeCategoryID = gradeID;
                        }
                        st.StudentID = stdid;
                        st.Approve = true;
                        st.SemesterID = semester();
                        db.Document.Add(st);

                        db.SaveChanges();
                        Session["ShowErrorMessage"] = 0;
                        // ViewBag.msg = "Uploaded successfully!";
                        return RedirectToAction("UploadFiles");
                    }
                    else
                    {
                        Session["ShowErrorMessage"] = 1;
                    }
                }
                else
                {
                    Session["ShowErrorMessage"] = 2;
                }
            }
            return RedirectToAction("UploadFiles");

        }

        [HttpPost]
        public ActionResult AddInternInfo(InternInfo info, string City)
        {
            string ddlCity = City;
            int id = Convert.ToInt32(Session["StudentID"]);
            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();
            string surname = Session["Surname"].ToString();

            if (info.CompanyName != null && info.CompanyAddress != null && info.AuthorizedPersonel != null && ddlCity != "EmptyCity" && info.CompanyPhone != null)
            {
                var existingStudent = db.InternInfo.Where(s => s.Approve == true && s.StudentID == id).FirstOrDefault();
                if (existingStudent == null)
                {

                    InternInfo newInfo = new InternInfo();
                    newInfo.CompanyName = info.CompanyName;
                    newInfo.CompanyCity = ddlCity.ToString();
                    newInfo.CompanyAddress = info.CompanyAddress;
                    newInfo.AuthorizedPersonel = info.AuthorizedPersonel.ToUpper();
                    newInfo.CompanyPhone = info.CompanyPhone;
                    newInfo.StudentID = id;
                    newInfo.StudentName = name;
                    newInfo.StudentSurname = surname;
                    newInfo.Approve = false;
                    newInfo.isDelete = false;
                    newInfo.SemesterID = semester();
                    newInfo.DayOfRegistration = DateTime.Now;

                    db.InternInfo.Add(newInfo);
                    db.SaveChanges();
                    ViewBag.InfoApprove = "Completed!";
                    ViewBag.InfoControl = true;
                    return RedirectToAction("AddInternInfo");
                }
                else
                {
                    ViewBag.InfoApprove = "You are already in the system!";
                    ViewBag.InfoControl = true;
                }
            }
            else
            {
                ViewBag.ControlFill = true;
                ViewBag.Control = true;
                ViewBag.Fail = "Please fill all areas!";
            }

            return View();
        }

        [HttpPost]
        public ActionResult UpdateInternInfo(InternInfo info, string City)
        {
            string ddlCity = City;
            int id = Convert.ToInt32(Session["StudentID"]);
            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();
            string surname = Session["Surname"].ToString();

            if (info.CompanyName != null && info.CompanyAddress != null && info.AuthorizedPersonel != null && info.CompanyPhone != null)
            {
                var confirmedIntern = db.InternInfo.Where(s => s.Approve == false && s.StudentID == id).FirstOrDefault();
                StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                if (confirmedIntern != null)
                {
                    InternInfo newInfo = new InternInfo();

                    confirmedIntern.CompanyName = info.CompanyName;
                    if (thisStd.isAbroad == true)
                    {
                        confirmedIntern.CompanyCity = info.CompanyCity;
                    }
                    else
                    {
                        confirmedIntern.CompanyCity = ddlCity.ToString();
                    }
                    confirmedIntern.CompanyAddress = info.CompanyAddress;
                    confirmedIntern.AuthorizedPersonel = info.AuthorizedPersonel.ToUpper();
                    confirmedIntern.CompanyPhone = info.CompanyPhone;
                    confirmedIntern.StudentID = id;
                    confirmedIntern.StudentName = name;
                    confirmedIntern.StudentSurname = surname;
                    confirmedIntern.isDelete = false;
                    db.SaveChanges();
                }
                return View(confirmedIntern);
            }
            else
            {
                ViewBag.ControlFill = true;
                ViewBag.Control = true;
                ViewBag.Fail = "Please fill all areas!";
            }
            return RedirectToAction("UpdateInternInfo");
        }

        [HttpPost]
        public ActionResult AddInternInfoAbroad(InternInfo infoAbroad)
        {
            int id = Convert.ToInt32(Session["StudentID"]);
            int depID = Convert.ToInt32(Session["DepartmentID"]);
            int facID = Convert.ToInt32(Session["FacultyID"]);
            string name = Session["Name1"].ToString();
            string surname = Session["Surname"].ToString();

            if (infoAbroad.CompanyName != null && infoAbroad.CompanyAddress != null && infoAbroad.AuthorizedPersonel != null
                && infoAbroad.CompanyCity != null && infoAbroad.CompanyPhone != null)
            {
                var existingStudent = db.InternInfo.Where(s => s.Approve == true && s.StudentID == id).FirstOrDefault();
                if (existingStudent == null)
                {

                    InternInfo newInfo = new InternInfo();
                    newInfo.CompanyName = infoAbroad.CompanyName;
                    newInfo.CompanyCity = infoAbroad.CompanyCity;
                    newInfo.CompanyAddress = infoAbroad.CompanyAddress;
                    newInfo.AuthorizedPersonel = infoAbroad.AuthorizedPersonel.ToUpper();
                    newInfo.CompanyPhone = infoAbroad.CompanyPhone;
                    newInfo.StudentID = id;
                    newInfo.StudentName = name;
                    newInfo.StudentSurname = surname;
                    newInfo.Approve = false;
                    newInfo.SemesterID = semester();

                    db.InternInfo.Add(newInfo);
                    db.SaveChanges();
                    ViewBag.InfoApprove = "Completed!";
                    ViewBag.InfoControl = true;
                    return RedirectToAction("AddInternInfoAbroad");
                }
                else
                {
                    ViewBag.InfoApprove = "You are already in the system!";
                    ViewBag.InfoControl = true;
                }
            }
            else
            {
                ViewBag.ControlFill = true;
                ViewBag.Control = true;
                ViewBag.Fail = "Please fill all areas!";
            }

            return View();
        }

        [HttpPost]
        public ActionResult SendMessage(CommentDocumentViewModel c)
        {
            string sName = Session["Name1"].ToString();
            string sSurname = Session["Surname"].ToString();
            int studentID = (int)Session["StudentID"];

            if (c.comment != null)
            {
                Comment newComment = new Comment();
                newComment.Comment1 = c.comment;
                newComment.Approve = true;
                newComment.StudentID = studentID;
                newComment.Date = dateTimeNow();
                newComment.GradeCategoryID = (int)Session["id"];
                newComment.SenderID = studentID;
                newComment.SenderName = sName;
                newComment.SenderSurname = sSurname;
                newComment.SemesterID = semester();

                db.Comment.Add(newComment);
                db.SaveChanges();

            }
            return RedirectToAction("UploadFiles");
        }

        //DÜZ METHODS
        public ActionResult makeAbroad()
        {
            int id = Convert.ToInt32(Session["StudentID"]);

            StudentUserInfo std = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
            std.isAbroad = true;
            db.SaveChanges();
            return RedirectToAction("AddInternInfoAbroad");
        }
        public ActionResult makeDomestic()
        {
            int id = Convert.ToInt32(Session["StudentID"]);

            StudentUserInfo std = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
            std.isAbroad = false;
            db.SaveChanges();

            return RedirectToAction("AddInternInfo");
        }
        public void clearUnsaved()
        {
            int id = Convert.ToInt32(Session["StudentID"]);
            if (id == 0)
            {
                notFound(id);
            }
            else
            {
                InternInfo thisStd = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
                StudentUserInfo enteredStd = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                if (thisStd == null)
                {
                    enteredStd.isAbroad = false;
                    db.SaveChanges();
                }
            }

        }
        public ActionResult notFound(int id)
        {
            return RedirectToAction("NotFoundStd");
        }
        public void clearGrade()
        {
            Session["shwGrade"] = 0;
        }
        void openFileuploadDatePicker()
        {

            //DateTime todaydate = DateTime.Now;
            //Özellikle datetime ve string çevirmesi çok yaptım. ihtiyaçtan ve üşengeçlikten oldu. :D
            string todaydate = dateTimeNow().ToString("yyyy MM dd HH:mm tt").Substring(0, 16) + ":00";
            DateTime todayd = Convert.ToDateTime(todaydate);
            string todaydatelast = todayd.ToString("yyyy-MM-dd HH:mm tt");

            int depID = (int)Session["DepartmentID"];

            List<GradeCategory> listGradeCategory = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
            if (listGradeCategory != null)
            {
                for (int i = 0; i < listGradeCategory.Count; i++)
                {
                    DateTime startd = Convert.ToDateTime(listGradeCategory[i].StartDate);
                    string todaydatestart = startd.ToString("yyyy-MM-dd HH:mm tt");
                    DateTime endd = Convert.ToDateTime(listGradeCategory[i].LastDate);
                    string todaydateend = endd.ToString("yyyy-MM-dd HH:mm tt");

                    if (todaydatestart == todaydatelast)
                    {
                        listGradeCategory[i].isGrading = true;
                        db.SaveChanges();
                    }
                    if (todaydateend == todaydatelast)
                    {
                        listGradeCategory[i].isGrading = false;
                        db.SaveChanges();
                    }
                    if (startd <= todayd && todayd < endd)
                    {
                        listGradeCategory[i].isGrading = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        listGradeCategory[i].isGrading = false;
                        db.SaveChanges();
                    }
                    // hata mesajı
                }
            }
        }
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
        public int semester()
        {
            int DepID = Convert.ToInt32(Session["DepartmentID"]);
            Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == DepID select x).SingleOrDefault();
            if (thisSemester != null)
            {
                return thisSemester.SemesterID;
            }
            return 0;
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
           

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new Exception("Password should not be empty");
            }

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
        public bool chkSpace(string pass)
        {
            string s = pass;
            bool fHasSpace = s.Contains(" ");
            return fHasSpace;
        }
    }

}