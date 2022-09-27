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

        private static async Task Main(string[] args)
        {
            Program program = new Program();

            program.loggingClass.initializeNLogLogger();

            await program.utilityUpdater();

            Thread.Sleep(30000);

            await program.agentUpdater();

            Thread.Sleep(30000);
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