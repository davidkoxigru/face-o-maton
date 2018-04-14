using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Dispatcher;
using System.ServiceProcess;
using System.Windows.Forms;
using System.ServiceModel.Discovery;

namespace SoundSyncx86
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // force current working directory to executable folder
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

            try
            {
                ProjectInstaller projectInstaller = new ProjectInstaller();
                Assembly self = Assembly.GetExecutingAssembly();
                String ServiceName = projectInstaller.Service.ServiceName;
                using (ServiceController serviceController = new ServiceController(ServiceName))
                {
                    WinConsole.Visible = true;

                    using (var services = new WindowsService())
                    {
                        services.StartWCFServices();

                        WinConsole.WriteLine(Properties.Resources.Quit);
                        WinConsole.Read();

                        services.StopWCFServices();
                    }
                }
            }
            catch (Exception ex)
            {
                if (Debugger.IsAttached == true)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(Properties.Resources.PressKey);
                    Console.ReadKey();
                }
            }

            Environment.Exit(0);
        }
    }
}
