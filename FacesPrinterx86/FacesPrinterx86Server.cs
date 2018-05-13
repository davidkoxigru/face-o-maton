using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceModel;
using System.Diagnostics;
using FacesPrinterInterface;

namespace FacesPrinterx86
{
    [ServiceContract]
    public interface IFacesPrinterx86Interfaces : IFacesPrinterContract
    {
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class FacesPrinterx86Server : IFacesPrinterx86Interfaces
    {
        #region IFacesPrinterx86Interfaces Members

        public bool Print(List<String> photos, int angle)
        {
            try
            {
                photos.ForEach(p => WinConsole.WriteLine("Print :" + p));
                Printer.Print(photos, angle);
                return true;
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