using GooglePhotoUploader;
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

#if !DEBUG
            Topmost = true;
#endif

            EnableButtonStep1();
            DisableButtonStep1();

            _facesAnim = new FacesAnim();
            _facesAnim.Initialize(DisplayImage, (int)ImageMixFaces.Width, (int)ImageMixFaces.Height);
        }

        public void Open()
        {
            try
            {
                _facesAnim.Start();
                Show();
            } catch (Exception ex)
            {
                MessageBox.Show("Error ", ex.ToString());
            }
        }

        private void EnableButtonStep1()
        {
            Button_change_left.IsEnabled = true;
            Button_change_left.Opacity = 100;
            Button_change_right.IsEnabled = true;
            Button_change_right.Opacity = 100;
            Button_stop.IsEnabled = true;
            Button_stop.Opacity = 100;
        }

        private void DisableButtonStep1()
        {
            Button_cancel.IsEnabled = false;
            Button_cancel.Opacity = 0;
            Button_print.IsEnabled = false;
            Button_print.Opacity = 0;
        }

        private void EnableButtonsStep2()
        {
            Button_cancel.IsEnabled = true;
            Button_cancel.Opacity = 100;
            Button_print.IsEnabled = true;
            Button_print.Opacity = 100;
        }

        private void DisableButtonsStep2()
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
            EnableButtonsStep2();
            DisableButtonsStep2();
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            EnableButtonStep1();
            DisableButtonStep1();
        }

        private void Button_print_Click(object sender, RoutedEventArgs e)
        {
            // Print image 
            Tuple<Bitmap, InterfaceProj.Json> imageAnim = _facesAnim.GetPrintImage();
            var filePath = _facesAnim.Save(imageAnim);
            
            FacesPrinter.PrintSticker(PrinterCallback, new List <PhotoPath> { new PhotoPath(filePath + @".jpg") }, imageAnim.Item2.angle);
        }
        private void PrinterCallback(Boolean printOk)
        {
             // Check what to do
        }

        private void DisplayImage(Bitmap image)
        {
            GridMixFaces.Dispatcher.Invoke(() => ImageMixFaces.Source = BitmapUtils.BitmapToImageSource(image));
        }
    }
}
