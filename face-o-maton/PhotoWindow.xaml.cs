using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using WebEye.Controls.Wpf.StreamPlayerControl;
using Timer = System.Timers.Timer;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        private Timer _timer = new Timer(1000 / 15);

        public ICameraDevice CameraDevice { get; set; }
        
        public PhotoWindow(CameraDeviceManager DeviceManager)
        {
            InitializeComponent();
            CameraDevice = DeviceManager.SelectedCameraDevice;
        }

        public void Open()
        {
            Show();
            CancelButton.IsEnabled = true;
            PhotoButton.IsEnabled = true;
            CameraDevice.PhotoCaptured += DeviceManager_PhotoCaptured;
            StartLiveView();
        }

        public void StartLiveView()
        {
            try
            {
                string resp = CameraDevice.GetProhibitionCondition(OperationEnum.LiveView);
                if (string.IsNullOrEmpty(resp))
                {
                    Thread thread = new Thread(StartLiveViewThread);
                    thread.Start();
                    thread.Join();
                }
                else
                {

                    MessageBox.Show("Error starting live view " + resp);
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting live view ", ex.ToString());
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
                            Stop();
                        }
                    }
                } while (retry && retryNum < 35);

                if (CameraDevice.GetCapability(CapabilityEnum.LiveViewStream))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(
                        () => _videoSource.StartPlay(new Uri(CameraDevice.GetLiveViewStream()))));
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
                MessageBox.Show("Unable to start liveview ! ", exception.ToString());
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
                }
            }
            catch (Exception)
            {
            }
        }

        private void StopLiveView()
        {
            try
            {
                _timer.Stop();
                var LiveViewData = CameraDevice.GetLiveViewImage();
                if (LiveViewData.IsLiveViewRunning)
                {
                    CameraDevice.StopLiveView();
                }
            }
            catch
            {
                // Do nothing
            }
        }
        private void Photo_Button_Click(object sender, RoutedEventArgs e)
        {
            PhotoButton.IsEnabled = false;
            PhotoCapture();
        }

        private void PhotoCapture()
        {
            StopLiveView();
            CameraDevice.WaitForReady();
            Thread thread = new Thread(Capture);
            thread.Start();
        }

        private void Capture()
        {
            bool retry;
            int retryNum = 0;
            do
            {
                retry = false;
                try
                {
                    CameraDevice.CapturePhoto();
                }
                catch (DeviceException ex)
                {
                    Thread.Sleep(100);
                    retryNum++;
                    retry = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred :" + ex.Message);
                    Stop();
                }

            } while (retry && retryNum < 35);
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
                eventArgs.CameraDevice.IsBusy = true;
                var date = DateTime.Now.ToString("yyyyMMddHHmmssffff");
                var fileName = Properties.Settings.Default.FacesPath + date + ".jpg";

                // check the folder of filename, if not found create it
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                string tempFile = Path.GetTempFileName();

                if (File.Exists(tempFile))
                    File.Delete(tempFile);

                Stopwatch stopWatch = new Stopwatch();
                // transfer file from camera  
                // in this way if the session folder is used as hot folder will prevent write errors
                stopWatch.Start();
                eventArgs.CameraDevice.TransferFile(eventArgs.Handle, tempFile);
                eventArgs.CameraDevice.ReleaseResurce(eventArgs.Handle);
                eventArgs.CameraDevice.IsBusy = false;
                stopWatch.Stop();

                if (!eventArgs.CameraDevice.CaptureInSdRam)
                    eventArgs.CameraDevice.DeleteObject(new DeviceObject() { Handle = eventArgs.Handle });

                File.Copy(tempFile, fileName);

                WaitForFile(fileName);

                GridPhoto.Dispatcher.Invoke(() => Photo.Source = new BitmapImage(new Uri(fileName)));


                // TODO Move print function
                PrintSticker(fileName);
            }
            catch (Exception ex)
            {
                eventArgs.CameraDevice.IsBusy = false;
                MessageBox.Show("Error download photo from camera :\n" + ex.Message);
                Stop();
            }
        }

        private static void PrintSticker(string fileName)
        {
            List<Tuple<string, int>> ps = new List<Tuple<string, int>>
                {
                    Tuple.Create(fileName, 0)
                };
            FacesPrinter.Print(ps);
        }

        public static void WaitForFile(string file)
        {
            if (!File.Exists(file))
                return;
            int retry = 15;
            while (IsFileLocked(file) && retry > 0)
            {
                Thread.Sleep(100);
                retry--;
            }
        }

        public static bool IsFileLocked(string file)
        {
            FileStream stream = null;
            try
            {
                stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            StopLiveView();
            CameraDevice.PhotoCaptured -= DeviceManager_PhotoCaptured;
            Hide();
        }
    }
}
