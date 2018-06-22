using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Controls;

namespace face_o_maton
{
    public class FacesPrinter
    {
        public enum PrinterType
        {
            Sticker,
            Color
        }

        public static void Print(Action<Boolean> callback, List<String> photos, int nbPrints, PrinterType printer)
        {
            try
            {
                if (PrinterType.Sticker == printer)
                {
                    PrintSticker(callback, photos, nbPrints);
                }
                else if (PrinterType.Color == printer)
                {
                    PrintColor(callback, photos, nbPrints);
                }
            }
            catch
            {
                callback(false);
            }
        }

        private static void PrintColor(Action<Boolean> callback, List<String> photos, int nbPrints)
        {
            PrintDocument pd = new PrintDocument();
            PrintController printController = new StandardPrintController();
            pd.PrintController = printController;
         
            pd.PrintPage += (sender, args) =>
            {
                System.Drawing.Image i = null;
                if (photos.Count == 1 && nbPrints == 1)
                {
                    i = ResizeOnePictureAndAddLogo(photos[0]);
                }
                else if (photos.Count == 1 && nbPrints == 4)
                {
                    i = ResizeFourPicturesAndAddLogo(new List<string> { photos[0], photos[0], photos[0], photos[0] });
                }
                else if (photos.Count == 4)
                {
                    i = ResizeFourPicturesAndAddLogo(photos);
                }

                if (i != null)
                {
                    args.Graphics.DrawImage(i, 30, 20);
                }
            };

            pd.EndPrint += (sender, args) =>
            {
                callback(true);
            };

            pd.PrinterSettings.PrinterName = "Canon SELPHY CP1300";
            pd.Print();
        }

        public static System.Drawing.Image ResizeOnePictureAndAddLogo(string stPhotoPath)
        {
            int destWidth = 1555;
            int destHeight = 1036;
            int logoDestWidth = 300;
            int logoDestHeight = 270;

            var imgPhoto = System.Drawing.Image.FromFile(stPhotoPath);

            Bitmap bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            String logoPath = Directory.GetCurrentDirectory() + @"\pictures\CSWedding.png";
            var logo = System.Drawing.Image.FromFile(logoPath);

            DrawImageAndLogo(grPhoto, imgPhoto, 0, 0, destWidth, destHeight, logo, logoDestWidth, logoDestHeight);

            grPhoto.Dispose();
            imgPhoto.Dispose();
            return bmPhoto;
        }

        public static System.Drawing.Image ResizeFourPicturesAndAddLogo(List<string> stPhotoPath)
        {
            // Photo 770x510,  mmarge 15:  logo 150 x 135
            int destWidth = 1555;
            int destHeight = 1036;
            int photoWidth = 770;
            int photoHeight = 510;
            int logoDestWidth = 150;
            int logoDestHeight = 135;
            int marge = 15;

            var imgPhoto0 = System.Drawing.Image.FromFile(stPhotoPath[0]);
            var imgPhoto1 = System.Drawing.Image.FromFile(stPhotoPath[1]);
            var imgPhoto2 = System.Drawing.Image.FromFile(stPhotoPath[2]);
            var imgPhoto3 = System.Drawing.Image.FromFile(stPhotoPath[3]);

            Bitmap bmPhoto = new Bitmap(destWidth, destHeight, PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(imgPhoto0.HorizontalResolution, imgPhoto0.VerticalResolution);


            Graphics grPhoto = Graphics.FromImage(bmPhoto);
            grPhoto.Clear(Color.White);
            grPhoto.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // Add logo
            String logoPath = Directory.GetCurrentDirectory() + @"\pictures\CSWedding.png";
            var logo = System.Drawing.Image.FromFile(logoPath);

            // Photo 0
            DrawImageAndLogo(grPhoto, imgPhoto0, 0, 0, photoWidth, photoHeight, logo, logoDestWidth, logoDestHeight);
            DrawImageAndLogo(grPhoto, imgPhoto1, photoWidth + marge, 0, photoWidth, photoHeight, logo, logoDestWidth, logoDestHeight);
            DrawImageAndLogo(grPhoto, imgPhoto2, 0, photoHeight + marge, photoWidth, photoHeight, logo, logoDestWidth, logoDestHeight);
            DrawImageAndLogo(grPhoto, imgPhoto3, photoWidth + marge, photoHeight + marge, photoWidth, photoHeight, logo, logoDestWidth, logoDestHeight);

            grPhoto.Dispose();
            imgPhoto0.Dispose();
            return bmPhoto;
        }

        private static void DrawImageAndLogo(Graphics grPhoto, System.Drawing.Image imgPhoto, int posX, int posY, int destWidth, int destHeight, System.Drawing.Image logo, int logoDestWidth, int logoDestHeight)
        {
            grPhoto.DrawImage(imgPhoto,
                new Rectangle(posX, posY, destWidth, destHeight),
                new Rectangle(0, 0, imgPhoto.Width, imgPhoto.Height),
                GraphicsUnit.Pixel);

            grPhoto.DrawImage(logo,
                new Rectangle(posX + destWidth - logoDestWidth - 20, posY + destHeight - logoDestHeight - 20, logoDestWidth, logoDestHeight),
                new Rectangle(0, 0, logo.Width, logo.Height),
                GraphicsUnit.Pixel);
        }

        public static void PrintSticker(Action<Boolean> callback, List<String> photos, int angle)
        {
            FacesPrinterClient.Print(photos, angle);
            callback(true);
        }
    }
}
