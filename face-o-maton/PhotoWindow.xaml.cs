using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        public CameraDeviceManager DeviceManager { get; set; }
        public string FolderForPhotos { get; set; }

        public PhotoWindow()
        {
            InitializeComponent();
            StartCamera();
        }

        
        private void StartCamera()
        {
            DeviceManager = new CameraDeviceManager();
            DeviceManager.ConnectToCamera();
            DeviceManager.SelectedCameraDevice = DeviceManager.ConnectedDevices.First();
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            
            // check if camera support live view
            // DeviceManager.SelectedCameraDevice.GetCapability(CapabilityEnum.LiveView);  
        }

        void DeviceManager_CameraDisconnected(ICameraDevice cameraDevice)
        {
            // TODO Display error;
        }

        void DeviceManager_PhotoCaptured(object sender, PhotoCapturedEventArgs eventArgs)
        {
            // to prevent UI freeze start the transfer process in a new thread
            Thread thread = new Thread(PhotoCaptured);
            thread.Start(eventArgs);
        }

        private void PhotoCaptured(object o)
        {
            PhotoCapturedEventArgs eventArgs = o as PhotoCapturedEventArgs;
            if (eventArgs == null)
                return;
            try
            {

                var date = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                var fileName = Properties.Settings.Default.FacesPath + date + ".jpg";
                // if file exist try to generate a new filename to prevent file lost. 
                // This useful when camera is set to record in ram the the all file names are same.
                if (File.Exists(fileName))
                    fileName =
                      StaticHelper.GetUniqueFilename(
                        System.IO.Path.GetDirectoryName(fileName) + "\\" + System.IO.Path.GetFileNameWithoutExtension(fileName) + "_", 0,
                        System.IO.Path.GetExtension(fileName));

                // check the folder of filename, if not found create it
                if (!Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
                }
                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, fileName);
                // the IsBusy may used internally, if file transfer is done should set to false  
                eventArgs.CameraDevice.IsBusy = false;
                GridPhoto.Dispatcher.Invoke(() => Photo.Source = new BitmapImage(new Uri(fileName)));
            }
            catch (Exception exception)
            {
                eventArgs.CameraDevice.IsBusy = false;
                MessageBox.Show("Error download photo from camera :\n" + exception.Message);
            }
        }


        private void Photo_Button_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(Capture);
            thread.Start();
        }

        private void Capture()
        {
            bool retry;
            do
            {
                retry = false;
                try
                {
                    DeviceManager.SelectedCameraDevice.CapturePhoto();
                }
                catch (DeviceException exception)
                {
                    // if device is bussy retry after 100 miliseconds
                    if (exception.ErrorCode == ErrorCodes.MTP_Device_Busy ||
                        exception.ErrorCode == ErrorCodes.ERROR_BUSY)
                    {
                        // !!!!this may cause infinite loop
                        Thread.Sleep(100);
                        retry = true;
                    }
                    else
                    {
                        MessageBox.Show("Error occurred :" + exception.Message);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred :" + ex.Message);
                }

            } while (retry);
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
