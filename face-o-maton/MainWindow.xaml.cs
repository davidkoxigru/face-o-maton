using CameraControl.Devices;
using System;
using System.Linq;
using System.Windows;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        PhotoWindow photoWindow;
        VideoWindow videoWindow;
        MixFacesWindow mixFacesWindow;

        public CameraDeviceManager DeviceManager;

        public MainWindow()
        {
            InitializeComponent();

            StartCamera();
            videoWindow = new VideoWindow(DeviceManager);
            photoWindow = new PhotoWindow(DeviceManager);
            mixFacesWindow = new MixFacesWindow();
        }

        private void Video_Button_Click(object sender, RoutedEventArgs e)
        {
            videoWindow.Open();
        }

        private void Photo_Button_Click(object sender, RoutedEventArgs e)
        {
            photoWindow.Open();
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
    }
}
