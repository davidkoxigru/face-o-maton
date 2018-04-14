using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace FacesPrinterInterface
{
    [ServiceContract]
    public interface IFacesPrinterContract
    {
        [OperationContract]
        bool Print(string photo, int angle);
    }
}
