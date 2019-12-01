using Microsoft.VisualStudio.TestTools.UnitTesting;
using FacesCreationLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FacesCreationLib.Tests
{
    [TestClass()]
    public class FacesCreationTests
    {
        [TestMethod()]
        public void FacesCreationTest()
        {

            //DirectoryInfo d = new DirectoryInfo(@"D:\Faces");
            DirectoryInfo d = new DirectoryInfo(@".\Picture");
            FileInfo[] Files = d.GetFiles("*.jpg"); 
            foreach (FileInfo file in Files)
            {
                FacesCreation.ProcessFromPath(file.FullName);
            }           
            //Assert.Fail();
        }
    }
}