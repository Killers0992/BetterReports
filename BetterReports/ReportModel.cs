using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterReports
{
    public class ReportModel
    {
        public int TicketID { get; set; }
        public byte Status { get; set; }
        public string IssuerID { get; set; }
        public string IssuerNICK { get; set; }
        public string TargetID { get; set; }
        public string TargetNICK { get; set; }
        public DateTime IssueTime { get; set; }
        public string Reason { get; set; }
        public string ServerIP { get; set; }
        public ushort ServertPort { get; set; }
        public string Response { get; set; }
        public string ClosedbyID { get; set; }
        public string ClosedbyNICK { get; set; }
        public DateTime ClosedTime { get; set; }
    }
}
