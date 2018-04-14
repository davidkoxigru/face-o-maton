using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceModel;
using System.Diagnostics;
using SoundSyncInterface;
using SoundLib;

namespace SoundSyncx86
{
     [ServiceContract]
    public interface ISoundSyncx86Interfaces : ISoundSyncContract
    {
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SoundSyncx86Server : ISoundSyncx86Interfaces
    {
        private SoundSync _soundSync = new SoundSync();

        #region ISoundSyncx86Interfaces Members

        public void Play()
        {
            try
            {
                WinConsole.WriteLine("Play");
                _soundSync.Play();
            }
            catch (Exception ex)
            {
                WinConsole.WriteLine(ex);
            }
        }


        public void Pause()
        {
            try
            {
                WinConsole.WriteLine("Pause");
                _soundSync.Pause();
            }
            catch (Exception ex)
            {
                WinConsole.WriteLine(ex);
            }
        }

        public void Stop()
        {
            try
            {
                WinConsole.WriteLine("Stop");
                _soundSync.Stop();
            }
            catch (Exception ex)
            {
                WinConsole.WriteLine(ex);
            }
        }

        public Boolean Display()
        {
            try
            {
                return _soundSync.Display();
            }
            catch (Exception ex)
            {
                WinConsole.WriteLine(ex);
                return false;
            }
        }
        #endregion
    }
}