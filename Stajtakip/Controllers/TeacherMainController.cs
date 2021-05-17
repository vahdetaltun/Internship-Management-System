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
using Stajtakip.Models.EntityFramework;
using StajTakip.Models;

namespace StajTakip.Controllers
{
    public class TeacherMainController : Controller
    {
        private StajTakipEntities db = new StajTakipEntities();


        // GET: TeacherMain
        //GET METHODS
        public ActionResult NotFoundTch()
        {
            return View();
        }
        public ActionResult NotFoundTchWOButton()
        {
            return View();
        }
        public ActionResult TeacherMainPage()
        {
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int TchID = Convert.ToInt32(Session["TeacherID"]);
            int DepID = Convert.ToInt32(Session["DepartmentID"]);
            if (DepID == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty Information");
            }
            Department depName = (from x in db.Department where x.DepartmentID == DepID && x.Approve == true select x).SingleOrDefault();
            Session["Department"] = depName.DepartmentName;

            TeacherUserInfo thisTeacher = (from x in db.TeacherUserInfo where x.TeacherID == TchID && x.DepartmentID == DepID && x.Approve == true select x).SingleOrDefault();
            if (thisTeacher != null)
            {
                Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == thisTeacher.DepartmentID select x).SingleOrDefault();
                ViewBag.tchSemesterName = thisSemester.SemesterName;
                ViewBag.tchInternLastDate = thisSemester.EndDateInternship;

                List<StudentUserInfo> CountStudentInCouncil = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == DepID && x.CouncilID == thisTeacher.CouncilID && x.CouncilID != null select x).ToList();
                if (CountStudentInCouncil != null)
                {
                    ViewBag.tchCountStudentInCouncil = CountStudentInCouncil.Count;
                }
                else
                {
                    ViewBag.tchCountStudentInCouncil = "-";
                }


                string fileName = depName.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year.ToString() + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString() + ".pdf";

                string embed = "<object data=\"{0}\" type=\"application/pdf\" width=\"100%\" height=\"100%\">";
                embed += "If you are unable to view file, you can download from <a href = \"{0}\">here</a>";
                embed += " or download <a target = \"_blank\" href = \"http://get.adobe.com/reader/\">Adobe PDF Reader</a> to view the file.";
                embed += "</object>";

                ViewBag.Embed = string.Format(embed, VirtualPathUtility.ToAbsolute("~/Internship Calendar/" + fileName));

            }


