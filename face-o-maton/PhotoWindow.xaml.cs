using CameraControl.Devices;
using CameraControl.Devices.Classes;
using FacesCreationLib;
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
        private Timer _timerLiveView = new Timer(1000 / 15); // Display 15 images per seconds
        private Timer _timerBeforeStopping = new Timer(2000);
        private System.Threading.Timer _timerStartCapture;
        private FacesPrinter.PrinterType _printer;
        private int _nbPhotos;
        private int _nbPrints;
        private List<String> _photos;
        private Action _playMain;
        private Action _decreaseNbColorPictures;

        public ICameraDevice CameraDevice { get; set; }

        FacesCreation _facesCreation = new FacesCreation();

        public PhotoWindow(CameraDeviceManager DeviceManager, Action PlayMain, Action DecreaseNbColorPictures)
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            _playMain = PlayMain;
            _decreaseNbColorPictures = DecreaseNbColorPictures;
            CameraDevice = DeviceManager.SelectedCameraDevice;

            WatchMessage.Visibility = Visibility.Hidden;
            WaitMessage.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            PrintMessage.Visibility = Visibility.Hidden;
            FourPictures.Visibility = Visibility.Hidden;
            PrintButton.Visibility = Visibility.Hidden;
            CancelButton.Visibility = Visibility.Hidden;
            Photo.Visibility = Visibility.Hidden;
        }

        public void Open(int nbPhotos, FacesPrinter.PrinterType printer, int nbPrints)
        {
            _nbPhotos = nbPhotos;
            _printer = printer;
            _nbPrints = nbPrints;

            Show();
            WatchMessage.Visibility = Visibility.Hidden;
            WaitMessage.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            PrintMessage.Visibility = Visibility.Hidden;
            FourPictures.Visibility = Visibility.Hidden;
            PrintButton.IsEnabled = false;
            PrintButton.Visibility = Visibility.Hidden;
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Hidden;
            Photo.Visibility = Visibility.Visible;
            CameraDevice.PhotoCaptured += DeviceManager_PhotoCaptured;
            
            _photos = new List<string>();
            CameraDevice.WaitForReady();

            StartLiveView();
            LaunchTimerBeforeCapture(3);
        }

        private void LaunchTimerBeforeCapture(int duration)
        {
            StartLiveView();
            WaitMessage.Visibility = Visibility.Hidden;
            WatchMessage.Visibility = Visibility.Hidden;

            Photo.Visibility = Visibility.Visible;
            WatchMessage.Visibility = Visibility.Visible;
            Counter.Visibility = Visibility.Visible;
            Counter.Text = duration.ToString();
            _timerStartCapture = new System.Threading.Timer(OnTimerStartCapture, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimerStartCapture(object status)
        {
            GridPhoto.Dispatcher.Invoke(() =>
            {
                int counter = Int32.Parse(Counter.Text);
                if (counter-- > 1)
                {
                    Counter.Text = counter.ToString();
                }
                else
                {
                    Counter.Text = "0";
                    _timerStartCapture.Dispose();
                    StopLiveView();
                    CameraDevice.WaitForReady();
                    PhotoCapture();
                }
            });
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
                   Log.Debug("Error starting live view " + resp);
                    _timerLiveView.Stop();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Error starting live view " + ex.ToString());
                _timerLiveView.Stop();
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
                    _timerLiveView.Elapsed += TimerLiveViewElapsed;
                    _timerLiveView.Start();
                }

                Log.Debug("LiveView: Liveview start done");
            }
            catch (Exception exception)
            {
                Log.Debug("Unable to start liveview ! " + exception.ToString());
            }
        }

        void TimerLiveViewElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerLiveView.Stop();
            Task.Factory.StartNew(GetLiveViewThread);
        }


        private void GetLiveViewThread()
        {
            Get();

            _timerLiveView.Start();
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
                _timerLiveView.Stop();
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
        
        private void PhotoCapture()
        {
            Thread thread = new Thread(Capture);
            thread.Start();
        }

        private void Capture()
        {
            bool retry;
            int retryNum = 0;
            int retryMax = 5;
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
                    Log.Debug("Error occurred :" + ex.Message);
                    StopWithErrorMessage();
                }

            } while (retry && retryNum < retryMax);

            if (retryNum >= retryMax)
            {
                Log.Debug(retryMax + " errors occurred");
                GridPhoto.Dispatcher.Invoke(() => StopWithErrorMessage());
            }
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
                GridPhoto.Dispatcher.Invoke(() =>
                {
                    Counter.Visibility = Visibility.Hidden;
                    Photo.Visibility = Visibility.Hidden;
                    WatchMessage.Visibility = Visibility.Hidden;
                    WaitMessage.Visibility = Visibility.Visible;
                });

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

                _photos.Add(fileName);
                
                if (_photos.Count < _nbPhotos)
                {
                    WindowPhoto.Dispatcher.Invoke(() => LaunchTimerBeforeCapture(2));
                }
                else
                {
                    _photos.ForEach(p => _facesCreation.EnqueueFileName(p));
                    GridPhoto.Dispatcher.Invoke(() => DisplayPrintPhotos(_photos));
                }
            }
            catch (Exception ex)
            {
                eventArgs.CameraDevice.IsBusy = false;
                Log.Debug("Error download photo from camera :\n" + ex.Message);
                StopWithErrorMessage();
            }
        }

        private void DisplayPhoto (String fileName) {
            Photo.Visibility = Visibility.Visible;
            Photo.Source = new BitmapImage(new Uri(fileName));
            CancelButton.Visibility = Visibility.Visible;
        }

        private void DisplayPrintPhotos(List<String> photos)
        {
            WaitMessage.Visibility = Visibility.Hidden;
            WatchMessage.Visibility = Visibility.Hidden;

            if (photos.Count == 1)
            {
                Photo.Visibility = Visibility.Visible;
                Photo.Source = new BitmapImage(new Uri(photos[0]));
            } else if (photos.Count == 4)
            {
                Photo.Visibility = Visibility.Hidden;
                Photo0.Source = new BitmapImage(new Uri(photos[0]));
                Photo1.Source = new BitmapImage(new Uri(photos[1]));
                Photo2.Source = new BitmapImage(new Uri(photos[2]));
                Photo3.Source = new BitmapImage(new Uri(photos[3]));
                FourPictures.Visibility = Visibility.Visible;
            }
            else
            {
                // Error
                Stop();
            }

            PrintButton.IsEnabled= true;
            PrintButton.Visibility = Visibility.Visible;

            CancelButton.IsEnabled = true;
            CancelButton.Visibility = Visibility.Visible;
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

        private void Button_print_Click(object sender, RoutedEventArgs e)
        {
            // Display print message
            PrintMessage.Visibility = Visibility.Visible;

            // Mask all
            PrintButton.IsEnabled = false;
            PrintButton.Visibility = Visibility.Hidden;
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Hidden;

            FourPictures.Visibility = Visibility.Hidden;
            Photo.Visibility = Visibility.Hidden;

            Thread printThread = new Thread(() => FacesPrinter.Print(PrinterCallback, _photos, _nbPrints, _printer));
            printThread.Start();
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void StopWithErrorMessage()
        {
            // Display error message
            ErrorMessage.Visibility = Visibility.Visible;

            // Mask all
            Counter.Visibility = Visibility.Hidden;
            PrintMessage.Visibility = Visibility.Hidden;
            WatchMessage.Visibility = Visibility.Hidden;

            PrintButton.IsEnabled = false;
            PrintButton.Visibility = Visibility.Hidden;
            CancelButton.IsEnabled = false;
            CancelButton.Visibility = Visibility.Hidden;

            FourPictures.Visibility = Visibility.Hidden;
            Photo.Visibility = Visibility.Hidden;

            StopAfterTimer();
        }

        private void PrinterCallback(Boolean printOk)
        {
            if (!printOk)
            {
                StopWithErrorMessage();
            }
            else
            {
                if (_printer == FacesPrinter.PrinterType.Color)
                {
                    _decreaseNbColorPictures();
                }
                StopAfterTimer();
            }
        }

        private void StopAfterTimer()
        {
            // Launch timer
            _timerBeforeStopping.Elapsed += TimerBeforeStoppingElapsed;
            _timerBeforeStopping.Start();
        }

        void TimerBeforeStoppingElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerBeforeStopping.Stop();
            GridPhoto.Dispatcher.Invoke(() => Stop());
        }

        private void Stop()
        {
            StopLiveView();
            CameraDevice.PhotoCaptured -= DeviceManager_PhotoCaptured;

            WatchMessage.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            PrintMessage.Visibility = Visibility.Hidden;
            FourPictures.Visibility = Visibility.Hidden;
            PrintButton.Visibility = Visibility.Hidden;
            CancelButton.Visibility = Visibility.Hidden;
            Photo.Visibility = Visibility.Hidden;

            _playMain();
            Hide();
        }
    }
}
