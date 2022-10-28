using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TEPSClientInstallService_UpdateUtility.Classes;

namespace TEPSClientInstallService_UpdateUtility
{
    internal class Program
    {
        private loggingClass loggingClass = new loggingClass();
        private agentUpdateClass agentUpdateClass = new agentUpdateClass();
        private selfUpdateClass selfUpdateClass = new selfUpdateClass();
        private serviceClass serviceClass = new serviceClass();

        private static async Task Main(string[] args)
        {
            Directory.CreateDirectory(@"C:\Services\Tyler-Client-Install-Agent");
            Directory.CreateDirectory(@"C:\ProgramData\Tyler Technologies\Public Safety\Tyler-Client-Install-Agent");

            Program program = new Program();

            program.loggingClass.initializeNLogLogger();

            try
            {
                await program.utilityUpdater();
            }
            catch(Exception ex)
            {

            }

            Thread.Sleep(30000);

            try
            {
                await program.agentUpdater();
            }
            catch(Exception ex)
            {

            }

            Thread.Sleep(30000);

            if (program.serviceClass.getServiceStatus("TEPS Automated Client Install Agent") == "stopped")
            {
                program.serviceClass.startService($"TEPS Automated Client Install Agent");
            }
        }

        private async Task agentUpdater()
        {
            await agentUpdateClass.updateAPICheckAsync();
        }

        private async Task utilityUpdater()
        {
            await selfUpdateClass.updateAPICheckAsync();
        }
    }
}