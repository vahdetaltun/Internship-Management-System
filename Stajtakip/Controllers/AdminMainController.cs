using Newtonsoft.Json;
using StajTakip.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.IO;
using Spire.Xls;
using System.Web.UI;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Stajtakip.Models.EntityFramework;

namespace StajTakip.Controllers
{
    public class AdminMainController : Controller
    {
        private StajTakipEntities db = new StajTakipEntities();

        //GET Methods
        public ActionResult StartSemester()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            return View();
        }
        public ActionResult NotFound()
        {
         
            return View();
        }
        public ActionResult InternshipOperations()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearUnsaved();
            clearSessionAllButMainPage();
            return View();
        }
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        public ActionResult AdminMainPage()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            
            int DepID = Convert.ToInt32(Session["DepartmentID"]);
            openFileuploadDatePicker();
            Session["CouncilIDClear"] = null;
            Session["StudentCouncilIDClear"] = null;
           

            clearCouncilIDWOCStd();
            
            clearUnsaved();
            clearSessions();

            Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == DepID select x).SingleOrDefault();
            List<StudentUserInfo> CountofStd = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == DepID select x).ToList();
            Department thisDepartment = (from x in db.Department where x.Approve == true && x.DepartmentID == DepID select x).SingleOrDefault();

            Session["CountofStd"] = CountofStd.Count;
            Session["SemesterName"] = thisSemester.SemesterName;
            Session["InternshipLastDate"] = thisSemester.EndDateInternship;

            string fileName = thisDepartment.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year.ToString() + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString() + ".pdf";

            string embed = "<object data=\"{0}\" type=\"application/pdf\" width=\"100%\" height=\"100%\">";
            embed += "If you are unable to view file, you can download from <a href = \"{0}\">here</a>";
            embed += " or download <a target = \"_blank\" href = \"http://get.adobe.com/reader/\">Adobe PDF Reader</a> to view the file.";
            embed += "</object>";

            ViewBag.Embed = string.Format(embed, VirtualPathUtility.ToAbsolute("~/Internship Calendar/" + fileName));

           

            return View();
        }
        public ActionResult PendingStudents()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            int semID = semester();
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessionAllButMainPage();
            clearSessions();
            List<StudentUserInfo> thisDepStds = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID && x.SemesterID == semID select x).ToList();

            List<InternInfo> listunconfirm = (from x in db.InternInfo where x.Approve == false && x.isDelete == false && x.SemesterID == semID select x).ToList();


            var result = listunconfirm.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID));



            return View(result);
        }
        public ActionResult ConfirmedStudents()
        {
            int depID = (int)Session["DepartmentID"];
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessions();
            List<StudentUserInfo> thisDepStds = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<InternInfo> listconfirmed = (from x in db.InternInfo where x.Approve == true select x).ToList();

            var result = listconfirmed.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID));

            return View(result);
        }
        public ActionResult AdminEditInfo()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessionAllButMainPage();
            clearSessions();
            int? id = Convert.ToInt32(Session["AdminID"]);
            var existingAdmin = db.AdminUserInfo.Where(x => x.AdminID == id).FirstOrDefault();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else if (existingAdmin == null)
            {
                //return HttpNotFound();
                return RedirectToAction("NotFound");
            }
            return View(existingAdmin);
        }
        public ActionResult AddTeacher()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            clearUnsaved();
            clearSessions();
            return View();
        }
        public ActionResult ListTeacher()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessions();
            clearSessionAllButMainPage();

            var listTeacher = (from x in db.TeacherUserInfo where x.DepartmentID == depID && x.Approve == true select x).ToList();
            return View(listTeacher);
        }
        public ActionResult EditTeacher(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            clearUnsaved();
            clearSessions();
            var findTeacher = db.TeacherUserInfo.Find(id);

            return View(findTeacher);
        }
        public ActionResult DeleteTeacher(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessions();
            clearSessionAllButMainPage();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var deleteTeacher = (from x in db.TeacherUserInfo where x.TeacherID == id select x).SingleOrDefault();
            deleteTeacher.Approve = false;
            deleteTeacher.CouncilID = null;

            db.SaveChanges();

            if (deleteTeacher == null)
            {
                //return HttpNotFound();
                return RedirectToAction("NotFound");
            }

            return RedirectToAction("ListTeacher");
        }
        public ActionResult DetailTeacher(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessionAllButMainPage();
            clearSessions();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherUserInfo teacherDetail = db.TeacherUserInfo.Find(id);
            if (teacherDetail == null)
            {
                //return HttpNotFound();
                return RedirectToAction("NotFound");
            }
            if (teacherDetail.Approve == true)
            {
                ViewBag.Control = true;
            }
            else
            {
                ViewBag.Control = false;
            }
            return View(teacherDetail);

        }
        public ActionResult CreateCouncil()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessionAllButMainPage();
            clearSessions();
            return View();

        }
        public ActionResult AddMemberToCouncil(int? TeacherID, int? CouncilID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            if (Session["CouncilIDClear"] != null && CouncilID != null)
            {
                if ((int)Session["CouncilIDClear"] != CouncilID)
                {
                    //return RedirectToAction("AdminMainPage");
                    return RedirectToAction("NotFound");
                }
            }
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            clearSessions();
            // int[] members = new int[3];
            int? ddlCouncilID = CouncilID;
            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();
            if (Session["CouncilIDClear"] == null)
            {
                Session["CouncilIDClear"] = CouncilID;
            }

            Council slcCouncil = (from x in db.Council where x.CouncilID == ddlCouncilID select x).SingleOrDefault();
            Council fillingCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            List<Council> listCouncils = (from x in db.Council where x.DepartmentID == depID select x).ToList();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.Approve == true && x.DepartmentID == depID select x).ToList();
            if (fillingCouncil == null)
            {

                ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, null);
            }

            else
            {
                List<TeacherUserInfo> listTeacherInCouncil = (from x in db.TeacherUserInfo where x.CouncilID == fillingCouncil.CouncilID && x.Approve == true select x).ToList();
                ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, listTeacherInCouncil);
                ViewBag.teacherInCouncilCount = listTeacherInCouncil.Count;
            }


            if (fillingCouncil != null)
            {
                ViewBag.Control = true;
            }
            List<SelectListItem> councilList = new List<SelectListItem>();
            foreach (var item in (from x in db.Council where x.DepartmentID == depID select x).ToList())
            {
                if (item.Approve == false && item.SemesterID == semester())
                {
                    councilList.Add(new SelectListItem { Text = item.CouncilName, Value = item.CouncilID.ToString() });

                }
            }
            ViewBag.ddlCouncil = councilList;
            //Council slc = (from x in db.Council where x.CouncilID == id select x).SingleOrDefault();

            ctvm.t = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).ToList();


            if (slcCouncil != null)
            {
                // clearUnsaved();

                ViewBag.selectedCouncil = slcCouncil.CouncilID;
                ViewBag.selected = slcCouncil.CouncilName;
                slcCouncil.isFilling = true;

                db.SaveChanges();

            }
            TeacherUserInfo addingTeacher = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
            if (addingTeacher != null)
            {
                addingTeacher.CouncilID = fillingCouncil.CouncilID;
                db.SaveChanges();
            }



            return View(ctvm);

        }
        public ActionResult AddMemberToCouncilButton(int? TeacherID, int? CouncilID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];

            if (Session["CouncilIDClear"] != null && CouncilID != null)
            {
                if ((int)Session["CouncilIDClear"] != CouncilID)
                {
                    //return RedirectToAction("AdminMainPage");
                    return RedirectToAction("NotFound");
                }
            }
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            clearSessions();
            // int[] members = new int[3];
            int? ddlCouncilID = CouncilID;
            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();
            if (Session["CouncilIDClear"] == null)
            {
                Session["CouncilIDClear"] = CouncilID;
            }

            Council slcCouncil = (from x in db.Council where x.CouncilID == ddlCouncilID select x).SingleOrDefault();
            Council fillingCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            List<Council> listCouncils = (from x in db.Council where x.DepartmentID == depID select x).ToList();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.Approve == true && x.DepartmentID == depID select x).ToList();
            if (fillingCouncil == null)
            {

                ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, null);
            }

            else
            {
                List<TeacherUserInfo> listTeacherInCouncil = (from x in db.TeacherUserInfo where x.CouncilID == fillingCouncil.CouncilID && x.Approve == true select x).ToList();
                ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, listTeacherInCouncil);
                ViewBag.teacherInCouncilCount = listTeacherInCouncil.Count;
            }


            if (fillingCouncil != null)
            {
                ViewBag.Control = true;
            }
            List<SelectListItem> councilList = new List<SelectListItem>();
            foreach (var item in (from x in db.Council where x.DepartmentID == depID select x).ToList())
            {
                if (item.Approve == false && item.SemesterID == semester())
                {
                    councilList.Add(new SelectListItem { Text = item.CouncilName, Value = item.CouncilID.ToString() });

                }
            }
            ViewBag.ddlCouncil = councilList;
            //Council slc = (from x in db.Council where x.CouncilID == id select x).SingleOrDefault();

            ctvm.t = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).ToList();


            if (slcCouncil != null)
            {
                // clearUnsaved();

                ViewBag.selectedCouncil = slcCouncil.CouncilID;
                ViewBag.selected = slcCouncil.CouncilName;
                slcCouncil.isFilling = true;

                db.SaveChanges();

            }
            TeacherUserInfo addingTeacher = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
            if (addingTeacher != null)
            {
                addingTeacher.CouncilID = fillingCouncil.CouncilID;
                db.SaveChanges();
            }

            return RedirectToAction("AddMemberToCouncil");
        }
        public ActionResult AddStudentToCouncil(int? StudentID, int? CouncilID)
        {
            int depID = (int)Session["DepartmentID"];
            int slcCouncilIDWTeachers;
            if (Session["StudentCouncilIDClear"] == null)
            {
                Session["StudentCouncilIDClear"] = CouncilID;
            }
            Council isHere = (from x in db.Council where x.CouncilID == CouncilID && x.Approve == true select x).SingleOrDefault();
            if (CouncilID != null)
            {
                if (isHere == null)
                {
                    return RedirectToAction("NotFound");

                }
            }

            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            if (CouncilID == null)
            {
                Session["ShowComitee"] = 0;
            }
            clearUnsaved();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            if (CouncilID != null)
            {
                Session["slcCouncilName"] = isHere.CouncilName;
                Session["CouncilID"] = CouncilID;
                if ((int)Session["CouncilID"] == CouncilID)
                {
                    ViewBag.controlCounCount = true;
                }
                else
                {
                    ViewBag.controlCounCount = false;
                }

            }
            int? ddlCouncilID = CouncilID;

            List<Council> listCouncils = (from x in db.Council where x.DepartmentID == depID select x).ToList();
            //List<StudentUserInfo> listStudents = (from x in db.StudentUserInfo where x.CouncilID == null && x.Approve == true select x).ToList();                       
            List<InternInfo> listStudent = (from x in db.InternInfo where x.Approve == true && x.CouncilID == null select x).ToList();
            List<StudentUserInfo> thisDepStds = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<InternInfo> result = listStudent.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID)).ToList();
            if (CouncilID != null || Session["CouncilID"] != null)
            {
                Session["ShowComitee"] = 1;
            }
            if (Session["CouncilID"] != null)
            {
                slcCouncilIDWTeachers = (int)Session["CouncilID"];
            }
            else
            {
                slcCouncilIDWTeachers = 0;
            }
            List<TeacherUserInfo> listTeacherName = (from x in db.TeacherUserInfo where x.CouncilID == slcCouncilIDWTeachers && x.Approve == true select x).ToList();

            CouncilStudentViewModel csvm = new CouncilStudentViewModel();
            if (Session["CouncilID"] != null)
            {
                List<StudentUserInfo> stdCountOfCouncil = (from x in db.StudentUserInfo where x.CouncilID == ddlCouncilID select x).ToList();
                int countStd = stdCountOfCouncil.Count;
                Session["count1"] = countStd;

                Council slcCouncil = (from x in db.Council where x.CouncilID == ddlCouncilID select x).SingleOrDefault();

                if (slcCouncil != null)
                {


                    db.SaveChanges();
                }
            }
            csvm.tupCTS = new Tuple<List<Council>, List<TeacherUserInfo>, List<InternInfo>>(listCouncils, listTeacherName, result);
            List<SelectListItem> councilList = new List<SelectListItem>();
            foreach (var item in (from x in db.Council where x.DepartmentID == depID select x).ToList())
            {
                if (item.Approve == true)
                {
                    councilList.Add(new SelectListItem { Text = item.CouncilName, Value = item.CouncilID.ToString() });

                }
            }
            ViewBag.Council = councilList;

            return View(csvm);

        }
        public ActionResult AddStudentMethod(int? StudentID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            if (Session["CouncilID"] != null)
            {
                int? slcCouncilID = (int)Session["CouncilID"];
                Council fillingCouncil = (from x in db.Council where x.CouncilID == slcCouncilID select x).SingleOrDefault();
                StudentUserInfo slcStudent = (from x in db.StudentUserInfo where x.StudentID == StudentID select x).SingleOrDefault();
                InternInfo slcStudentInternInfo = (from x in db.InternInfo where x.StudentID == StudentID select x).SingleOrDefault();
                slcStudent.CouncilID = fillingCouncil.CouncilID;
                slcStudentInternInfo.CouncilID = fillingCouncil.CouncilID;
                db.SaveChanges();
                List<StudentUserInfo> stdCountOfCouncil = (from x in db.StudentUserInfo where x.CouncilID == slcCouncilID select x).ToList();
                int countStd = stdCountOfCouncil.Count;
                Session["count"] = countStd;
            }

            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();


            return Redirect("AddStudentToCouncil");

        }
        public ActionResult ListCouncil()
        {
            int depID = (int)Session["DepartmentID"];
            Session["EditCouncilID"] = null; //Edit councilde farklı konseylere geçtiğimizde hoca isimlerinin düzgün gelmesini sağlıyor.
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearCouncilIDWOCStd();
            clearUnsaved();
            clearSessionAllButMainPage();


            List<Council> listCouncils = (from x in db.Council where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.DepartmentID == depID select x).ToList();
            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();
            ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, null);
            return View(ctvm);
        }
        public ActionResult ListCouncilWithError()
        {
            int depID = (int)Session["DepartmentID"];
            Session["EditCouncilID"] = null; //Edit councilde farklı konseylere geçtiğimizde hoca isimlerinin düzgün gelmesini sağlıyor.
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            clearUnsaved();


            List<Council> listCouncils = (from x in db.Council where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.DepartmentID == depID select x).ToList();
            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();
            ctvm.tupCT = new Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>>(listCouncils, listTeachers, null);
            return View(ctvm);
        }
        public ActionResult AddEditMemberToCouncil(int? TeacherID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            int cID = (int)Session["EditCouncilID"];
            TeacherUserInfo addingTeacher = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
            Council addingCouncil = (from x in db.Council where x.CouncilID == cID && x.Approve == true select x).SingleOrDefault();
            Council fillingCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.Approve == true select x).ToList();

            addingTeacher.CouncilID = addingCouncil.CouncilID;
            db.SaveChanges();

            return RedirectToAction("EditCouncil");
        }
        public ActionResult AddEditMember(int? TeacherID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            int cID = (int)Session["EditCouncilID"];
            TeacherUserInfo addingTeacher = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
            Council addingCouncil = (from x in db.Council where x.CouncilID == cID && x.Approve == true select x).SingleOrDefault();
            Council fillingCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == null && x.Approve == true select x).ToList();

            addingTeacher.CouncilID = addingCouncil.CouncilID;
            db.SaveChanges();

            return RedirectToAction("EditCouncil");
        }
        public ActionResult EditCouncil(int? CouncilID)
        {
            int depID = (int)Session["DepartmentID"];
            List<StudentGrade> councilIsHere = (from x in db.StudentGrade where x.CouncilID == CouncilID select x).ToList();
            if (councilIsHere.Count != 0)
            {
                return RedirectToAction("ListCouncilWithError");
            }
            Council addingCouncil = (from x in db.Council where x.CouncilID == CouncilID select x).SingleOrDefault();
            Session["AnyCouncilSlc"] = null;
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();

            if (CouncilID != null && Session["EditCouncilID"] == null)
            {
                Session["EditCouncilID"] = CouncilID;
            }
            int cID = (int)Session["EditCouncilID"];
            Council slcCouncil = (from x in db.Council where x.CouncilID == CouncilID select x).SingleOrDefault();

            if (slcCouncil != null)
            {
                Session["Cname"] = slcCouncil.CouncilName;
            }

            List<TeacherUserInfo> listTeachersNull = (from x in db.TeacherUserInfo where x.CouncilID == null && x.Approve == true && x.DepartmentID == depID select x).ToList();
            //List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == CouncilID select x).ToList();

            Council isDeleting = (from x in db.Council where x.CouncilID == cID select x).SingleOrDefault();
            List<TeacherUserInfo> TeacherInCouncil = (from x in db.TeacherUserInfo where x.CouncilID == isDeleting.CouncilID && x.DepartmentID == depID select x).ToList();

            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();

            ctvm.tupEditCT = new Tuple<Council, List<TeacherUserInfo>, List<TeacherUserInfo>>(null, listTeachersNull, TeacherInCouncil);

            //isFull();
            return View(ctvm);
        }
        public ActionResult RemoveFromCouncil(int? TeacherID)
        {
            int depID = (int)Session["DepartmentID"];
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();

            List<StudentGrade> removeGrade = (from x in db.StudentGrade where x.TeacherID == TeacherID select x).ToList();
            for (int i = 0; i < removeGrade.Count; i++)
            {
                removeGrade[i].Approve = false;
            }
            var teacherWCouncil = (from x in db.TeacherUserInfo where x.TeacherID == TeacherID select x).SingleOrDefault();
            Council council = (from x in db.Council where x.CouncilID == teacherWCouncil.CouncilID select x).SingleOrDefault();
            CouncilTeacherViewModel ctvm = new CouncilTeacherViewModel();
            List<TeacherUserInfo> listTeachersNull = (from x in db.TeacherUserInfo where x.CouncilID == null && x.DepartmentID == depID select x).ToList();
            List<TeacherUserInfo> listTeachers = (from x in db.TeacherUserInfo where x.CouncilID == council.CouncilID && x.Approve == true && x.DepartmentID == depID select x).ToList();
            ctvm.tupEditCT = new Tuple<Council, List<TeacherUserInfo>, List<TeacherUserInfo>>(council, listTeachersNull, listTeachers);

            teacherWCouncil.CouncilID = null;
            db.SaveChanges();
            List<TeacherUserInfo> listTeachersIsEmpty = (from x in db.TeacherUserInfo where x.CouncilID == council.CouncilID && x.Approve == true && x.DepartmentID == depID select x).ToList();
            if (listTeachersIsEmpty.Count == 0)
            {
                council.isFilling = false;
                council.Approve = false;
                db.SaveChanges();
                return RedirectToAction("ListCouncil");
            }
            int cID = (int)Session["EditCouncilID"];
            Council addingCouncil = (from x in db.Council where x.CouncilID == cID select x).SingleOrDefault();

            return RedirectToAction("EditCouncil");

        }
        public ActionResult DeleteCouncil(int? CouncilID)
        {
            List<StudentGrade> councilIsHere = (from x in db.StudentGrade where x.CouncilID == CouncilID select x).ToList();
            if (councilIsHere.Count != 0)
            {
                return RedirectToAction("ListCouncilWithError");
            }
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            Council slcCouncil = (from x in db.Council where x.CouncilID == CouncilID select x).SingleOrDefault();
            List<TeacherUserInfo> teacherInCouncil = (from x in db.TeacherUserInfo where x.CouncilID == slcCouncil.CouncilID select x).ToList();
            List<StudentUserInfo> studentInCouncil = (from x in db.StudentUserInfo where x.CouncilID == CouncilID && x.Approve == true select x).ToList();
            List<InternInfo> internInCouncil = (from x in db.InternInfo where x.CouncilID == CouncilID select x).ToList();
            for (int i = 0; i < teacherInCouncil.Count; i++)
            {
                teacherInCouncil[i].CouncilID = null;
                db.SaveChanges();
            }
            for (int i = 0; i < studentInCouncil.Count; i++)
            {
                studentInCouncil[i].CouncilID = null;
                db.SaveChanges();
            }
            for (int i = 0; i < internInCouncil.Count; i++)
            {
                internInCouncil[i].CouncilID = null;
                db.SaveChanges();
            }

            slcCouncil.Approve = false;
            slcCouncil.isFilling = false;
            db.SaveChanges();
            return RedirectToAction("ListCouncil");
        }
        public ActionResult DeleteStudentCouncil(int? StudentID, int? CouncilID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            var findStudent = (from x in db.StudentUserInfo where x.StudentID == StudentID && x.Approve == true select x).SingleOrDefault();
            var findStudentInternInfo = (from x in db.InternInfo where x.StudentID == StudentID select x).SingleOrDefault();
            if (findStudent != null)
            {
                findStudent.CouncilID = null;
                findStudentInternInfo.CouncilID = null;
                db.SaveChanges();

            }
            Session["AnyCouncilSlc"] = null;
            return Redirect("DeleteStudentFromCouncil");
        }
        public ActionResult EditGradeCategory(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            clearUnsaved();
            var findCategory = db.GradeCategory.Find(id);
            Session["id"] = id;
            //if (id != 0)
            //{
            //    if (findCategory.StartDate != null)
            //    {
            //        Session["GradeStartDate"] = findCategory.StartDate.Substring(0, 16);
            //    }
            //    if (findCategory.LastDate != null)
            //    {
            //        Session["GradeLastDate"] = findCategory.LastDate.Substring(0, 16);
            //    }

            //    ViewBag.lastupdatemsg = true;

            //}
            if (findCategory.isGrading == true)
            {
                ViewBag.Send = false;
            }
            else
            {
                ViewBag.Send = true;
            }
            return View(findCategory);
        }
        public ActionResult DeleteCategory(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            clearUnsaved();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var deleteCategory = (from x in db.GradeCategory where x.GradeCategoryID == id select x).SingleOrDefault();
            deleteCategory.Approve = false;

            db.SaveChanges();

            if (deleteCategory == null)
            {
                //return HttpNotFound();
                return RedirectToAction("NotFound");
            }

            return RedirectToAction("ListGradeCategory");
        }
        public ActionResult AddGradeCategory(string args)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            int? res = 0;
            clearUnsaved();
            List<GradeCategory> allGrade = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
            for (int i = 0; i < allGrade.Count; i++)
            {
                res += allGrade[i].Percent;
            }



            return View();
        }
        public ActionResult ListGradeCategory()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = (int)Session["DepartmentID"];
            clearSessions();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            clearUnsaved();
            var listCategory = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<GradeCategory> slcCategory = (from x in db.GradeCategory where x.Approve == true && x.isGrading == true && x.DepartmentID == depID select x).ToList();



            return View(listCategory);
        }
        public ActionResult AllStudentsGrade()
        {
            int depID = (int)Session["DepartmentID"];
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearCouncilIDWOCStd();
            clearSessionAllButMainPage();
            double? avarage = 0;
            List<StudentUserInfo> thisDepStds = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
            List<InternInfo> allStudents = (from x in db.InternInfo where x.Approve == true select x).ToList();
            List<InternInfo> notGradedStudents = (from x in db.InternInfo where x.Approve == true && x.Grade == null select x).ToList();
            var result1 = allStudents.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID)).ToList();
            var result2 = notGradedStudents.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID)).ToList();
            for (int i = 0; i < result1.Count; i++)
            {
                if (result1[i].Grade != null)
                {
                    avarage += result1[i].Grade;
                }
            }
            avarage = avarage / result1.Count;
            if (result1.Count == result2.Count)
            {
                ViewBag.show = false;
            }
            else if (result1.Count != result2.Count)
            {
                ViewBag.show = true;
            }
            Session["avg"] = Math.Round((double)avarage, 2);
            return View(result1);
        }
        public ActionResult SendToFileUpload()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            int? id = (int)Session["id"];
            GradeCategory slcCategory = (from x in db.GradeCategory where x.GradeCategoryID == id select x).SingleOrDefault();
            slcCategory.isGrading = true;
            db.SaveChanges();

            return RedirectToAction("ListGradeCategory");
        }
        public ActionResult RemoveFromFileUpload()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearSessionAllButMainPage();
            clearCouncilIDWOCStd();
            int? id = (int)Session["id"];
            GradeCategory slcCategory = (from x in db.GradeCategory where x.GradeCategoryID == id select x).SingleOrDefault();
            slcCategory.isGrading = false;
            db.SaveChanges();
            return RedirectToAction("ListGradeCategory");
        }
        public ActionResult ConfirmIntern(int? id)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessionAllButMainPage();
            clearSessions();
            InternInfo slcStudent = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            slcStudent.Approve = true;
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == slcStudent.StudentID select x).SingleOrDefault();
            slcStudent.Approve = true;
            db.SaveChanges();

            var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
            var toAddress = new MailAddress(thisStd.StudentEmail);
            const string subject = "Welcome To HKU INTERNSHIP PROGRAM";
            string name = slcStudent.StudentName;
            string surname = thisStd.StudentSurname;
            string body = "Dear, " + name + " " + surname + ";" + "\n\nYour internship application is approved !" + "\n\nPlease go to our system and check it !" + "\n\n" + "http://mfstaj.hku.edu.tr/";

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
            return RedirectToAction("PendingStudents");
        }
         
        public ActionResult UnconfirmStudent(int? id)
        {
            int depID =(int) Session["DepartmentID"];
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            Session["AnyCouncilSlc"] = null;
            clearSessions();
            clearSessionAllButMainPage();
            InternInfo slcStudent = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == slcStudent.StudentID select x).SingleOrDefault();
            if(slcStudent.Grade == null)
            {
                slcStudent.Approve = false;
                db.SaveChanges();

                var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                var toAddress = new MailAddress(thisStd.StudentEmail);
                const string subject = "WARNING - HKU INTERNSHIP PROGRAM";
                string name = slcStudent.StudentName;
                string surname = slcStudent.StudentSurname;
                string body = "Dear, " + name + " " + surname + ";" + "\n\nYour internship application is reviewing again !";

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
            else
            {
                List<StudentUserInfo> thisDepStds = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
                List<InternInfo> listconfirmed = (from x in db.InternInfo where x.Approve == true select x).ToList();

                var result = listconfirmed.Where(p => thisDepStds.Any(p2 => p2.StudentID == p.StudentID));
                ViewBag.GradedStd = true;
                return View("ConfirmedStudents",result);
            }
            return RedirectToAction("ConfirmedStudents");
        }
        public ActionResult DeleteStudentFromCouncil(int? CouncilID)
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            clearSessionAllButMainPage();
            Session["AnyCouncilSlc"] = null;
            if (CouncilID != null)
            {
                Session["DeleteStudentCouncilID"] = CouncilID;
            }
            int? deleteStdCouncilID = (int)Session["DeleteStudentCouncilID"];
            if (deleteStdCouncilID == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Naughty");
            }
            Council thisCouncil = (from x in db.Council where x.CouncilID == deleteStdCouncilID select x).SingleOrDefault();
            List<StudentUserInfo> stdsOfCouncil = (from x in db.StudentUserInfo where x.CouncilID == thisCouncil.CouncilID && x.Approve == true select x).ToList();
            if (stdsOfCouncil == null)
            {
                return RedirectToAction("NotFound");
            }
            List<StudentGrade> councilIsHere = (from x in db.StudentGrade where x.CouncilID == deleteStdCouncilID select x).ToList();
            if (councilIsHere.Count != 0)
            {
                Session["isStdDeletable"] = 0;
            }
            else
            {
                Session["isStdDeletable"] = 1;
            }

            return View(stdsOfCouncil);
        }
        public ActionResult RejectStudentDetail(int? id)
        {
            int DepID = Convert.ToInt32(Session["DepartmentID"]);
            int CurrentSemester = semester();

            RejectedApplicationInternInfoViewModel raı = new RejectedApplicationInternInfoViewModel();
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == id && x.DepartmentID == DepID && x.SemesterID == CurrentSemester && x.Approve == true select x).SingleOrDefault();
            if(thisStd != null)
            {
                InternInfo thisStdIntern = (from x in db.InternInfo where x.StudentID == thisStd.StudentID select x).SingleOrDefault();
                List<RejectedApplications> listRejectedApp = (from x in db.RejectedApplications where x.StudentID == thisStd.StudentID && x.Approve == true select x).ToList();
                
                raı.tupIR = new Tuple<InternInfo, List<RejectedApplications>,StudentUserInfo>(thisStdIntern, listRejectedApp, thisStd);              
            }
            
            return View(raı);
        }
        //public ActionResult RejectStudent(int RejectID)
        //{
        //    if (Session["AdminID"] == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
        //    }
        //    clearSessionAllButMainPage();
        //    InternInfo isReject = (from x in db.InternInfo where x.StudentID == RejectID && x.isDelete == false select x).SingleOrDefault();
        //    StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == RejectID && x.Approve == true select x).SingleOrDefault();
        //    if (isReject != null)
        //    {
        //        isReject.isDelete = true;
        //        db.SaveChanges();
        //        RejectedApplications RejectedStudents = new RejectedApplications();
        //        RejectedStudents.StudentName = isReject.StudentName;
        //        RejectedStudents.StudentSurname = isReject.StudentSurname;
        //        RejectedStudents.CompanyName = isReject.CompanyName;
        //        RejectedStudents.CompanyAddress = isReject.CompanyAddress;
        //        RejectedStudents.CompanyCity = isReject.CompanyCity;
        //        RejectedStudents.CompanyPhone = isReject.CompanyPhone;
        //        RejectedStudents.AuthorizedPersonel = isReject.AuthorizedPersonel;
        //        RejectedStudents.Approve = true;
        //        RejectedStudents.StudentID = isReject.StudentID;
        //        RejectedStudents.SemesterID = isReject.SemesterID;
        //        RejectedStudents.DayOfRegister = isReject.DayOfRegistration;
        //        RejectedStudents.DayOfRejection = dateTimeNow();
        //        db.RejectedApplications.Add(RejectedStudents);
        //        db.SaveChanges();


        //        var fromAddress = new MailAddress("hku.intern1@gmail.com");
        //        var toAddress = new MailAddress(thisStd.StudentEmail);
        //        const string subject = "Welcome To HKU INTERNSHIP PROGRAM";
        //        string name = thisStd.StudentName;
        //        string surname = thisStd.StudentSurname;
        //        string body = "Dear, " + name + " " + surname + ";" + "\n\nYour internship application is rejected !" + "\n\nPlease go to our system and re-apply !" + "\n\n" + "http://localhost:51262/Login/Login";


        //        using (var smtp = new SmtpClient
        //        {
        //            Host = "smtp.gmail.com",
        //            Port = 587,
        //            EnableSsl = true,
        //            DeliveryMethod = SmtpDeliveryMethod.Network,
        //            UseDefaultCredentials = false,
        //            Credentials = new NetworkCredential(fromAddress.Address, "gbUf>Z/dNn^-n33e")
        //        })
        //        {
        //            using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
        //            {
        //                smtp.Send(message);
        //            }
        //        }
        //    }

        //    return RedirectToAction("PendingStudents");
        //}
        public ActionResult EditSemester()
        {
            if (Session["AdminID"] == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int depID = Convert.ToInt32(Session["AdminID"]);
            clearSessionAllButMainPage();
            Semester existingSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
            if (existingSemester != null)
            {
                if (existingSemester.SemesterName == "Fall Semester")
                {
                    ViewBag.fallSemester = true;
                }
                else if (existingSemester.SemesterName == "Spring Semester")
                {
                    ViewBag.springSemester = true;
                }
                else
                {
                    ViewBag.summerSemester = true;
                }

                ViewBag.DeadlineSemester = existingSemester.EndDate;
                ViewBag.DeadlineInternshipApp = existingSemester.EndDateInternship;
            }

            return View(existingSemester);
        }
        public ActionResult AddAdmin()
        {
            //if (Session["AdminID"] == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            //}
            return View();
        }

        //POST METHODS
        [HttpPost]
        public ActionResult AddAdmin(AdminUserInfo a)
        {
            //if (ModelState.IsValid)
            //{
            //    string email = t.TeacherEmail;
            //    string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TeacherName);
            //    string surname = t.TeacherSurname.ToUpper();
            //    var existingTeacher = (from x in db.TeacherUserInfo where x.TeacherEmail == email && x.Approve == true select x).SingleOrDefault();

            //    if (existingTeacher == null)
            //    {
            //        string newPass = GeneratePassword();
            //        TeacherUserInfo teacher = new TeacherUserInfo();
            //        teacher.TeacherName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TeacherName);
            //        teacher.TeacherSurname = t.TeacherSurname.Trim().ToUpper();
            //        teacher.TeacherEmail = t.TeacherEmail.Trim();
            //        teacher.TeacherPassword = newPass;
            //        teacher.TeacherRoom = t.TeacherRoom.Trim();
            //        teacher.DepartmentID = 1;
            //        teacher.CouncilID = null;
            //        teacher.DepartmentID = (int)Session["DepartmentID"];

            //        teacher.Approve = true;

            //        db.TeacherUserInfo.Add(teacher);
            //        db.SaveChanges();



            //        var fromAddress = new MailAddress("hku.intern1@gmail.com");
            //        var toAddress = new MailAddress(email);
            //        const string subject = "Welcome To HKU INTERNSHIP PROGRAM";

            //        string body = "Dear Lecturer, " + name + " " + surname + ";" + "\n\nWelcome to our internship system." + "\n\nYour log-in email is :" + email + "\n\nYour temporary password is : " + newPass + ""
            //                    + "\n\nPlease change it as soon as possible." + "\n\n" + "http://localhost:51262/Login/Login";
            //        using (var smtp = new SmtpClient
            //        {
            //            Host = "smtp.gmail.com",
            //            Port = 587,
            //            EnableSsl = true,
            //            DeliveryMethod = SmtpDeliveryMethod.Network,
            //            UseDefaultCredentials = false,
            //            Credentials = new NetworkCredential(fromAddress.Address, "gbUf>Z/dNn^-n33e")
            //        })
            //        {
            //            using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body })
            //            {
            //                smtp.Send(message);
            //            }
            //        }

            //        ViewBag.Control = true;
            //        ViewBag.success = "Teacher Added Successfully !";
            //        ModelState.Clear();
            //    }
            //    else
            //    {
            //        ViewBag.Control = false;
            //        ViewBag.fail = "Teacher was already added!";
            //    }

            //}
            //else
            //{
            //    ViewBag.Control = false;
            //    ViewBag.fail = "Please fill all the areas!";
            //}

            return View();
        }
        [HttpPost]
        public ActionResult BackToList()
        {
            return Redirect("ListCouncil");
        }
        [HttpPost]
        public ActionResult SaveCouncil(int? CouncilID)
        {
            Session["CouncilIDClear"] = null;
            Session["AnyCouncilSlc"] = 0;
            Council slcCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            if (slcCouncil != null)
            {
                slcCouncil.isFilling = false;
                slcCouncil.Approve = true;
                db.SaveChanges();
            }

            return Redirect("AddMemberToCouncil");
        }
        [HttpPost]
        public ActionResult EditIntern(int? id, bool? cbxApprove)
        {

            if (ModelState.IsValid)
            {
                InternInfo Interninfo2 = (from x in db.InternInfo where x.StudentID == id select x).SingleOrDefault();
                if (cbxApprove == true)
                {
                    Interninfo2.Approve = cbxApprove;
                }
                else if (cbxApprove == null)
                {
                    Interninfo2.Approve = false;
                }

                db.SaveChanges();
                ViewBag.InfoControl = true;
                ViewBag.InfoApprove = "Updated!";
                return View(Interninfo2); ;

            }
            return View();
        }
        [HttpPost]
        public ActionResult AdminEditInfo(AdminUserInfo admin, string Currentpass)
        {
            int id = Convert.ToInt32(Session["AdminID"]);
            string encryptedCurrentPass = MD5Hash(Currentpass);
            var dbAdmin = db.AdminUserInfo.Find(id);
            if (ModelState.IsValid)
            {
                if (Currentpass != null && admin.NewPassword != null && admin.ConfirmPassword != null)
                {
                    if (admin.NewPassword != admin.ConfirmPassword)
                    {
                        ViewBag.co = true;
                        ViewBag.mess = "New password and confirm password are not matching!";
                    }
                    else
                    {
                        if (dbAdmin.AdminPassword == encryptedCurrentPass)
                        {
                            if(ValidatePassword(admin.NewPassword) == true && ModelState.IsValid)
                            {
                                //int controlExtensionUntillEt = admin.AdminEmail.IndexOf('@');
                                //string lastPositionMail = admin.AdminEmail.Substring(controlExtensionUntillEt + 1);
                                //if (lastPositionMail == "hku.edu.tr")
                                //{
                                    dbAdmin.AdminName = admin.AdminName;
                                    dbAdmin.AdminSurname = admin.AdminSurname;
                                    //dbAdmin.AdminEmail = admin.AdminEmail;
                                    if(chkSpace(admin.NewPassword) != true)
                                {
                                    dbAdmin.AdminPassword = MD5Hash(admin.NewPassword);
                                }
                                else
                                {
                                    ViewBag.ThereIsSpaceInPass = true;
                                    return View();
                                }
                                    db.SaveChanges();
                                    ModelState.Clear();
                                    return RedirectToAction("Login", "Login");
                                //}
                                //else
                                //{
                                //    ViewBag.othermailtype = true;
                                //}
                               
                            }
                            else
                            {
                                ViewBag.ValidatePassError = true;                               
                            }                            
                        }
                        else
                        {
                            ViewBag.Control = true;
                            ViewBag.wrongCurrent = "Current password is wrong!";
                        }
                    }
                }
                else
                {
                    ViewBag.ControlNull = true;
                    ViewBag.NullPass = "You must fill all areas!";
                }
            }
            ModelState.Clear();
            return View(dbAdmin);
        }
        [HttpPost]
        public ActionResult AddTeacher(TeacherUserInfo t)
        {
            if (ModelState.IsValid)
            {
                string email = t.TeacherEmail;
                string name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TeacherName);
                string surname = t.TeacherSurname.ToUpper();
                var existingTeacher = (from x in db.TeacherUserInfo where x.TeacherEmail == email && x.Approve == true select x).SingleOrDefault();

                if (existingTeacher == null)
                {
                    string newPass = GeneratePassword();
                    TeacherUserInfo teacher = new TeacherUserInfo();
                    teacher.TeacherName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TeacherName);
                    teacher.TeacherSurname = t.TeacherSurname.Trim().ToUpper();
                    teacher.TeacherEmail = t.TeacherEmail.Trim();
                    teacher.TeacherPassword = newPass;
                    teacher.TeacherRoom = t.TeacherRoom.Trim();
                    teacher.DepartmentID = 1;
                    teacher.CouncilID = null;
                    teacher.DepartmentID = (int)Session["DepartmentID"];

                    teacher.Approve = true;

                    db.TeacherUserInfo.Add(teacher);
                    db.SaveChanges();



                    var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                    var toAddress = new MailAddress(email);
                    const string subject = "Welcome To HKU INTERNSHIP PROGRAM";

                    string body = "Dear Lecturer, " + name + " " + surname + ";" + "\n\nWelcome to our internship system." + "\n\nYour log-in email is :" + email + "\n\nYour temporary password is : " + newPass + ""
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

                    ViewBag.Control = true;
                    ViewBag.success = "Teacher Added Successfully !";
                    ModelState.Clear();
                }
                else
                {
                    ViewBag.Control = false;
                    ViewBag.fail = "Teacher was already added!";
                }

            }
            else
            {
                ViewBag.Control = false;
                ViewBag.fail = "Please fill all the areas!";
            }

            return View();
        }
        [HttpPost]
        public ActionResult EditTeacher(TeacherUserInfo t, int? id)
        {
            if (ModelState.IsValid)
            {
                TeacherUserInfo findTeacher = db.TeacherUserInfo.Find(id);
                if (findTeacher == null)
                {
                    //return HttpNotFound();
                    return RedirectToAction("NotFound");
                }

                findTeacher.TeacherName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.TeacherName);
                findTeacher.TeacherSurname = t.TeacherSurname.ToUpper();
                findTeacher.TeacherEmail = t.TeacherEmail;
                findTeacher.TeacherPassword = t.TeacherPassword;
                findTeacher.TeacherRoom = t.TeacherRoom;

                db.SaveChanges();
                ViewBag.Control = true;
                ViewBag.success = "Updated !";
                return View(findTeacher);
            }
            else
            {
                ViewBag.Control = false;
                ViewBag.fail = "Occured a problem !";
            }
            return View();
        }
        [HttpPost]
        public ActionResult CreateCouncil(string CouncilName)
        {
            int depID = (int)Session["DepartmentID"];
            string cname = CouncilName.ToUpper().Trim();
            int Thissemester = (int)semester();

            Council existingCouncil = (from x in db.Council where x.CouncilName == cname && x.DepartmentID == depID && x.SemesterID == Thissemester select x).SingleOrDefault();
            Council isCouncil = (from x in db.Council where x.CouncilName == cname && x.isDeleting == true select x).SingleOrDefault();

            if (ModelState.IsValid)
            {
                if (existingCouncil == null)
                {
                    if (CouncilName != "")
                    {

                        Council newCouncil = new Council();

                        newCouncil.CouncilName = cname;
                        newCouncil.Approve = false;
                        newCouncil.DepartmentID = depID;
                        newCouncil.SemesterID = semester();

                        db.Council.Add(newCouncil);
                        db.SaveChanges();

                        ViewBag.Control = true;
                        ViewBag.success = "Council created successfully!";
                        ModelState.Clear();
                    }
                    else
                    {
                        ViewBag.Control = false;
                        ViewBag.fail = "Please enter a valid name!";
                    }

                }
                else
                {
                    ViewBag.Control = false;
                    ViewBag.fail = "This name has taken!";
                }
            }
            else
            {
                ViewBag.fail = "Contact to Furkan & Vahdet & İbrahim";
            }

            if (isCouncil != null)
            {
                existingCouncil.isDeleting = false;
                db.SaveChanges();
                ViewBag.Control = true;
                ViewBag.success = "Council created successfully!";
                ModelState.Clear();
            }

            return View();
        }
        [HttpPost]
        public ActionResult saveStudent(int? studentID, int? CouncilID)
        {
            int depID = (int)Session["DepartmentID"];
            Council slcCouncil = (from x in db.Council where x.isFilling == true && x.DepartmentID == depID select x).SingleOrDefault();
            if (slcCouncil != null)
            {
                slcCouncil.isFilling = false;
                db.SaveChanges();
            }

            return Redirect("AddStudentToCouncil");

        }
        [HttpPost]
        public ActionResult EditGradeCategory(GradeCategory g, int? id, string StartDate, string EndDate)
        {
            if (ModelState.IsValid)
            {
                int depID = (int)Session["DepartmentID"];
                int? res = 0;
                GradeCategory findCategory = db.GradeCategory.Find(id);
                if (findCategory.isGrading == true)
                {
                    ViewBag.Remove = true;
                }
                else if (findCategory.isGrading == false)
                {
                    ViewBag.Send = true;
                }

                if (findCategory == null)
                {
                    //return HttpNotFound();
                    return RedirectToAction("NotFound");
                }

                List<GradeCategory> allCategories = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
                for (int i = 0; i < allCategories.Count; i++)
                {
                    res += allCategories[i].Percent;
                }

                res = res - findCategory.Percent;
                res = res + g.Percent;
                if (res <= 100)
                {
                    GradeCategory existingCategory = (from x in db.GradeCategory where x.GradeCategoryID == id && x.Approve == true select x).SingleOrDefault();
                    string oldFolderName = existingCategory.GradeName;
                    Department depname = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                    Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                    string path = depname.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString();
                    string oldFolder = Server.MapPath(string.Format("~/Archives/" + path + "/{0}/", existingCategory.GradeName.Trim()));



                    findCategory.GradeName = g.GradeName;
                    findCategory.Percent = g.Percent;
                    findCategory.Approve = true;
                    findCategory.DepartmentID = depID;
                    if (StartDate != "")
                    {
                        if ((Convert.ToDateTime(StartDate)) < DateTime.Now)
                        {
                            ViewBag.StartDateError = true;
                            return View(findCategory);
                        }
                        findCategory.StartDate = StartDate.Replace("T", " ") + ":00";
                    }
                    if (EndDate != "")
                    {
                        if ((Convert.ToDateTime(EndDate)) < (Convert.ToDateTime(StartDate)))
                        {
                            ViewBag.EndDateError = true;
                            return View(findCategory);
                        }
                        findCategory.LastDate = EndDate.Replace("T", " ") + ":00";
                    }
                    db.SaveChanges();
                    ViewBag.Control = true;
                    ViewBag.success = "Updated !";

                    GradeCategory newName = (from x in db.GradeCategory where x.GradeCategoryID == id && x.Approve == true select x).SingleOrDefault();


                    string foldername = newName.GradeName;
                    string newFolder = Server.MapPath(string.Format("~/Archives/" + path + "/{0}/", foldername.Trim()));

                    if (!Directory.Exists(newFolder))
                    {

                        Directory.Move(oldFolder, newFolder);
                        List<Document> listOldDocuments = (from x in db.Document where x.Approve == true select x).ToList();
                        int lenghtofpath = oldFolder.Length;
                        for (int i = 0; i < listOldDocuments.Count; i++)
                        {
                            if (listOldDocuments[i].FileLocation.Substring(0, lenghtofpath) == oldFolder)
                            {
                                string newDirectory = listOldDocuments[i].FileLocation.Replace(oldFolderName, foldername);
                                listOldDocuments[i].FileLocation = newDirectory;
                                db.SaveChanges();
                            }
                        }
                    }

                    return View(findCategory);
                }
                else
                {
                    ViewBag.Control1 = true;
                    ViewBag.fail = "You cannot set more than 100% !";
                }
            }
            else
            {
                ViewBag.Control = false;
                ViewBag.fail = "Occured a problem !";
            }
            return View();
        }
        [HttpPost]
        public ActionResult AddGradeCategory(GradeCategory gc, string StartDate, string EndDate)
        {
            if (ModelState.IsValid)
            {
                int depID = (int)Session["DepartmentID"];
                string gradeName = gc.GradeName;
                GradeCategory existingGrade = (from x in db.GradeCategory where x.GradeName == gradeName && x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                int? max = 0;
                List<GradeCategory> allCategory = (from x in db.GradeCategory where x.Approve == true && x.DepartmentID == depID select x).ToList();
                for (int i = 0; i < allCategory.Count; i++)
                {
                    max += allCategory[i].Percent;
                }

                if (existingGrade == null)
                {

                    if (max + gc.Percent <= 100)
                    {
                        GradeCategory newCategory = new GradeCategory();
                        newCategory.GradeName = gc.GradeName;
                        newCategory.Percent = gc.Percent;
                        newCategory.Approve = true;
                        newCategory.isGrading = false;
                        newCategory.DepartmentID = depID;
                        newCategory.SemesterID = semester();
                        if (StartDate != "")
                        {
                            if((Convert.ToDateTime(StartDate)) < DateTime.Now)
                            {
                                ViewBag.StartDateError = true;
                                return View();
                            }
                            newCategory.StartDate = StartDate.Replace("T", " ") + ":00";
                        }
                        if (EndDate != "")
                        {
                            if ((Convert.ToDateTime(EndDate)) < (Convert.ToDateTime(StartDate)))
                            {
                                ViewBag.EndDateError = true;
                                return View();
                            }
                            newCategory.LastDate = EndDate.Replace("T", " ") + ":00";
                        }
                        db.GradeCategory.Add(newCategory);
                        db.SaveChanges();
                        Department depname = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                        Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                        string path = depname.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString();
                        string foldername = gc.GradeName;
                        string folder = Server.MapPath(string.Format("~/Archives/" + path + "/{0}/", foldername.Trim()));

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);

                        }
                        ModelState.Clear();
                        ViewBag.Control = true;
                        ViewBag.success = "Added Succesfully !";

                    }
                    else if (max + gc.Percent > 100)
                    {
                        ViewBag.Control = false;
                        ViewBag.fail = "You cannot set more than 100% !";
                    }
                    else if (gc.GradeName == null || gc.Percent == null)
                    {
                        ViewBag.Control = false;
                        ViewBag.fail = "Please fill all the areas !";
                    }

                }
                else
                {
                    ViewBag.Control = false;
                    ViewBag.fail = "This category type is already exist!";
                }
            }

            return View();
        }
        [HttpPost]
        public ActionResult StartSemester(string EndDate, Semester newSem, string confirm, string Semester, string EndDateInternship)
        {
            int depID = (int)Session["DepartmentID"];
            if (Semester == "Empty")
            {
                ViewBag.SnameControl = true;
            }
            if (Semester != "Empty")
            {
                if ((EndDate != "" && newSem.EndDate > DateTime.Now) && (EndDateInternship != "" && newSem.EndDateInternship > DateTime.Now))
                {
                    if (confirm.ToLower() == "confirm")
                    {

                        Semester newSemester = new Semester();
                        newSemester.SemesterName = Semester;
                        newSemester.StartDate = dateTimeNow();
                        newSemester.EndDate = Convert.ToDateTime(EndDate);
                        newSemester.DepartmentID = depID;
                        newSemester.Approve = true;
                        newSemester.EndDateInternship = Convert.ToDateTime(EndDateInternship);

                        db.Semester.Add(newSemester);
                        db.SaveChanges();

                        Department thisDep = (from x in db.Department where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                        string foldername = thisDep.DepartmentName + " " + Semester + " " + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString();
                        string folder = Server.MapPath(string.Format("~/Archives/{0}/", foldername));
                        if (!Directory.Exists(Semester))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                        List<StudentUserInfo> allStudents = (from x in db.StudentUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
                        if (allStudents != null)
                        {
                            for (int i = 0; i < allStudents.Count; i++)
                            {
                                allStudents[i].SemesterID = semester();
                                db.SaveChanges();

                                //Send Mail each Student
                                string emailstd = allStudents[i].StudentEmail;
                                var toAddress = new MailAddress(emailstd);

                                const string subject = "Welcome To HKU INTERNSHIP PROGRAM";
                                string body = "Dear Student, " + allStudents[i].StudentName + " " + allStudents[i].StudentSurname + ";" + "\n\nWelcome to our internship system." + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1) +
                                                " Education has started. We wish you a good year.";
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
                            List<TeacherUserInfo> allTeachers = (from x in db.TeacherUserInfo where x.Approve == true && x.DepartmentID == depID select x).ToList();
                            for (int i = 0; i < allTeachers.Count; i++)
                            {
                                string emailtch = allTeachers[i].TeacherEmail;
                                var toAddress = new MailAddress(emailtch);
                                const string subject = "Welcome To HKU INTERNSHIP PROGRAM";
                                string body = "Dear Lecturer, " + allTeachers[i].TeacherName + " " + allTeachers[i].TeacherSurname + ";" + "\n\n\tWelcome to our internship system." + DateTime.Now.Year + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1) +
                                                " Education has started. We wish you a good year.";
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
                        }
                        return RedirectToAction("AdminMainPage");
                    }
                    else
                    {
                        ViewBag.ConfirmControl = true;
                        return View();
                    }
                }
                else
                {
                    ViewBag.EdateControl = true;
                    return View();
                }
            }
            else
            {
                ViewBag.SnameControl = true;
                return View();
            }


        }
        [HttpPost]
        public ActionResult EditSemester(string EndDate, Semester newSem, string confirm, string Semester, string EndDateInternship)
        {

            int depID = (int)Session["DepartmentID"];

            if ((EndDate != "" && newSem.EndDate > DateTime.Now) && (EndDateInternship != "" && newSem.EndDateInternship > DateTime.Now))
            {
                if (confirm.ToLower() == "confirm")
                {
                    Semester existingSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                    if (existingSemester != null)
                    {
                        existingSemester.SemesterName = Semester;
                        existingSemester.StartDate = dateTimeNow();
                        existingSemester.EndDate = Convert.ToDateTime(EndDate);
                        existingSemester.DepartmentID = depID;
                        existingSemester.EndDateInternship = Convert.ToDateTime(EndDateInternship);

                        db.SaveChanges();
                    }
                }
            }

            if (confirm == "" || EndDate == "" || EndDateInternship == "")
            {

                Semester existingSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
                ViewBag.fillAllAreas = true;
                return View(existingSemester);
            }

            return RedirectToAction("AdminMainPage");
        }
        [HttpPost]
        public ActionResult AdminMainPage(HttpPostedFileBase file)
        {
            // uploadCalendar(file);
            int depID = Convert.ToInt32(Session["DepartmentID"]);
            Semester thisSemester = (from x in db.Semester where x.Approve == true && x.DepartmentID == depID select x).SingleOrDefault();
            Department thisDepertment = (from x in db.Department where x.DepartmentID == depID && x.Approve == true select x).SingleOrDefault();

            if (file != null)
            {
                string fileName = thisDepertment.DepartmentName + " " + thisSemester.SemesterName + " " + DateTime.Now.Year.ToString() + "-" + (Convert.ToInt32(DateTime.Now.Year) + 1).ToString() + ".pdf";
                var path = Path.Combine(Server.MapPath("~/Internship Calendar/"), fileName);

                string flocation = "PDF File/";
                string PathString = System.IO.Path.Combine(flocation, fileName);
                string fileExtend = System.IO.Path.GetExtension(Path.GetFileName(file.FileName));

                if (fileExtend.ToLower() == ".pdf")
                {
                    file.SaveAs(path);
                    Session["NullFile"] = 0;
                }
                else
                {
                    Session["NullFile"] = 2;
                }
                
            }
            else
            {

                Session["NullFile"] = 1;
            }
            return RedirectToAction("AdminMainPage");
        }
        [HttpPost]
        public ActionResult RejectStudentDetail(int? id,string rejectmsg)
        {
            int DepID = Convert.ToInt32(Session["DepartmentID"]);
            int CurrentSemester = semester();
            RejectedApplicationInternInfoViewModel raı = new RejectedApplicationInternInfoViewModel();
            StudentUserInfo thisStd = (from x in db.StudentUserInfo where x.StudentID == id && x.DepartmentID == DepID && x.SemesterID == CurrentSemester && x.Approve == true select x).SingleOrDefault();
            if (thisStd != null)
            {                       
                InternInfo thisStdIntern = (from x in db.InternInfo where x.StudentID == thisStd.StudentID select x).SingleOrDefault();
                List<RejectedApplications> listRejectedApp = (from x in db.RejectedApplications where x.StudentID == thisStd.StudentID && x.Approve == true select x).ToList();
                raı.tupIR = new Tuple<InternInfo, List<RejectedApplications>,StudentUserInfo>(thisStdIntern, listRejectedApp, thisStd);

                if (rejectmsg == "")
                {
                    ViewBag.textAreaNull = true;
                    return View(raı);
                }
                else
                {                    
                    RejectedApplications RejectedStudents = new RejectedApplications();
                    RejectedStudents.RejectedMessage = rejectmsg;
                    RejectedStudents.StudentName = thisStdIntern.StudentName;
                    RejectedStudents.StudentSurname = thisStdIntern.StudentSurname;
                    RejectedStudents.CompanyName = thisStdIntern.CompanyName;
                    RejectedStudents.CompanyAddress = thisStdIntern.CompanyAddress;
                    RejectedStudents.CompanyCity = thisStdIntern.CompanyCity;
                    RejectedStudents.CompanyPhone = thisStdIntern.CompanyPhone;
                    RejectedStudents.AuthorizedPersonel = thisStdIntern.AuthorizedPersonel;
                    RejectedStudents.Approve = true;
                    RejectedStudents.StudentID = thisStdIntern.StudentID;
                    RejectedStudents.SemesterID = thisStdIntern.SemesterID;
                    RejectedStudents.DayOfRegister = thisStdIntern.DayOfRegistration;
                    RejectedStudents.DayOfRejection = dateTimeNow();

                    db.RejectedApplications.Add(RejectedStudents);
                    db.SaveChanges();

                    thisStdIntern.isDelete = true;
                    db.SaveChanges();

                    var fromAddress = new MailAddress("coopengineering@hku.edu.tr");
                    var toAddress = new MailAddress(thisStd.StudentEmail);
                    const string subject = "REJECTED APPLICATION - HKU INTERNSHIP PROGRAM";
                    string name = thisStd.StudentName;
                    string surname = thisStd.StudentSurname;
                    string body = "Dear, " + name + " " + surname + ";" + "\n\nYour internship application is rejected !"+"\n\nThe reason is: "+rejectmsg+"." + "\n\nPlease go to our system and re-apply !" + "\n\n" + "http://mfstaj.hku.edu.tr/";


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

                ViewBag.RejectSuccess = true;
                return RedirectToAction("PendingStudents");
            }
            else
            {
                ViewBag.RejectSuccess = false;
            }

            return View(raı);
        }

        //DÜZ METHODS
        public void clearCouncilIDWOCStd()
        {
            if(Session["AdminID"] == null)
            {
                 new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Forbidden!");
            }
            int DepID = Convert.ToInt32(Session["DepartmentID"]);

            List<StudentUserInfo> allStudents = (from x in db.StudentUserInfo where x.Approve == true  && x.DepartmentID == DepID select x).ToList();
            List<InternInfo> allStudentsInternInfo = (from x in db.InternInfo where x.Approve == true select x).ToList();
            List<Council> deletedCouncil = (from x in db.Council where x.Approve == false select x).ToList();
            for (int i = 0; i < allStudents.Count; i++)
            {
                for (int j = 0; j < deletedCouncil.Count; j++)
                {
                    if (allStudents[i].CouncilID == deletedCouncil[j].CouncilID)
                    {
                        allStudents[i].CouncilID = null;
                        allStudentsInternInfo[i].CouncilID = null;
                        db.SaveChanges();
                    }
                }
            }
        }
        public void clearSessions()
        {
            Session["slcCouncil"] = null;
            Session["slcCouncilName"] = null;
            Session["CouncilID"] = null;
            Session["count"] = null;
            Session["count1"] = null;
        }
        public ActionResult goToAddMember(int? CouncilID)
        {
            if (Session["AnyCouncilSlc"] == null)
            {
                Session["AnyCouncilSlc"] = 0;
            }
            Council fillingCouncil = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            Council slcCouncil = (from x in db.Council where x.CouncilID == CouncilID select x).SingleOrDefault();
            if (slcCouncil != null)
            {

                ViewBag.selectedCouncil = slcCouncil.CouncilID;
                ViewBag.selected = slcCouncil.CouncilName;
                slcCouncil.isFilling = true;

                db.SaveChanges();
                if (CouncilID != null)
                {
                    Session["AnyCouncilSlc"] = 1;
                }
            }
            return RedirectToAction("AddMemberToCouncil");
        }
        public void clearUnsaved()
        {
            Session["AnyCouncilSlc"] = null;
            Council willBeDeleted = (from x in db.Council where x.isFilling == true select x).SingleOrDefault();
            if (willBeDeleted != null)
            {
                List<TeacherUserInfo> willBeNull = (from x in db.TeacherUserInfo where x.CouncilID == willBeDeleted.CouncilID select x).ToList();
                for (int i = 0; i < willBeNull.Count; i++)
                {
                    willBeNull[i].CouncilID = null;
                    db.SaveChanges();
                }
                willBeDeleted.Approve = false;
                willBeDeleted.isFilling = false;
                db.SaveChanges();
            }
        }
        public string GeneratePassword()
        {
            string PasswordLength = "8";
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

            return thisSemester.SemesterID;
        }
        public void clearSessionAllButMainPage()
        {
            Session["NullFile"] = 991;
        }
        //Anasayfada upload calendar func.      
        public ActionResult uploadCalendar(HttpPostedFileBase file)
        {
           
            return RedirectToAction("AdminMainPage");
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
