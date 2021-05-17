using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Stajtakip.Models.EntityFramework;


namespace StajTakip.Models
{
    public class GradeDocumentViewModel
    {
        public IEnumerable<Document> docgrade { get; set; }
        public IEnumerable<Comment> comment { get; set; }
        public string sendingcomment { get; set; }
        [Required(ErrorMessage ="This field cannot be empty!")]
        public Nullable<double> Grade { get; set; }


    }
}