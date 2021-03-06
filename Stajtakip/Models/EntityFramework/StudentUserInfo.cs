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
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    public partial class StudentUserInfo
    {
        public int StudentID { get; set; }
        public string StudentSchoolID { get; set; }
        public string StudentName { get; set; }
        public string StudentSurname { get; set; }
        public string StudentEmail { get; set; }
        public string StudentPassword { get; set; }
        public Nullable<int> DepartmentID { get; set; }
        public string StudentPhoneNumber { get; set; }
        public Nullable<bool> Approve { get; set; }
        public Nullable<int> CouncilID { get; set; }
        [DisplayName("New Password")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DisplayName("Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public Nullable<bool> isAbroad { get; set; }
        public Nullable<int> SemesterID { get; set; }
        public string NewPasswordnewPassword { get; set; }

        [DisplayName("Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmPasswordnewPassword { get; set; }

        [DisplayName("activationLink")]

        public string activationLink { get; set; }
    }
}
