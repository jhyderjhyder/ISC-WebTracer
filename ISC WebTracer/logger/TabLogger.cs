using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISC_WebTracer.logger
{
    public class TabLogger: Logger
    {
        public void log(String prefix, Object o)
        {
            String data = DateTime.Now + "\t" + prefix + "\t" + o;
            Console.WriteLine(data);
            StreamWriter logWriter = new System.IO.StreamWriter("./trace.log", true);
            logWriter.WriteLine(data);
            logWriter.Flush();
            logWriter.Close();

        }
    }
}
