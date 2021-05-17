using Stajtakip.Models.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StajTakip.Models
{
    public class CommentDocumentViewModel
    {
        public Tuple<List<Document>, List<Comment>, Comment> tupCD { get; set; }
        public string comment { get; set; }
    }
}