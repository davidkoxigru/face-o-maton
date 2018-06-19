using bpac;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace FacesPrinterx86
{
    public class Printer
    {
        public static void Print(List<String> photos, int angle)
        {
            String templatePath = null;
            //if (angle == 0)
            //{
                if (photos.Count == 1)
                {
                    // Photo en mode paysage par défaut
                    templatePath = Properties.Settings.Default.LandscapeTemplatePath;
                }
                else if (photos.Count == 4)
                {
                    templatePath = Properties.Settings.Default.Landscape4Template;
                }
                
            //} 
            //else if (photos.Count == 1)
            //{
            //    templatePath = Properties.Settings.Default.PortraitTemplatePath;
            //}

            var doc = new Document();
            if (doc.Open(templatePath) != false)
            {

                String qrCodeString = String.Empty;
                Char delimiter = '\\';
                for (var i = 0; i < photos.Count; i++)
                {
                    var path = photos[i].Remove(0, Path.GetPathRoot(photos[i]).Length);
                    path = path.Remove(path.Length - Path.GetExtension(photos[i]).Length, Path.GetExtension(photos[i]).Length);
                    String[] substrings = path.Split(delimiter);
                    if (qrCodeString != String.Empty)
                    {
                        qrCodeString += "," ;
                    }
                    qrCodeString += substrings[substrings.Length - 1];
                }
                qrCodeString  = Properties.Settings.Default.Url + qrCodeString;
                QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.M);
                QrCode qrCode = qrEncoder.Encode(qrCodeString);

                GraphicsRenderer renderer = new GraphicsRenderer(new FixedModuleSize(5, QuietZoneModules.Two), Brushes.Black, Brushes.White);
                String qrCodePath = Directory.GetCurrentDirectory() + @"\qrCode.jpg";
                using (FileStream stream = new FileStream(qrCodePath, FileMode.Create))
                {
                    renderer.WriteToStream(qrCode.Matrix, ImageFormat.Jpeg, stream);
                }
                doc.GetObject("QrCode").SetData(0, qrCodePath, 4);

                for (var i = 0; i < photos.Count; i ++)
                {
                    doc.GetObject("Photo" + i).SetData(0, photos[i], 4);
                }
                
                doc.SetPrinter("Brother QL-500", false);
                doc.Printer.GetInstalledPrinters();
                doc.SetMediaById(doc.Printer.GetMediaId(), true);
                doc.StartPrint("", PrintOptionConstants.bpoHighResolution);
                doc.PrintOut(1, PrintOptionConstants.bpoHighResolution);
                doc.EndPrint();
                doc.Close();
            }
        }
    }
}
