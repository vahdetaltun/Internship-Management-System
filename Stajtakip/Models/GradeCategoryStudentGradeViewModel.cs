using Stajtakip.Models.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StajTakip.Models
{
    public class GradeCategoryStudentGradeViewModel
    {
        public Tuple<List<GradeCategory>, List<StudentGrade>> tupGCSG { get; set; }
    }
}