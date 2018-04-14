using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Windows.Forms;


namespace SoundSyncx86
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(serviceInstaller.ServiceName))
                EventLog.CreateEventSource(serviceInstaller.ServiceName, Properties.Resources.Application);
        }

        public ServiceInstaller Service
        {
            get { return serviceInstaller; }
        }

        private void serviceInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            // stop the service
            using (ServiceController serviceController = new ServiceController(serviceInstaller.ServiceName))
            {
                try
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                }
                catch (System.InvalidOperationException)
                {
                    // service is not accessible
                }
            }
        }

        private void serviceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            // stop the service
            using (ServiceController serviceController = new ServiceController(serviceInstaller.ServiceName))
            {
                try
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 1, 0));
                }
                catch (System.InvalidOperationException)
                {
                    // service is not accessible
                }
            }
        }

        private void serviceInstaller_Committing(object sender, InstallEventArgs e)
        {
            ManagementBaseObject InParam = null;
            ManagementBaseObject OutParam = null;
            try
            {
                // Windows Interact W/Desktop...        
                ConnectionOptions coOptions = new ConnectionOptions();
                coOptions.Impersonation = ImpersonationLevel.Impersonate;
                ManagementScope mgmtScope = new System.Management.ManagementScope(@"root\CIMV2", coOptions);
                mgmtScope.Connect();
                using (ManagementObject wmiService = new ManagementObject(string.Format(Properties.Resources.Win32ServiceName, serviceInstaller.ServiceName)))
                {
                    InParam = wmiService.GetMethodParameters(Properties.Resources.ParamInfo);
                    InParam[Properties.Resources.DesktopInteract] = true;
                    OutParam = wmiService.InvokeMethod(Properties.Resources.ParamInfo, InParam, null);
                }

                // set actions on recovery in order to restart the service 3 times
                var fa = new List<FailureAction>();
                fa.Add(new FailureAction(RecoverAction.Restart, 2000));
                fa.Add(new FailureAction(RecoverAction.Restart, 2000));
                fa.Add(new FailureAction(RecoverAction.Restart, 2000));
                SetRestartOnFailureRecovery(fa, "", "");

            }
            catch (Exception Error)
            {
                EventLog.WriteEntry(serviceInstaller.ServiceName, Error.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                if (InParam != null) InParam.Dispose();
                if (OutParam != null) OutParam.Dispose();
            }
        }

        private void serviceProcessInstaller_BeforeInstall(object sender, InstallEventArgs e)
        {
            String domain = Context.Parameters[Properties.Resources.DomainParam];
            String userName = Context.Parameters[Properties.Resources.UserNameParam];
            String password1 = Context.Parameters[Properties.Resources.Password1Param];
            String password2 = Context.Parameters[Properties.Resources.Password2Param];

            if (String.IsNullOrEmpty(domain) == false)
            {
                if (password1 != password2)
                    password1 = null;

                serviceProcessInstaller.Account = ServiceAccount.User;
                serviceProcessInstaller.Username = domain + @"\" + userName;
                serviceProcessInstaller.Password = Properties.Resources.ServiceProcessInstallerPassword;
            }
            else
            {
                String domainAccount = Context.Parameters[Properties.Resources.User];
                String password = Context.Parameters[Properties.Resources.Password];

                serviceProcessInstaller.Account = ServiceAccount.User;
                serviceProcessInstaller.Username = domainAccount;
                serviceProcessInstaller.Password = password;
            }
        }

        #region SetRestartOnFailureRecovery

        #region NT Service Win32 Interop

        // The struct for setting the service failure actions
        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_FAILURE_ACTIONS
        {
            public int dwResetPeriod;
            public string lpRebootMsg;
            public string lpCommand;
            public int cActions;
            public int lpsaActions;
        }

        // Win32 function to open the service control manager
        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenSCManager(string lpMachineName, string lpDatabaseName, int dwDesiredAccess);

        // Win32 function to open a service instance
        [DllImport("advapi32.dll")]
        public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);

        // Win32 function to lock the service database to perform write operations.
        [DllImport("advapi32.dll")]
        public static extern IntPtr LockServiceDatabase(IntPtr hSCManager);

        // Win32 function to change the service config for the failure actions.
        [DllImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2")]
        public static extern bool ChangeServiceFailureActions(IntPtr hService, int dwInfoLevel,
            [MarshalAs(UnmanagedType.Struct)] ref SERVICE_FAILURE_ACTIONS lpInfo);

        // Win32 function to close a service related handle.
        [DllImport("advapi32.dll")]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        // Win32 function to unlock the service database.
        [DllImport("advapi32.dll")]
        public static extern bool UnlockServiceDatabase(IntPtr hSCManager);

        // The infamous GetLastError() we have all grown to love
        [DllImport("kernel32.dll")]
        public static extern int GetLastError();

        // Some Win32 constants I'm using in this app

        private const int SC_MANAGER_ALL_ACCESS = 0xF003F;
        private const int SERVICE_ALL_ACCESS = 0xF01FF;
        private const int SERVICE_CONFIG_FAILURE_ACTIONS = 0x2;
        private const int SERVICE_NO_CHANGE = -1;
        private const int ERROR_ACCESS_DENIED = 5;

        #endregion

        // Enum for recovery actions (correspond to the Win32 equivalents )
        public enum RecoverAction
        {
            None = 0, Restart = 1, Reboot = 2, RunCommand = 3
        }

        // Class to represent a failure action which consists of a recovery
        // action type and an action delay
        public class FailureAction
        {
            public FailureAction(RecoverAction actionType, int actionDelay)
            {
                this.Type = actionType;
                this.Delay = actionDelay;
            }

            public RecoverAction Type { get; set; }
            public int Delay { get; set; }
        }

        private bool SetRestartOnFailureRecovery(List<FailureAction> FailureActions, String RunProgram, String RebootMessage)
        {
            // We've got work to do
            IntPtr scmHndl = IntPtr.Zero;
            IntPtr svcHndl = IntPtr.Zero;
            IntPtr tmpBuf = IntPtr.Zero;
            IntPtr svcLock = IntPtr.Zero;

            // Err check var
            bool rslt = false;


            // Place all our code in a try block
            try
            {

                // Open the service control manager
                scmHndl = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);

                if (scmHndl.ToInt32() <= 0)
                {
                    EventLog.WriteEntry(serviceInstaller.ServiceName, Properties.Resources.ScmHndlInfo, EventLogEntryType.Error);
                    return false;
                }

                // Lock the Service Database
                svcLock = LockServiceDatabase(scmHndl);

                if (svcLock.ToInt32() <= 0)
                {

                    EventLog.WriteEntry(serviceInstaller.ServiceName, Properties.Resources.SvcLockInfo, EventLogEntryType.Error);
                    return false;
                }

                // Open the service
                svcHndl = OpenService(scmHndl, serviceInstaller.ServiceName, SERVICE_ALL_ACCESS);

                if (svcHndl.ToInt32() <= 0)
                {

                    EventLog.WriteEntry(serviceInstaller.ServiceName, Properties.Resources.SvcHndlInfo, EventLogEntryType.Error);
                    return false;
                }

                // Need to set service failure actions. Note that the API lets us set as many as
                // we want, yet the Service Control Manager GUI only lets us see the first 3.
                // Bill is aware of this and has promised no fixes. Also note that the API allows
                // granularity of seconds whereas GUI only shows days and minutes.

                // We're gonna serialize the SA_ACTION structs into an array of ints
                // for simplicity in marshalling this variable length ptr to win32
                int[] actions = new int[FailureActions.Count * 2];

                int currInd = 0;
                foreach (FailureAction fa in FailureActions)
                {
                    actions[currInd] = (int)fa.Type;
                    actions[++currInd] = fa.Delay;
                    currInd++;
                }

                // Need to pack 8 bytes per struct
                tmpBuf = Marshal.AllocHGlobal(FailureActions.Count * 8);

                // Move array into marshallable pointer
                Marshal.Copy(actions, 0, tmpBuf, FailureActions.Count * 2);

                // Set the SERVICE_FAILURE_ACTIONS struct
                SERVICE_FAILURE_ACTIONS sfa = new SERVICE_FAILURE_ACTIONS();

                sfa.cActions = FailureActions.Count;
                sfa.dwResetPeriod = SERVICE_NO_CHANGE;
                sfa.lpCommand = RunProgram;
                sfa.lpRebootMsg = RebootMessage;
                sfa.lpsaActions = tmpBuf.ToInt32();

                // Call the ChangeServiceFailureActions() abstraction of ChangeServiceConfig2()
                rslt = ChangeServiceFailureActions(svcHndl, SERVICE_CONFIG_FAILURE_ACTIONS, ref sfa);

                //Check the return
                if (!rslt)
                {
                    int err = GetLastError();
                    if (err == ERROR_ACCESS_DENIED)
                    {
                        throw new Exception(Properties.Resources.AccessDenied);
                    }
                }

                // Free the memory
                Marshal.FreeHGlobal(tmpBuf); tmpBuf = IntPtr.Zero;

                EventLog.WriteEntry(serviceInstaller.ServiceName, Properties.Resources.Successfully, EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(serviceInstaller.ServiceName, ex.Message, EventLogEntryType.Error);

                return false;
            }
            finally
            {

                if (scmHndl != IntPtr.Zero)
                {
                    // Unlock the service database
                    if (svcLock != IntPtr.Zero)
                    {
                        UnlockServiceDatabase(svcLock);
                        svcLock = IntPtr.Zero;
                    }

                    // Close the service control manager handle
                    CloseServiceHandle(scmHndl);
                    scmHndl = IntPtr.Zero;
                }

                // Close the service handle
                if (svcHndl != IntPtr.Zero)
                {
                    CloseServiceHandle(svcHndl);
                    svcHndl = IntPtr.Zero;
                }

                // Free the memory
                if (tmpBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(tmpBuf);
                    tmpBuf = IntPtr.Zero;
                }
            }

            return true;
        }

        #endregion
    }
}