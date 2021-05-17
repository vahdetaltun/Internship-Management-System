using Stajtakip.Models.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StajTakip.Models
{
    public class CouncilTeacherViewModel
    {

        public Tuple<List<Council>, List<TeacherUserInfo>, List<TeacherUserInfo>> tupCT { get; set; }
        public Tuple<Council,List<TeacherUserInfo>, List<TeacherUserInfo>> tupEditCT { get; set; }
        public IEnumerable<TeacherUserInfo> t { get; set; }
        public int TeacherID { get; set; }
        public string TeacherName { get; set; }
        public string TeacherSurname { get; set; }
        public string TeacherEmail { get; set; }
        public string TeacherPassword { get; set; }
        public string TeacherRoom { get; set; }
        public Nullable<bool> Approve { get; set; }
        public Nullable<int> CouncilID { get; set; }
        public Nullable<bool> isFilling { get; set; }       
        
    }
}