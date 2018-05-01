using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using bpac;
using Gma.QrCodeNet.Encoding;
using Gma.QrCodeNet.Encoding.Windows.Render;

namespace FacesPrinterx86
{
    public class Printer
    {
        public static void Print(string photo, int angle)
        {
            String templatePath ;
            if (angle == 0)
            {
                // Photo en mode paysage par défaut
                templatePath = Properties.Settings.Default.LandscapeTemplatePath;
            } 
            else
            {
                templatePath = Properties.Settings.Default.PortraitTemplatePath;
            }
            
            var doc = new DocumentClass();
            if (doc.Open(templatePath) != false)
            {
                QrEncoder qrEncoder = new QrEncoder(ErrorCorrectionLevel.M);

                var path = photo.Remove(0, Path.GetPathRoot(photo).Length);
                path = path.Remove(path.Length - Path.GetExtension(photo).Length, Path.GetExtension(photo).Length);
                QrCode qrCode = qrEncoder.Encode(Properties.Settings.Default.Url + path);
                GraphicsRenderer renderer = new GraphicsRenderer(new FixedModuleSize(5, QuietZoneModules.Two), Brushes.Black, Brushes.White);
                String qrCodePath = Directory.GetCurrentDirectory() + @"\qrCode.jpg";
                using (FileStream stream = new FileStream(qrCodePath, FileMode.Create))
                {
                    renderer.WriteToStream(qrCode.Matrix, ImageFormat.Jpeg, stream);
                }
                doc.GetObject("QrCode").SetData(0, qrCodePath, 4);
                doc.GetObject("Photo").SetData(0, photo, 4);

                doc.SetMediaById(doc.Printer.GetMediaId(), true);
                doc.StartPrint("", PrintOptionConstants.bpoHighResolution);
                doc.PrintOut(1, PrintOptionConstants.bpoHighResolution);
                doc.EndPrint();
                doc.Close();
            }
        }
    }
}
