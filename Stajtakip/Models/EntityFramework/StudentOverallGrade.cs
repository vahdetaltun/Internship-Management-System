//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Stajtakip.Models.EntityFramework
{
    using System;
    using System.Collections.Generic;
    
    public partial class StudentOverallGrade
    {
        public int OverallGradeID { get; set; }
        public Nullable<int> StudentID { get; set; }
        public Nullable<double> Grade { get; set; }
        public Nullable<bool> Approve { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public Nullable<int> SemesterID { get; set; }
    }
}