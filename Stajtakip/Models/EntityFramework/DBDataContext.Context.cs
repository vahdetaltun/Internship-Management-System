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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class StajTakipEntities : DbContext
    {
        public StajTakipEntities()
            : base("name=StajTakipEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<AdminUserInfo> AdminUserInfo { get; set; }
        public virtual DbSet<Comment> Comment { get; set; }
        public virtual DbSet<Council> Council { get; set; }
        public virtual DbSet<Department> Department { get; set; }
        public virtual DbSet<Document> Document { get; set; }
        public virtual DbSet<Faculty> Faculty { get; set; }
        public virtual DbSet<GradeCategory> GradeCategory { get; set; }
        public virtual DbSet<GradeTypeAvarage> GradeTypeAvarage { get; set; }
        public virtual DbSet<InternInfo> InternInfo { get; set; }
        public virtual DbSet<RejectedApplications> RejectedApplications { get; set; }
        public virtual DbSet<Semester> Semester { get; set; }
        public virtual DbSet<StudentGrade> StudentGrade { get; set; }
        public virtual DbSet<StudentOverallGrade> StudentOverallGrade { get; set; }
        public virtual DbSet<StudentUserInfo> StudentUserInfo { get; set; }
        public virtual DbSet<TeacherUserInfo> TeacherUserInfo { get; set; }
    }
}
