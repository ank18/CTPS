using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogConfigLayer
{
    public class LogEntity
    {
        public string Type { get; set; }
        public DateTime When { get; set; }
        public string Source { get; set; }
        public object Message { get; set; }
        public object Exception { get; set; }
    }
}
