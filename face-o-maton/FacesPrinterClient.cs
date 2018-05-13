using System;
using System.Collections.Generic;
using System.ServiceModel;
using FacesPrinterInterface;

namespace face_o_maton
{
    [ServiceContract]
    public interface IAgentx86Interface : IFacesPrinterContract
    {
    }

    public class FacesPrinterClient : WcfClient<IAgentx86Interface>
    {
        static String FacesPrinterx86Uri = Properties.Settings.Default.FacesPrinterx86Uri;
        
        FacesPrinterClient()
            : base(new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), new EndpointAddress(FacesPrinterx86Uri))
        {
        }

        static Lazy<FacesPrinterClient> _instance = new Lazy<FacesPrinterClient>(() => new FacesPrinterClient());
        public static FacesPrinterClient Instance { get { return _instance.Value; } }

        public static bool Print(List<String> photos, int angle)
        {
            try
            {
                return Instance.Call.Print(photos, angle);
            }
            catch
            {
                return false;
            }
        }
    }
}