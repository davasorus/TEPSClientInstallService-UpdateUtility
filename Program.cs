using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEPSClientInstallService_UpdateUtility.Classes;

namespace TEPSClientInstallService_UpdateUtility
{
    internal class Program
    {
        private loggingClass loggingClass = new loggingClass();

        static void Main(string[] args)
        {
            Program program = new Program();

            program.loggingClass.initializeNLogLogger();
        }
    }
}