            return View();
        }
        public ActionResult UpdateInfo()
        {
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            return View();
        }
        public ActionResult MyStudents()
        {
            Session["stdID"] = null;
            Session["GradeCatID"] = null;
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int id = Convert.ToInt32(Session["TeacherID"]);
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Occured a problem! Please re-login the system.");
            }

            TeacherUserInfo enteredTeacher = (from x in db.TeacherUserInfo where x.TeacherID == id select x).SingleOrDefault();
            List<StudentUserInfo> myStudents = (from x in db.StudentUserInfo where x.CouncilID == enteredTeacher.CouncilID && x.CouncilID != null && x.Approve == true select x).ToList();
            if (myStudents.Count == 0)
            {
                ViewBag.Controlstd = true;
                ViewBag.emptyStd = "You have not been assigned to a council yet. Therefore, there are no students assigned to you yet.";
            }
            return View(myStudents);
        }
        public ActionResult GradeCategoryList(int? stdID)
        {

            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            List<GradeCategory> allCategories = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
            if (Session["stdID"] == null)
            {
                Session["stdID"] = stdID;
            }
            else
            {
                return RedirectToAction("NotFoundTch");
            }


            return View(allCategories);
        }
        public ActionResult GradeStudent(int? GradeCategoryID)
        {
            if (Session["GradeCatID"] != null)
            {
                if (Convert.ToInt32(Session["GradeCatID"]) != GradeCategoryID && GradeCategoryID != null)
                {
                    return RedirectToAction("NotFoundTch");
                }
            }

            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            if (Session["GradeCatID"] == null)
            {
                Session["GradeCatID"] = GradeCategoryID;
            }

            int gradeCatID = (int)Session["GradeCatID"];

            GradeDocumentViewModel gdvm = new GradeDocumentViewModel();
            if (Session["stdID"] == null || gdvm == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty - Session");
            }
            int? TeacherID = (int)Session["TeacherID"];
            int? studentID = (int)Session["stdID"];
            int? stdID = (int)Session["stdID"];
            //List<Document> listDoc = (from x in db.Document where x.StudentID == stdID && x.GradeCategoryID == GradeCategoryID && x.Approve == true select x).ToList();
            StudentUserInfo thiStd = (from x in db.StudentUserInfo where x.StudentID == studentID select x).SingleOrDefault();
            string name = thiStd.StudentName + " " + thiStd.StudentSurname;
            string schID = thiStd.StudentSchoolID.ToString();
            if (GradeCategoryID != null)
            {
                List<Document> thisStd = (from x in db.Document where x.StudentID == studentID && x.Approve == true && x.GradeCategoryID == GradeCategoryID select x).ToList();
                var listStdPDF = thisStd;
                for (int i = 0; i < listStdPDF.Count; i++)
                {
                    int index = listStdPDF[i].FileLocation.IndexOf("$$");
                    listStdPDF[i].FileLocation = listStdPDF[i].FileLocation.Substring(index + 2);
                }
                gdvm.docgrade = listStdPDF.ToList();
            }
            else if (GradeCategoryID == null)
            {
                List<Document> thisStd1 = (from x in db.Document where x.StudentID == studentID && x.Approve == true && x.GradeCategoryID == gradeCatID select x).ToList();
                var listStdPDF1 = thisStd1;
                for (int i = 0; i < listStdPDF1.Count; i++)
                {
                    int index = listStdPDF1[i].FileLocation.IndexOf("$$");
                    listStdPDF1[i].FileLocation = listStdPDF1[i].FileLocation.Substring(index + 2);
                }
                gdvm.docgrade = listStdPDF1.ToList();
            }





            int? id = (int)Session["stdID"];
            List<Comment> listComment = (from x in db.Comment where x.StudentID == id && x.Approve == true select x).ToList();
            gdvm.comment = listComment;
            List<SelectListItem> gradeList = new List<SelectListItem>();
            foreach (var item in db.GradeCategory.ToList())
            {
                if (item.Approve == true)
                {
                    gradeList.Add(new SelectListItem { Text = item.GradeName, Value = item.GradeCategoryID.ToString() });

                }
            }
            ViewBag.Grade = gradeList;
            int? ddlGradeID = GradeCategoryID;
            if (GradeCategoryID != null)
            {
                Session["id"] = (int)GradeCategoryID;
            }

            GradeCategory slcGradeCategory = (from x in db.GradeCategory where x.GradeCategoryID == ddlGradeID && x.Approve == true select x).SingleOrDefault();

            List<GradeCategory> listGradeCategory = db.GradeCategory.ToList();

            if (slcGradeCategory == null)
            {
                ViewBag.Control1 = true;

            }

            InternInfo ishere = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            StudentUserInfo selectedStudent = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();

            if (slcGradeCategory != null)
            {
                StudentGrade thisGrade = (from x in db.StudentGrade where x.StudentID == selectedStudent.StudentID && x.GradeCategoryID == slcGradeCategory.GradeCategoryID && x.TeacherID == TeacherID && x.Approve == true select x).SingleOrDefault();
                if (thisGrade != null)
                {
                    Session["CurrentGrade"] = thisGrade.Grade;
                }
                else
                {
                    Session["CurrentGrade"] = null;
                }

            }
            else if (ViewBag.gohomepage == false)
            {
                return RedirectToAction("MyStudents");
            }


            InternInfo emptyInfo = new InternInfo();
            if (ishere == null)
            {
                emptyInfo.StudentID = id;
                emptyInfo.CompanyName = "-";
                emptyInfo.CompanyAddress = "-";
                emptyInfo.CompanyCity = "-";
                emptyInfo.CompanyPhone = "-";
                emptyInfo.AuthorizedPersonel = "-";
                emptyInfo.Approve = false;
                emptyInfo.StudentName = selectedStudent.StudentName;
                emptyInfo.StudentSurname = selectedStudent.StudentSurname;
                db.InternInfo.Add(emptyInfo);
                db.SaveChanges();
            }

            return View(gdvm);
        }
        public ActionResult ViewPDF(int? id)
        {
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int thisTchID = (int)Session["TeacherID"];
            TeacherUserInfo thisTch = (from x in db.TeacherUserInfo where x.Approve == true && x.TeacherID == thisTchID select x).SingleOrDefault();
            Document slcDoc = (from x in db.Document where x.DocumentID == id && x.Approve == true select x).SingleOrDefault();
            if (slcDoc != null)
            {
                StudentUserInfo stdInCouncil = (from x in db.StudentUserInfo where x.CouncilID == thisTch.CouncilID && x.StudentID == slcDoc.StudentID && x.Approve == true select x).SingleOrDefault();
                if (stdInCouncil == null)
                {
                    return RedirectToAction("NotFoundTchWOButton");
                }
                Document slcDocument = (from x in db.Document where x.DocumentID == id select x).SingleOrDefault();
                string filename = slcDocument.FileLocation;
                String test = filename;
                String number = test.Substring(test.LastIndexOf("Upload\\") + 1);

                var file = filename;
                file = Path.GetFullPath(file);

                return File(file, number);
            }

            return RedirectToAction("NotFoundTchWOButton");

        }
        public ActionResult MyCouncil()
        {
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
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
        public ActionResult MyStudentsInternDetail(int? StudentID)
        {
            if (Session["TeacherID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }

            InternInfo thisInternInfo = (from x in db.InternInfo where x.StudentID == StudentID select x).SingleOrDefault();
            return View(thisInternInfo);
        }
        //POST METHODS
        [HttpPost]
        public ActionResult UpdateInfo(TeacherUserInfo tch, string CurrentPass)
        {
            int id = Convert.ToInt32(Session["TeacherID"]);
            string encryptedCurrentPasswordTch = MD5Hash(CurrentPass);
            var existingTeacher = db.TeacherUserInfo.Where(s => s.TeacherID == id && s.Approve == true).FirstOrDefault();
           
                if (existingTeacher.TeacherPassword == encryptedCurrentPasswordTch && tch.NewPassword == tch.ConfirmPassword && tch.NewPassword != null && ValidatePassword(tch.NewPassword) == true)
                {

                    if (chkSpace(tch.NewPassword) != true)
                    {

                        existingTeacher.TeacherPassword = MD5Hash(tch.NewPassword);
                        db.SaveChanges();
                        return RedirectToAction("Login", "Login");

                    }
                    else
                    {
                        ViewBag.ThereIsSpaceInPass = true;
                        return View();
                    }



                }
                else if (existingTeacher.TeacherPassword != encryptedCurrentPasswordTch && tch.NewPassword == tch.ConfirmPassword)
                {
                    ViewBag.PassError = "Your 'Current Password' is wrong !";
                    ViewBag.Pass = true;
                }
                else if (existingTeacher.TeacherPassword == encryptedCurrentPasswordTch && tch.NewPassword != tch.ConfirmPassword)
                {
                    ViewBag.PassError = "'Confirm Password' and 'New Password' are not matching !";
                    ViewBag.Pass = true;
                }
                else if (existingTeacher.TeacherPassword != encryptedCurrentPasswordTch && tch.NewPassword != tch.ConfirmPassword)
                {
                    ViewBag.PassError = "Your 'Current Password' is wrong also 'Confirm Password' and 'New Password' are not matching !";
                    ViewBag.Pass = true;
                }
                else if (tch.NewPassword == null || tch.ConfirmPassword == null || CurrentPass == null)
                {
                    ViewBag.PassError = "Please fill all areas !";
                    ViewBag.Pass = true;
                }
            
            
            ModelState.Clear();
            return View();
        }
        [HttpPost]
        public ActionResult GradeStudent(double? Grade)
        {
            int depID = (int)Session["DepartmentID"];
            GradeDocumentViewModel gdvm = new GradeDocumentViewModel();
            if (Grade == null)
            {
                gdvm.Grade = gdvm.Grade;
                db.SaveChanges();
                return RedirectToAction("MyStudents");

            }
            else
            {
                int? id = (int)Session["stdID"];
                int gradeID;
                double? res = 0;
                int cmCount;
                int gcID = Convert.ToInt32(Session["id"]);
                ModelState.Clear();
                GradeTypeAvarage grdTypeAvg = new GradeTypeAvarage();
                InternInfo slcStd = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
                StudentGrade gradedStudent = new StudentGrade();
                GradeCategory isSelected = (from x in db.GradeCategory where x.GradeCategoryID == gcID select x).SingleOrDefault();
                int? ddlGradeID = isSelected.GradeCategoryID;
                StudentOverallGrade newOverall = new StudentOverallGrade();
                StudentOverallGrade isExist = (from x in db.StudentOverallGrade where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                int TeacherID = Convert.ToInt32(Session["TeacherID"]);

                InternInfo gradedStudentInfo = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
                TeacherUserInfo teacher = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
                List<TeacherUserInfo> councilOfTch = (from x in db.TeacherUserInfo where x.CouncilID == teacher.CouncilID && x.Approve == true select x).ToList();
                string currentDate = DateTime.Now.Year.ToString();
                StudentGrade isGraded = (from x in db.StudentGrade where x.TeacherID == TeacherID && x.GradeCategoryID == ddlGradeID && x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                if (isGraded != null)
                {
                    Session["isGradedGrade"] = isGraded.Grade;
                }
                GradeTypeAvarage isGradeTypeAvgHere = (from x in db.GradeTypeAvarage where x.StudentID == id && x.GradeCategoryID == ddlGradeID select x).SingleOrDefault();


                if (isGraded == null)
                {
                    gradedStudent.TeacherID = TeacherID;
                    gradedStudent.CouncilID = teacher.CouncilID;
                    gradedStudent.GradeCategoryID = ddlGradeID;
                    //gradedStudent.Grade = (Math.Round(Grade,2));
                    gdvm.Grade = (Math.Round(Convert.ToDouble(Grade), 2));
                    gradedStudent.Grade = gdvm.Grade;
                    gradedStudent.Approve = true;
                    gradedStudent.StudentID = id;
                    gradedStudent.Date = currentDate;
                    gradedStudent.SemesterID = semester();

                    db.StudentGrade.Add(gradedStudent);
                    db.SaveChanges();
                }
                else
                {
                    isGraded.TeacherID = TeacherID;
                    isGraded.CouncilID = teacher.CouncilID;
                    isGraded.GradeCategoryID = ddlGradeID;
                    isGraded.Grade = (Math.Round(Convert.ToDouble(Grade), 2));
                    isGraded.Approve = true;
                    isGraded.StudentID = id;
                    isGraded.Date = currentDate;
                    db.SaveChanges();
                }
                List<StudentGrade> sameStudent = (from x in db.StudentGrade where x.StudentID == id && x.Approve == true select x).ToList();
                for (int i = 0; i < sameStudent.Count; i++)
                {
                    if (isExist == null)
                    {
                        gradeID = Convert.ToInt32(sameStudent[i].GradeCategoryID);
                        GradeCategory thisPercent = (from x in db.GradeCategory where x.GradeCategoryID == gradeID select x).SingleOrDefault();
                        newOverall.Grade = (Math.Round(Convert.ToDouble((Grade) * thisPercent.Percent / 100), 2));
                        newOverall.StudentID = Convert.ToInt32(sameStudent[i].StudentID);
                        newOverall.Date = DateTime.Now;
                        newOverall.Approve = true;
                        newOverall.SemesterID = semester();

                        db.StudentOverallGrade.Add(newOverall);
                        db.SaveChanges();
                    }
                    else
                    {
                        double result;
                        StudentUserInfo thisStudent = (from x in db.StudentUserInfo where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                        Council studentCouncil = (from x in db.Council where x.CouncilID == thisStudent.CouncilID select x).SingleOrDefault();
                        List<TeacherUserInfo> teachersInCouncil = (from x in db.TeacherUserInfo where x.CouncilID == studentCouncil.CouncilID && x.Approve == true select x).ToList();
                        cmCount = teachersInCouncil.Count;
                        StudentOverallGrade samestd = (from x in db.StudentOverallGrade where x.StudentID == id && x.Approve == true select x).SingleOrDefault();
                        gradeID = Convert.ToInt32(sameStudent[i].GradeCategoryID);
                        GradeCategory thisPercent = (from x in db.GradeCategory where x.GradeCategoryID == gradeID select x).SingleOrDefault();

                        res += (double)(sameStudent[i].Grade) * ((float)thisPercent.Percent / 100);

                        result = (double)(res / cmCount);
                        samestd.Grade = Math.Round(result, 2);

                        samestd.StudentID = Convert.ToInt32(sameStudent[i].StudentID);
                        samestd.Approve = true;
                        db.SaveChanges();
                    }
                    StudentOverallGrade thisStd = (from x in db.StudentOverallGrade where x.StudentID == id select x).SingleOrDefault(); //BURADA SIKINTI YOK TABLO TRUNCATELE GEÇER / tm
                    slcStd.Grade = (Math.Round(Convert.ToDouble(thisStd.Grade), 2));
                    db.SaveChanges();

                }
                if (isGradeTypeAvgHere == null && isGraded == null && councilOfTch.Count != 1)
                {
                    grdTypeAvg.Approve = false;
                    grdTypeAvg.GradeCategoryID = ddlGradeID;
                    grdTypeAvg.GradeCategoryName = isSelected.GradeName;
                    grdTypeAvg.DepartmentID = depID;
                    grdTypeAvg.GradeTypeAvarage1 = Grade;
                    grdTypeAvg.StudentName = slcStd.StudentName;
                    grdTypeAvg.StudentSurname = slcStd.StudentSurname;
                    grdTypeAvg.StudentID = slcStd.StudentID;
                    grdTypeAvg.SemesterID = semester();

                    db.GradeTypeAvarage.Add(grdTypeAvg);
                    db.SaveChanges();
                }
                else if (isGradeTypeAvgHere == null && isGraded == null && councilOfTch.Count == 1)
                {

                    grdTypeAvg.Approve = true;
                    grdTypeAvg.GradeCategoryID = ddlGradeID;
                    grdTypeAvg.GradeCategoryName = isSelected.GradeName;
                    grdTypeAvg.DepartmentID = depID;
                    grdTypeAvg.GradeTypeAvarage1 = Grade;
                    grdTypeAvg.StudentName = slcStd.StudentName;
                    grdTypeAvg.StudentSurname = slcStd.StudentSurname;
                    grdTypeAvg.StudentID = slcStd.StudentID;
                    grdTypeAvg.SemesterID = semester();

                    db.GradeTypeAvarage.Add(grdTypeAvg);
                    db.SaveChanges();
                }
                else if (isGradeTypeAvgHere != null && isGraded != null)
                {
                    if (isGradeTypeAvgHere.Approve == true) //artık bütün hocalar not vermiş kontrolü
                    {
                        double? prevGrade = (double)Session["isGradedGrade"];
                        double prevGradeWithMult = (double)isGradeTypeAvgHere.GradeTypeAvarage1 * councilOfTch.Count;
                        isGradeTypeAvgHere.GradeTypeAvarage1 = prevGradeWithMult - prevGrade;
                        db.SaveChanges();
                        isGradeTypeAvgHere.GradeTypeAvarage1 += Grade;
                        db.SaveChanges();
                        isGradeTypeAvgHere.GradeTypeAvarage1 = (Math.Round(Convert.ToDouble(isGradeTypeAvgHere.GradeTypeAvarage1 / councilOfTch.Count), 2));
                        db.SaveChanges();
                    }
                    else
                    {
                        double? prevGrade = (double)Session["isGradedGrade"];
                        isGradeTypeAvgHere.GradeTypeAvarage1 -= prevGrade;
                        db.SaveChanges();
                        isGradeTypeAvgHere.GradeTypeAvarage1 += Grade;
                        db.SaveChanges();
                    }

                }
                else
                {
                    GradeTypeAvarage hereTypeAvg = (from z in db.GradeTypeAvarage where z.StudentID == id && z.GradeCategoryID == ddlGradeID select z).SingleOrDefault();

                    List<StudentGrade> isCountOk = (from x in db.StudentGrade where x.StudentID == id && x.GradeCategoryID == ddlGradeID && x.Approve == true select x).ToList();
                    if (isCountOk.Count == councilOfTch.Count)
                    {
                        hereTypeAvg.GradeTypeAvarage1 += Grade;
                        db.SaveChanges();
                        hereTypeAvg.GradeTypeAvarage1 = (Math.Round(Convert.ToDouble(hereTypeAvg.GradeTypeAvarage1 / councilOfTch.Count), 2));
                        db.SaveChanges();
                        hereTypeAvg.Approve = true;
                        db.SaveChanges();
                    }
                    else
                    {
                        hereTypeAvg.GradeTypeAvarage1 += Grade;
                        db.SaveChanges();
                    }
                }
                return RedirectToAction("MyStudents");
            }

        }
        [HttpPost]
        public ActionResult SendMessage(GradeDocumentViewModel c)
        {

            string sName = Session["TeacherName"].ToString();
            string sSurname = Session["TeacherSurname"].ToString();
            int teacherID = (int)Session["TeacherID"];
            Comment newComment = new Comment();
            newComment.Comment1 = c.sendingcomment;
            newComment.Approve = true;
            newComment.StudentID = (int)Session["stdID"];
            newComment.Date = dateTimeNow();
            newComment.GradeCategoryID = (int)Session["GradeCatID"];
            newComment.SenderID = teacherID;
            newComment.SenderName = sName;
            newComment.SenderSurname = sSurname;
            newComment.SemesterID = semester();
            db.Comment.Add(newComment);
            db.SaveChanges();
            ViewBag.gohomepage = true;
            return RedirectToAction("GradeStudent");
        }
        //DÜZ Methods
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

            return thisSemester.SemesterID;
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
