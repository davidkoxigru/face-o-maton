using CameraControl.Devices;
using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private AdminWindow _adminWindow;
        private PhotoWindow _photoWindow;
        private VideoWindow _videoWindow;
        //private MixFacesWindow _mixFacesWindow;

        private const int _nbMaxColorPictures = 1; //18;
        private int _nbColorPictures = _nbMaxColorPictures;
        private int _AdminActiveCount = 0;

        public CameraDeviceManager DeviceManager;

        public MainWindow()
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif

            // MediaVideo.Source = new Uri(@"videos/test.mp4");
            MediaVideo.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
            MediaVideo.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
            //MediaVideo.ScrubbingEnabled = true;
            //MediaVideo.Play();

            StartCamera();

            _adminWindow = new AdminWindow(_nbMaxColorPictures, AdminCallback);
            _videoWindow = new VideoWindow(DeviceManager, Play);
            _photoWindow = new PhotoWindow(DeviceManager, Play, DecreaseNbColorPictures);

            // _mixFacesWindow = new MixFacesWindow(Play);

        }

        public void Play()
        {
            MediaVideo.Play();
        }

        public void DecreaseNbColorPictures()
        {
            _nbColorPictures--;
            ChecckColorPicturesButtons();
        }

        private void ChecckColorPicturesButtons()
        {
            if (_nbColorPictures <= 0)
            {
                MainGrid.Dispatcher.Invoke(() =>
                {
                    // Disabled buttons with color printer
                    Photo_Button_1_Color_1.IsEnabled = false;
                    Photo_Button_1_Color_4.IsEnabled = false;
                    Photo_Button_4_Color_4.IsEnabled = false;
                });
            }
        }

        private void Video_Button_Click(object sender, RoutedEventArgs e)
        {
            MainGrid.Dispatcher.Invoke(() => MediaVideo.Stop());
            _videoWindow.Open();
        }

        private void Photo_Button_1_Sticker_1_Click(object sender, RoutedEventArgs e)
        {
            MediaVideo.Stop();
            _photoWindow.Open(1, FacesPrinter.PrinterType.Sticker, 1);
        }

        private void Photo_Button_4_Sticker_4_Click(object sender, RoutedEventArgs e)
        {
            MediaVideo.Stop();
            _photoWindow.Open(4, FacesPrinter.PrinterType.Sticker, 4);
        }

        private void Photo_Button_1_Color_1_Click(object sender, RoutedEventArgs e)
        {
            MediaVideo.Stop();
            _photoWindow.Open(1, FacesPrinter.PrinterType.Color, 1);
        }

        private void Photo_Button_1_Color_4_Click(object sender, RoutedEventArgs e)
        {
            MediaVideo.Stop();
            _photoWindow.Open(1, FacesPrinter.PrinterType.Color, 4);
        }
        private void Photo_Button_4_Color_4_Click(object sender, RoutedEventArgs e)
        {
            MediaVideo.Stop();
            _photoWindow.Open(4, FacesPrinter.PrinterType.Color, 4);
        }

        //private void Mix_Faces_Button_Click(object sender, RoutedEventArgs e)
        //{
        //MediaVideo.Stop();
        //    mixFacesWindow.Open();
        //}

        private void StartCamera()
        {
            try
            {
                DeviceManager = new CameraDeviceManager();
                DeviceManager.CloseAll();
                DeviceManager.ConnectToCamera();
                DeviceManager.SelectedCameraDevice = DeviceManager.ConnectedDevices.First();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Vérifier l'appareil photo");
            }
        }

        private void CheckPrinters()
        {
            string printerName = @"Canon SELPHY CP1300";
            string query = string.Format("SELECT * from Win32_Printer WHERE Name LIKE '%{0}'", printerName);
            String result = String.Empty;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            using (ManagementObjectCollection coll = searcher.Get())
            {
                try
                {
                    foreach (ManagementObject printer in coll)
                    {
                        foreach (PropertyData property in printer.Properties)
                        {
                            result += string.Format("{0}: {1}", property.Name, property.Value) + Environment.NewLine;
                        }
                    }
                }
                catch (ManagementException ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            MessageBox.Show(result);
        }

        private void Admin1_Button_Click(object sender, RoutedEventArgs e)
        {
            _AdminActiveCount = 1;
        }

        private void Admin2_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_AdminActiveCount == 1)
            {
                _AdminActiveCount = 2;
            }
            else
            {
                _AdminActiveCount = 0;
            }
        }
        private void Admin3_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_AdminActiveCount == 2)
            {
                _AdminActiveCount = 3;
            }
            else
            {
                _AdminActiveCount = 0;
            }
        }
        private void Admin4_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_AdminActiveCount == 3)
            {
                MediaVideo.Stop();

                // Open admin screen 
                _adminWindow.Open(_nbColorPictures);
            }
            else
            {
                _AdminActiveCount = 0;
            }
        }

        public void AdminCallback (int nbColorPictures)
        {
            _nbColorPictures = nbColorPictures;
            Play();
            ChecckColorPicturesButtons();
        } 
    }
}
