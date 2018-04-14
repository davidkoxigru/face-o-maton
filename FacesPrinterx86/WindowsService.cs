using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.ServiceModel.Dispatcher;
using System.ServiceProcess;
using System.Threading;

namespace FacesPrinterx86
{
    public partial class WindowsService : ServiceBase
    {
        ServiceHost _serviceHost = null;

        public WindowsService()
        {
            InitializeComponent();
        }

        public void StartWCFServices()
        {
            var serviceAddress = Properties.Settings.Default.ServicesAddress.Replace("localhost", Environment.MachineName);
            WinConsole.WriteLine("Adresse printer server: " + serviceAddress);
            try
            {
                _serviceHost = new ServiceHost(typeof(FacesPrinterx86Server));
                NetNamedPipeBinding binding = new NetNamedPipeBinding(NetNamedPipeSecurityMode.None);
                _serviceHost.AddServiceEndpoint(typeof(IFacesPrinterx86Interfaces), binding, serviceAddress);
                _serviceHost.Open();
            }

            catch (Exception ex)
            {
                WinConsole.WriteLine(ex);
                throw;
            }
        }
        
        public void StopWCFServices()
        {
            try
            {
                _serviceHost.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _serviceHost.Abort();
                throw;
            }
        }
        
        protected override void OnStart(string[] args)
        {
            // VMWare machines are slow and can cause timeouts of SCM
            RequestAdditionalTime(60000);

            StartWCFServices();

            // call the base class so it has a chance
            // to perform any work it needs to
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            // VMWare machines are slow and can cause timeouts of SCM
            RequestAdditionalTime(60000);

            StopWCFServices();

            // call the base class 
            base.OnStop();
        }
    }
}
