using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace InterfaceProj
{
    public interface InterfaceImage
    {
        Boolean Ready();
        Tuple<Bitmap, Json> GetPrintImage();
        Bitmap GetNextImage();
        void Plus();
        void Minus();
    }
}
