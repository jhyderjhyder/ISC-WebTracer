using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISC_WebTracer.logger
{
    public class Logger
    {
        public Logger() { }
        public void log(String prefix, Object o)
        {
            Console.WriteLine(DateTime.Now + "::" + prefix + " ::" + o);
        }
    }
}
