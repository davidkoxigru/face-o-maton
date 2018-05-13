using Microsoft.VisualStudio.TestTools.UnitTesting;
using FacesPrinterx86;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacesPrinterx86.Tests
{
    [TestClass()]
    public class PrinterTests
    {
        [TestMethod()]
        public void PrintTest()
        {
            Printer.Print(new List<string> { @".\Picture\picture0.jpg", @".\Picture\picture1.jpg", @".\Picture\picture2.jpg", @".\Picture\picture3.jpg" }, 0);
        }
    }
}