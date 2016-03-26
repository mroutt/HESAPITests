using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class HESVisitorScanResponse
    {
        private DateTime? _expDate = null;
        public byte[] Photo { get; set; }
        public string Firstname { get; set; }
        public string Middlename { get; set; }
        public string Lastname { get; set; }
        public string Sponsorg { get; set; }
        public DateTime? Expirationdate
        {
            get { return _expDate; }
            set
            {
                if (value.HasValue)
                    _expDate = value.Value.Date;
            }
        }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string ExtendedInfo { get; set; }
    }
}
