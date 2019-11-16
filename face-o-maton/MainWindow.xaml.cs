using CameraControl.Devices;
using GooglePhotoUploader;
using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
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

        private string _path = @"\NbColorPictures.txt";
        private const int _nbMaxColorPictures = 18;
        private int _nbColorPictures = _nbMaxColorPictures;
        private int _AdminActiveCount = 0;
        private int _nbError = 0;

        public CameraDeviceManager DeviceManager;

        public MainWindow()
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            StartCamera();

            _adminWindow = new AdminWindow(_nbMaxColorPictures, null);
            _videoWindow = new VideoWindow(DeviceManager, Callback);
            _photoWindow = new PhotoWindow(DeviceManager, Callback, null, 
                new GPhotosUploader(
                    Properties.Settings.Default.GoogleCredentialsFile,
                    Properties.Settings.Default.GoogleTokenStoreFolder,
                    Properties.Settings.Default.GoogleAlbumName,
                    Properties.Settings.Default.GoogleUserName,
                    Properties.Settings.Default.FacesBackUpPath)
                );

            // _mixFacesWindow = new MixFacesWindow(Play);

            if (File.Exists(Directory.GetCurrentDirectory() + _path))
            {
                // Open the file to read from.
                string readText = File.ReadAllText(Directory.GetCurrentDirectory() + _path);
                _nbColorPictures = int.Parse(readText);
            }
        }
        
        private void Photo_Button_1_Sticker_1_Click(object sender, RoutedEventArgs e)
        {
            _photoWindow.Open(1, FacesPrinter.PrinterType.Sticker, 1);
        }

        private void Photo_Button_4_Sticker_4_Click(object sender, RoutedEventArgs e)
        {
            _photoWindow.Open(4, FacesPrinter.PrinterType.Sticker, 4);
        }

        private void Photo_Button_1_Color_1_Click(object sender, RoutedEventArgs e)
        {
            _photoWindow.Open(1, FacesPrinter.PrinterType.Color, 1);
        }

        private void Photo_Button_1_Color_4_Click(object sender, RoutedEventArgs e)
        {
            _photoWindow.Open(1, FacesPrinter.PrinterType.Color, 4);
        }
        private void Photo_Button_4_Color_4_Click(object sender, RoutedEventArgs e)
        {
            _photoWindow.Open(4, FacesPrinter.PrinterType.Color, 4);
        }

        //private void Mix_Faces_Button_Click(object sender, RoutedEventArgs e)
        //{
        //  StopVideo();
        //  mixFacesWindow.Open();
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
                // Open admin screen 
                _adminWindow.Open(_nbColorPictures);
            }
            else
            {
                _AdminActiveCount = 0;
            }
        }

        public void Callback(Boolean error)
        {
            if (error) _nbError++;
            else _nbError = 0;

            if (_nbError >= 2)
            {
                MainGrid.Dispatcher.Invoke(() =>
                {
                    FondErrorMessage.Visibility = Visibility.Visible;
                    ErrorMessage.Visibility = Visibility.Visible;
                });
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                ThreadStart ts = delegate ()
                {
                    Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        Application.Current.Shutdown();
                    });
                };
                Thread t = new Thread(ts);
                t.Start();
            } 
        }
        
    }
}
