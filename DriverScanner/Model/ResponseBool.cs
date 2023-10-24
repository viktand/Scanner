using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverScanner.Model
{
    public class ResponseBool
    {
        //{"value":false,"statusCode":200,"contentType":null}
        public bool value { get; set; }

        public int statusCode { get; set; }

        public object contentType { get; set; }
    }
}
