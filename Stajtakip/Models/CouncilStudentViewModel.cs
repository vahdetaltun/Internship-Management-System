using Stajtakip.Models.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StajTakip.Models
{
    public class CouncilStudentViewModel
    {
        //public Tuple<List<Council>, List<StudentUserInfo>> tup4 { get; set; }
        //public IEnumerable<StudentUserInfo> s { get; set; }
        //public Tuple<List<Council>, List<TeacherUserInfo>, List<StudentUserInfo>> tup2 { get; set; }
        public Tuple<List<Council> ,List<TeacherUserInfo>,List<InternInfo>> tupCTS { get; set; }
        public Nullable<int> CouncilID { get; set; }
        public Nullable<bool> isFilling { get; set; }     
        public Nullable<int> StudentID { get; set; }
        public string StudentSchoolID { get; set; }
        public string StudentName { get; set; }
        public string StudentSurname { get; set; }
        public string StudentEmail { get; set; }
    }
}