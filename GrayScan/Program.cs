using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrayScan
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Run process");
            var scaner = new ScanCore();
            scaner.GoAuto();
            Console.ReadKey();
            scaner.Close();
        }
    }
}
