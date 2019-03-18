using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApptsDb
{
    public class AppointmentDisplay
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public string Contact { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public System.DateTime Starts { get; set; }
        public System.DateTime Ends { get; set; }
    }
    
}
