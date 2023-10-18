using System;
using System.IO;

namespace DriverScanner
{
    public static class Logger
    {
        public static void Log(string message)
        {
            var text = DateTime.Now.ToString() + " : Log : " + message + "\r\n";
            File.AppendAllText("log.txt", text);
        }

        public static void Error(string message)
        {
            var text = DateTime.Now.ToString() + " : Error : " + message + "\r\n";
            File.AppendAllText("log.txt", text);
        }
    }
}
