namespace SoundSyncx86
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur de composants

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller
            // 
            this.serviceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller.Password = null;
            this.serviceProcessInstaller.Username = null;
            this.serviceProcessInstaller.BeforeInstall += new System.Configuration.Install.InstallEventHandler(this.serviceProcessInstaller_BeforeInstall);
            // 
            // serviceInstaller
            // 
            this.serviceInstaller.Description = "Faces Printer x86 Services";
            this.serviceInstaller.DisplayName = "Faces Printer x86 Service";
            this.serviceInstaller.ServiceName = "KickItFacesPrinterx86";
            this.serviceInstaller.Committing += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller_Committing);
            this.serviceInstaller.BeforeInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller_BeforeInstall);
            this.serviceInstaller.BeforeUninstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller_BeforeUninstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller,
            this.serviceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller serviceInstaller;
        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller;
    }
}