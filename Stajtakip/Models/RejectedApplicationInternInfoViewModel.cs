using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Stajtakip.Models.EntityFramework;

namespace StajTakip.Models
{
    public class RejectedApplicationInternInfoViewModel
    {
        public Tuple<InternInfo, List<RejectedApplications>,StudentUserInfo> tupIR { get; set; }        
    }
}