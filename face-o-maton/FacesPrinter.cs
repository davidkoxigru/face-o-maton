using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;

namespace face_o_maton
{
    class FacesPrinter
    {
        public static void Print(List<Tuple<string, int>> photos)
        {
            if (Properties.Settings.Default.Printer.Equals("Brother QL-500"))
            {
                FacesPrinterClient.Print(photos[0].Item1, photos[0].Item2);
            }
            else
            {
                // "Canon SELPHY CP1300 WS"
                PrintDocument pd = new PrintDocument();
                pd.PrintPage += (sender, args) =>
                {
                    Image i = Image.FromFile(photos[0].Item1);

                    // Créer image en fonction du nombre de photo 1, 2 ou 4
                    // Image de taille 1555x1050mm 300dpi
                    // Photo max 1480x1000 ou 1470x970 si marge

                    args.Graphics.DrawImage(i, args.MarginBounds);
                };
                pd.PrinterSettings.PrinterName = Properties.Settings.Default.Printer;
                pd.Print();
            }
        }
    }
}
