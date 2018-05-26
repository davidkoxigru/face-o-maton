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
        private PhotoWindow photoWindow;
        private VideoWindow videoWindow;
        private MixFacesWindow mixFacesWindow;

        private const int _nbMaxColorPictures = 1; //18;
        private int _nbColorPictures = _nbMaxColorPictures;

        public CameraDeviceManager DeviceManager;

        public MainWindow()
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif

            StartCamera();
            videoWindow = new VideoWindow(DeviceManager);
            photoWindow = new PhotoWindow(DeviceManager, DecreaseNbColorPictures);

            // mixFacesWindow = new MixFacesWindow();
        }

        public void DecreaseNbColorPictures()
        {
            _nbColorPictures--;
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
            videoWindow.Open();
        }

        private void Photo_Button_1_Sticker_1_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(1, FacesPrinter.PrinterType.Sticker, 1);
        }
        private void Photo_Button_1_Sticker_4_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(1, FacesPrinter.PrinterType.Sticker, 4);
        }

        private void Photo_Button_4_Sticker_4_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(4, FacesPrinter.PrinterType.Sticker, 4);
        }

        private void Photo_Button_1_Color_1_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(1, FacesPrinter.PrinterType.Color, 1);
        }

        private void Photo_Button_1_Color_4_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(1, FacesPrinter.PrinterType.Color, 4);
        }
        private void Photo_Button_4_Color_4_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open(4, FacesPrinter.PrinterType.Color, 4);
        }

        private void Mix_Faces_Button_Click(object sender, RoutedEventArgs e)
        {
            mixFacesWindow.Open();
        }

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
        
    }
}
