namespace DriverScanner.Model
{
    public class ResponseString
    {
        //{"value":"vvvv","statusCode":200,"contentType":null}
        public string value { get; set; }

        public int statusCode { get; set; }

        public object contentType { get; set; }
    }
}
