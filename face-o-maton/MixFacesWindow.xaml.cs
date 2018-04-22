using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour MixFacesWindow.xaml
    /// </summary>
    public partial class MixFacesWindow : Window
    {
        private FacesAnim _facesAnim;      

        public MixFacesWindow()
        {
            InitializeComponent();

            enableButtonStep1();
            disableButtonStep1();

            _facesAnim = new FacesAnim();
            _facesAnim.Initialize(DisplayImage, (int)this.Width, (int)this.Height);
        }

        public void Start()
        {
            _facesAnim.Start();
        }

        private void enableButtonStep1()
        {
            Button_change_left.IsEnabled = true;
            Button_change_left.Opacity = 100;
            Button_change_right.IsEnabled = true;
            Button_change_right.Opacity = 100;
            Button_stop.IsEnabled = true;
            Button_stop.Opacity = 100;
        }

        private void disableButtonStep1()
        {
            Button_cancel.IsEnabled = false;
            Button_cancel.Opacity = 0;
            Button_print.IsEnabled = false;
            Button_print.Opacity = 0;
        }

        private void enableButtonsStep2()
        {
            Button_cancel.IsEnabled = true;
            Button_cancel.Opacity = 100;
            Button_print.IsEnabled = true;
            Button_print.Opacity = 100;
        }

        private void disableButtonsStep2()
        {
            Button_change_left.IsEnabled = false;
            Button_change_left.Opacity = 0;
            Button_change_right.IsEnabled = false;
            Button_change_right.Opacity = 0;
            Button_stop.IsEnabled = false;
            Button_stop.Opacity = 0;
        }

        private void Button_change_left_Click(object sender, RoutedEventArgs e)
        {
            _facesAnim.Change();
        }

        private void Button_change_right_Click(object sender, RoutedEventArgs e)
        {
            _facesAnim.Change();
        }

        private void Button_stop_Click(object sender, RoutedEventArgs e)
        {
            _facesAnim.Stop();
            enableButtonsStep2();
            disableButtonsStep2();
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            enableButtonStep1();
            disableButtonStep1();
        }

        private void Button_print_Click(object sender, RoutedEventArgs e)
        {
            // Print image 

            Tuple<Bitmap, InterfaceProj.Json> imageAnim = _facesAnim.GetPrintImage();
            var filePath = _facesAnim.Save(imageAnim);

            List<Tuple<string, int>> ps = new List<Tuple<string, int>>
            {
                Tuple.Create(filePath + @".jpg", imageAnim.Item2.angle)
            };
            FacesPrinter.Print(ps);
        }

        private void DisplayImage(Bitmap image)
        {
            GridMixFaces.Dispatcher.Invoke(() => ImageMixFaces.Source = BitmapToImageSource(image));
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource BitmapToImageSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;
            try
            {
                retval = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }
    }
}
