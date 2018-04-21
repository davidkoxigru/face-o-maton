using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using SoundSyncInterface;

namespace face_o_maton
{
    [ServiceContract]
    public interface ISoundSyncx86Interfaces : ISoundSyncContract
    {
    }

    public class SoundSyncClient : WcfClient<ISoundSyncx86Interfaces>
    {
        static String SoundSyncx86Uri = Properties.Settings.Default.SoundSyncx86Uri;
        
        SoundSyncClient()
            : base(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), new EndpointAddress(SoundSyncx86Uri))
        {
        }

        static Lazy<SoundSyncClient> _instance = new Lazy<SoundSyncClient>(() => new SoundSyncClient());
        public static SoundSyncClient Instance { get { return _instance.Value; } }

        public static void Play()
        {
            try
            {
                Instance.Call.Play();
            }
            catch {}
        }
        public static void Pause()
        {
            try
            {
                Instance.Call.Pause();
            }
            catch { }
        }
        public static void Stop()
        {
            try
            {
                Instance.Call.Stop();
            }
            catch { }
        }
        public static Boolean Display()
        {
            try
            {
                return Instance.Call.Display();
            }
            catch 
            {
                return false;
            }
        }    
    }
}