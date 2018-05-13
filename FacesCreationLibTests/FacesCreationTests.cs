using Microsoft.VisualStudio.TestTools.UnitTesting;
using FacesCreationLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FacesCreationLib.Tests
{
    [TestClass()]
    public class FacesCreationTests
    {
        [TestMethod()]
        public void FacesCreationTest()
        {
            FacesCreation.ProcessFromPath(".//Picture//test.jpg");

            //Assert.Fail();
        }
    }
}