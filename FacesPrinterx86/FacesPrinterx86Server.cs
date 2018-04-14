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

        public bool Print(string photo, int angle)
        {
            try
            {
                WinConsole.WriteLine("Print :" + photo );
                Printer.print(photo, angle);
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