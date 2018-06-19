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
using System.Windows.Threading;
using Timer = System.Timers.Timer;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour Window1.xaml
    /// </summary>
    public partial class PhotoWindow : Window
    {
        private System.Threading.Timer _timerBeforeStoppingPhoto;
        private System.Threading.Timer _timerWatchDog;
        private System.Threading.Timer _timerPreview;
        private System.Threading.Timer _timerStartCapture;
        private FacesPrinter.PrinterType _printer;
        private int _nbPhotos;
        private int _nbPrints;
        private List<String> _photos;
        private Action _playMain;
        private Action _decreaseNbColorPictures;
        private LiveView _liveView;

        public ICameraDevice _cameraDevice { get; set; }

        FacesCreation _facesCreation = new FacesCreation();

        public PhotoWindow(CameraDeviceManager DeviceManager, Action PlayMain, Action DecreaseNbColorPictures)
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            _playMain = PlayMain;
            _decreaseNbColorPictures = DecreaseNbColorPictures;

            _cameraDevice = DeviceManager.SelectedCameraDevice;
            VisibilityManagement(0);
        }

        private void VisibilityManagement(int step)
        {
            // Step 1
            if (step == 1)
            {
                Preview.Visibility = Visibility.Visible;
                PositionMessage.Visibility = Visibility.Visible;
                FourPictures.Visibility = Visibility.Hidden;
            }
            else
            {
                Preview.Visibility = Visibility.Hidden;
                PositionMessage.Visibility = Visibility.Hidden;
            }

            
            // Step 2
            if (step == 2)
            {   
                WatchMessage.Visibility = Visibility.Visible;
                if (_nbPhotos == 4)
                {
                    FourPictures.Visibility = Visibility.Visible;
                    Photo0.Visibility = Visibility.Hidden;
                    Photo1.Visibility = Visibility.Hidden;
                    Photo2.Visibility = Visibility.Hidden;
                    Photo3.Visibility = Visibility.Hidden;
                    BackPhoto0.Visibility = Visibility.Visible;
                    BackPhoto1.Visibility = Visibility.Visible;
                    BackPhoto2.Visibility = Visibility.Visible;
                    BackPhoto3.Visibility = Visibility.Visible;
                    Number0.Visibility = Visibility.Visible;
                    Number1.Visibility = Visibility.Visible;
                    Number2.Visibility = Visibility.Visible;
                    Number3.Visibility = Visibility.Visible;
                }
                else
                {
                    Smile.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Smile.Visibility = Visibility.Hidden;
                Smile0.Visibility = Visibility.Hidden;
                Smile1.Visibility = Visibility.Hidden;
                Smile2.Visibility = Visibility.Hidden;
                Smile3.Visibility = Visibility.Hidden;
                WatchMessage.Visibility = Visibility.Hidden;
            }

            // Step 3
            if (step == 3)
            {
                WaitDownloadMessage.Visibility = Visibility.Visible;
                if (_nbPhotos == 4)
                {
                    if (_photos.Count == 0)
                    {
                        Smile0.Visibility = Visibility.Hidden;
                        Wait0.Visibility = Visibility.Visible;
                    }
                    else if (_photos.Count == 1)
                    {
                        Smile1.Visibility = Visibility.Hidden;
                        Wait1.Visibility = Visibility.Visible;
                    }
                    else if (_photos.Count == 2)
                    {
                        Smile2.Visibility = Visibility.Hidden;
                        Wait2.Visibility = Visibility.Visible;
                    }
                    else if (_photos.Count == 3)
                    {
                        Smile3.Visibility = Visibility.Hidden;
                        Wait3.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (_nbPrints == 4)
                    {
                        FourPictures.Visibility = Visibility.Visible;
                        Photo0.Visibility = Visibility.Hidden;
                        Photo1.Visibility = Visibility.Hidden;
                        Photo2.Visibility = Visibility.Hidden;
                        Photo3.Visibility = Visibility.Hidden;
                        BackPhoto0.Visibility = Visibility.Visible;
                        BackPhoto1.Visibility = Visibility.Visible;
                        BackPhoto2.Visibility = Visibility.Visible;
                        BackPhoto3.Visibility = Visibility.Visible;
                        Wait0.Visibility = Visibility.Visible;
                        Wait1.Visibility = Visibility.Visible;
                        Wait2.Visibility = Visibility.Visible;
                        Wait3.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Wait.Visibility = Visibility.Visible;
                    }
                }
            } 
            else
            {
                WaitDownloadMessage.Visibility = Visibility.Hidden;
                Wait.Visibility = Visibility.Hidden;
                Wait0.Visibility = Visibility.Hidden;
                Wait1.Visibility = Visibility.Hidden;
                Wait2.Visibility = Visibility.Hidden;
                Wait3.Visibility = Visibility.Hidden;
            }

            if (step != 3 && step != 4)
            {
                Photo.Visibility = Visibility.Hidden;
                if (FacesPrinter.PrinterType.Color == _printer)
                {
                    Photo.Effect = null;
                    Photo0.Effect = null;
                    Photo1.Effect = null;
                    Photo2.Effect = null;
                    Photo3.Effect = null;
                }
                else
                {
                    GrayscaleEffect.GrayscaleEffect effect = new GrayscaleEffect.GrayscaleEffect();
                    Photo.Effect = effect;
                    Photo0.Effect = effect;
                    Photo1.Effect = effect;
                    Photo2.Effect = effect;
                    Photo3.Effect = effect;
                }
            }
            else if (_nbPrints == 4)
            {
                FourPictures.Visibility = Visibility.Visible;
            }

            // Step 4
            if (step == 4)
            {
                AskPrintMessage.Visibility = Visibility.Visible;
                PrintButton.Visibility = Visibility.Visible;
                PrintButton.IsEnabled = true;
                CancelButton.Visibility = Visibility.Visible;
                CancelButton.IsEnabled = true;
            } 
            else
            {
                AskPrintMessage.Visibility = Visibility.Hidden;
                PrintButton.Visibility = Visibility.Hidden;
                PrintButton.IsEnabled = false;
                CancelButton.Visibility = Visibility.Hidden;
                CancelButton.IsEnabled = false;
            }

            // Step 5
            if (step == 5)
            {
                PrintMessage.Visibility = Visibility.Visible;
                Print.Visibility = Visibility.Visible;
            }
            else
            {
                PrintMessage.Visibility = Visibility.Hidden;
                Print.Visibility = Visibility.Hidden;
            }

            // Step -1 Error
            if (step == -1)
            {
                ErrorMessage.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorMessage.Visibility = Visibility.Hidden;
            }
        }

        public void Open(int nbPhotos, FacesPrinter.PrinterType printer, int nbPrints)
        {
            _nbPhotos = nbPhotos;
            _printer = printer;
            _nbPrints = nbPrints;

            Show();
            VisibilityManagement(1);

            _photos = new List<string>();

            _cameraDevice.PhotoCaptured += DeviceManager_PhotoCaptured;
            _cameraDevice.WaitForReady();
            
            _liveView = new LiveView(_cameraDevice, Stop);
            _liveView.Start(Preview, true);
            _timerPreview = new System.Threading.Timer(OnTimerPreview, null, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(-1));
        }

        private void OnTimerPreview(object status)
        {
            GridPhoto.Dispatcher.Invoke(() =>
            {
                _liveView.Stop();
                _cameraDevice.WaitForReady();
                VisibilityManagement(2);
                LaunchTimerBeforeCapture();          
            });
        }

        private void LaunchTimerBeforeCapture()
        {
            WaitDownloadMessage.Visibility = Visibility.Hidden;
            if (_nbPhotos == 4)
            {
                if (_photos.Count == 0)
                {
                    Number0.Visibility = Visibility.Hidden;
                    Smile0.Visibility = Visibility.Visible;
                }
                else if (_photos.Count == 1)
                {
                    Wait0.Visibility = Visibility.Hidden;
                    Number1.Visibility = Visibility.Hidden;
                    Smile1.Visibility = Visibility.Visible;
                }
                else if (_photos.Count == 2)
                {
                    Wait1.Visibility = Visibility.Hidden;
                    Number2.Visibility = Visibility.Hidden;
                    Smile2.Visibility = Visibility.Visible;
                }
                else if (_photos.Count == 3)
                {
                    Wait2.Visibility = Visibility.Hidden;
                    Number3.Visibility = Visibility.Hidden;
                    Smile3.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Wait.Visibility = Visibility.Hidden;
                Smile.Visibility = Visibility.Visible;
            }
            WatchMessage.Visibility = Visibility.Visible;
            _timerStartCapture = new System.Threading.Timer(OnTimerStartCapture, null, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(-1));
        }

        private void OnTimerStartCapture(object status)
        {
            GridPhoto.Dispatcher.Invoke(() => PhotoCapture());
        }
        
        private void PhotoCapture()
        {
            _timerWatchDog = new System.Threading.Timer(OnTimerWatchDogElapsed, null, TimeSpan.FromSeconds(30), TimeSpan.FromMilliseconds(-1));

            Thread thread = new Thread(Capture);
            thread.Start();
        }

        void OnTimerWatchDogElapsed(object status)
        {
            _timerWatchDog.Dispose();
            _timerWatchDog = null;
            GridPhoto.Dispatcher.Invoke(() => StopWithErrorMessage());
        }

        private void Capture()
        {
            bool retry;
            int retryNum = 0;
            int retryMax = 10;
            do
            {
                retry = false;
                try
                {
                    _cameraDevice.CapturePhoto();
                }
                catch (DeviceException ex)
                {
                    Log.Debug("Error occurred :" + ex.Message);
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
                    VisibilityManagement(3);
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
                {
                    eventArgs.CameraDevice.DeleteObject(new DeviceObject() { Handle = eventArgs.Handle });
                }

                File.Copy(tempFile, fileName);
                WaitForFile(fileName);

                _photos.Add(fileName);

                _timerWatchDog.Dispose();
                _timerWatchDog = null;

                if (_photos.Count >=_nbPhotos)
                {
                    _photos.ForEach(p => _facesCreation.EnqueueFileName(p));
                }
                GridPhoto.Dispatcher.Invoke(() => DisplayPrintPhotos());
            }
            catch (Exception ex)
            {
                eventArgs.CameraDevice.IsBusy = false;
                Log.Debug("Error download photo from camera :\n" + ex.Message);
                GridPhoto.Dispatcher.Invoke(() => StopWithErrorMessage());
            }
        }

        private void DisplayPrintPhotos()
        {
            try
            {
                if (_nbPhotos == 1)
                {
                    if (_nbPrints == 1)
                    {
                        DisplayPicture(Photo, _photos[0], EndDisplayCallback);
                    }
                    else
                    {
                        DisplayPicture(Photo0, _photos[0], null);
                        DisplayPicture(Photo1, _photos[0], null);
                        DisplayPicture(Photo2, _photos[0], null);
                        DisplayPicture(Photo3, _photos[0], EndDisplayCallback);
                    }
                }
                else 
                {
                    if (_photos.Count == 1) DisplayPicture(Photo0, _photos[0], LaunchTimerBeforeCapture);
                    else if (_photos.Count == 2) DisplayPicture(Photo1, _photos[1], LaunchTimerBeforeCapture);
                    else if (_photos.Count == 3) DisplayPicture(Photo2, _photos[2], LaunchTimerBeforeCapture);
                    else if (_photos.Count == 4) DisplayPicture(Photo3, _photos[3], EndDisplayCallback);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                StopWithErrorMessage();
            }
        }

        private static void DisplayPicture(System.Windows.Controls.Image photo, string path, Action callback)
        {
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Background, new Action(() => {
                    photo.Visibility = Visibility.Visible;
                    photo.Source = new BitmapImage(new Uri(path));
                    callback?.Invoke();
                }));
        }

        private void EndDisplayCallback()
        {
            VisibilityManagement(4);
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
            VisibilityManagement(5);

            Thread printThread = new Thread(() => FacesPrinter.Print(PrinterCallback, _photos, _nbPrints, _printer));
            printThread.Start();
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void StopWithErrorMessage()
        {
            GridPhoto.Dispatcher.Invoke(() => VisibilityManagement(-1));

            if (_liveView != null)
            {
                _liveView.Stop();
                _liveView = null;
            }
            
            // Stop all timers 
            _timerStartCapture.Dispose();
            _timerStartCapture = null;

            _timerWatchDog.Dispose();
            _timerWatchDog = null;

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
            if (_timerBeforeStoppingPhoto == null)
            {
                // Launch timer
                _timerBeforeStoppingPhoto = new System.Threading.Timer(OnTimerBeforeStoppingPhotoElapsed, null, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(-1));
            }
        }

        void OnTimerBeforeStoppingPhotoElapsed(object status)
        {
            _timerBeforeStoppingPhoto.Dispose();
            _timerBeforeStoppingPhoto = null;

            GridPhoto.Dispatcher.Invoke(() => Stop());
        }

        private void Stop()
        {
            if (_liveView != null)
            {
                _liveView.Stop();
                _liveView = null;
            }

            _cameraDevice.PhotoCaptured -= DeviceManager_PhotoCaptured;

            if (_timerBeforeStoppingPhoto != null)
            {
                _timerBeforeStoppingPhoto.Dispose();
                _timerBeforeStoppingPhoto = null;
            }
            if (_timerWatchDog != null)
            {
                _timerWatchDog.Dispose();
                _timerWatchDog = null;
            }

            GridPhoto.Dispatcher.Invoke(() => VisibilityManagement(0));
            
            _playMain();
            Hide();
        }
    }
}
