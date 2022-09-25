using System;
using TEPSClientInstallService_UpdateUtility.Classes;

namespace TEPSClientInstallService_UpdateUtility
{
    internal class Program
    {
        private loggingClass loggingClass = new loggingClass();
        private agentUpdateClass agentUpdateClass = new agentUpdateClass();

        private static void Main(string[] args)
        {
            Program program = new Program();

            program.loggingClass.initializeNLogLogger();

            program.agentUpdateClass.updateAPICheck();

            Console.ReadKey();
        }
    }
}