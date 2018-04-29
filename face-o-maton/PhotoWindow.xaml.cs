using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System;
using System.Collections.Generic;
using System.Drawing;
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
using WebEye.Controls.Wpf.StreamPlayerControl;
using Timer = System.Timers.Timer;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        public CameraDeviceManager DeviceManager { get; set; }

        public ICameraDevice CameraDevice { get; set; }

        private Timer _timer = new Timer(1000 / 15);


        public PhotoWindow()
        {
            InitializeComponent();
            StartCamera();
        }

        void PhotoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartLiveView();
        }

        private void StartCamera()
        {
            DeviceManager = new CameraDeviceManager();
            DeviceManager.ConnectToCamera();
            DeviceManager.SelectedCameraDevice = DeviceManager.ConnectedDevices.First();
            DeviceManager.PhotoCaptured += DeviceManager_PhotoCaptured;
            CameraDevice = DeviceManager.SelectedCameraDevice;
        }

        private void StartLiveView()
        {
            try
            {

                if (!IsActive)
                    return;

                string resp = CameraDevice.GetProhibitionCondition(OperationEnum.LiveView);
                if (string.IsNullOrEmpty(resp))
                {
                    Thread thread = new Thread(StartLiveViewThread);
                    thread.Start();
                    thread.Join();
                }
                else
                {
                    Log.Error("Error starting live view " + resp);
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error starting live view ", ex);
                _timer.Stop();
            }
        }

        private StreamPlayerControl _videoSource = new StreamPlayerControl();
        private void StartLiveViewThread()
        {
            try
            {
                bool retry = false;
                int retryNum = 0;
                Log.Debug("LiveView: Liveview started");
                do
                {
                    try
                    {
                        CameraDevice.StartLiveView();
                    }
                    catch (DeviceException deviceException)
                    {
                        if (deviceException.ErrorCode == ErrorCodes.ERROR_BUSY ||
                            deviceException.ErrorCode == ErrorCodes.MTP_Device_Busy)
                        {
                            Thread.Sleep(100);
                            Log.Debug("Retry live view :" + deviceException.ErrorCode.ToString("X"));
                            retry = true;
                            retryNum++;
                        }
                        else
                        {
                            throw;
                        }
                    }
                } while (retry && retryNum < 35);

                if (CameraDevice.GetCapability(CapabilityEnum.LiveViewStream))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(
                        () =>
                        {
                            _videoSource.StartPlay(new Uri(CameraDevice.GetLiveViewStream()));
                            StaticHelper.Instance.SystemMessage = "Waiting for live view stream...";
                        }));
                }
                else
                {
                    _timer.Elapsed += _timer_Elapsed;
                    _timer.Start();
                }

                Log.Debug("LiveView: Liveview start done");
            }
            catch (Exception exception)
            {
                Log.Error("Unable to start liveview !", exception);
                StaticHelper.Instance.SystemMessage = "Unable to start liveview ! " + exception.Message;
            }
        }

        void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            Task.Factory.StartNew(GetLiveViewThread);
        }


        private void GetLiveViewThread()
        {
            Get();
            _timer.Start();
        }

        void Get()
        {
            LiveViewData LiveViewData = null;
            try
            {
                LiveViewData = CameraDevice.GetLiveViewImage();
            
                if (LiveViewData != null && LiveViewData.ImageData != null)
                {
                    Bitmap bitmap = new Bitmap(new MemoryStream(LiveViewData.ImageData,
                        LiveViewData.ImageDataPosition,
                        LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition));

                    GridPhoto.Dispatcher.Invoke(() => Photo.Source = BitmapUtils.BitmapToImageSource(bitmap));

                    // IsMovieRecording = LiveViewData.MovieIsRecording;

                }
            }
            catch (Exception)
            {


            }
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
            CameraDevice.StopLiveView();
            CameraDevice.AutoFocus();
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
            CameraDevice.StopLiveView();
            Hide();
        }
    }
}
