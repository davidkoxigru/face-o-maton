using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WebEye.Controls.Wpf.StreamPlayerControl;

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        private Action _playMain;
        private Timer _timerStartVideo;
        private DateTime _recordStartTime;
        private bool _recording;

        private System.Timers.Timer _timerBeforeStopping = new System.Timers.Timer(2000);

        public ICameraDevice CameraDevice { get; set; }

        public VideoWindow(CameraDeviceManager DeviceManager, Action PlayMain)
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            _playMain = PlayMain;
            CameraDevice = DeviceManager.SelectedCameraDevice;

        }

        public void Open()
        {
            Show();
            CounterBeforeVideo.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            CounterVideo.Visibility = Visibility.Hidden;
            StartLiveView();
            LaunchTimerBeforeCapture(3);
        }

        private void LaunchTimerBeforeCapture(int duration)
        {
            StartLiveView();
            CounterBeforeVideo.Visibility = Visibility.Visible;
            CounterBeforeVideo.Text = duration.ToString();
            _timerStartVideo = new Timer(OnTimerStartVideo, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimerStartVideo(object status)
        {
            GridVideo.Dispatcher.Invoke(() =>
            {
                int counter = Int32.Parse(CounterBeforeVideo.Text);
                if (counter-- > 1)
                {
                    CounterBeforeVideo.Text = counter.ToString();
                }
                else
                {
                    _timerStartVideo.Dispose();
                    RecordMovie();
                    CounterBeforeVideo.Visibility = Visibility.Hidden;
                }
            });
        }

        private System.Timers.Timer _timer = new System.Timers.Timer(1000 / 15);
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
                    StopWithErrorMessage();
                    _timer.Stop();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Error starting live view " + ex.ToString());
                StopWithErrorMessage();
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
                Log.Debug("Unable to start liveview ! " + exception.ToString());
                StopWithErrorMessage();
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

                    GridVideo.Dispatcher.Invoke(() => Video.Source = BitmapUtils.BitmapToImageSource(bitmap));
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Error occurred :" + ex.Message);
                StopWithErrorMessage();
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


        private void LaunchTimerVideo(int duration)
        {
            StartLiveView();
            CounterVideo.Visibility = Visibility.Visible;
            CounterVideo.Text = duration.ToString();
            _timerStartVideo = new Timer(OnTimerVideo, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimerVideo(object status)
        {
            GridVideo.Dispatcher.Invoke(() =>
            {
                int counter = Int32.Parse(CounterVideo.Text);
                if (counter-- > 0)
                {
                    CounterVideo.Text = counter.ToString();
                }
                else
                {
                    _timerStartVideo.Dispose();
                    Stop();
                }
            });
        }

        private void RecordMovie()
        {
            try
            {
                string resp = _recording ? "" : CameraDevice.GetProhibitionCondition(OperationEnum.RecordMovie);
                if (string.IsNullOrEmpty(resp))
                {
                    LaunchTimerVideo(10);
                    var thread = new Thread(RecordMovieThread);
                    thread.Start();
                }
                else
                {
                    Log.Debug("Error occurred");
                    StopWithErrorMessage();
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Error occurred :" + ex.Message);
                StopWithErrorMessage();
            }
        }

        private void RecordMovieThread()
        {
            try
            {
                CameraDevice.StartRecordMovie();
                _recording = true;
                _recordStartTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.Debug("Error: " + ex.Message);
                GridVideo.Dispatcher.Invoke(() => StopWithErrorMessage());
            }
        }

        private void StopRecordMovie()
        {
            var thread = new Thread(StopRecordMovieThread);
            thread.Start();
        }

        private void StopRecordMovieThread()
        {
            try
            {
                _recording = false;
                CameraDevice.StopRecordMovie();
            }
            catch (Exception ex)
            {
                Log.Debug("Error: " + ex.Message);
                GridVideo.Dispatcher.Invoke(() => StopWithErrorMessage());
            }
        }
        private void StopWithErrorMessage()
        {
            // Display error message
            ErrorMessage.Visibility = Visibility.Visible;

            // Mask all
            CounterBeforeVideo.Visibility = Visibility.Hidden;
            CounterVideo.Visibility = Visibility.Hidden;
            Video.Visibility = Visibility.Hidden;

            StopAfterTimer();
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
            GridVideo.Dispatcher.Invoke(() => Stop());
        }

        private void Stop()
        {
            StopLiveView();
            if (_recording)
            {
                StopRecordMovie();
            }

            _playMain();
            Hide();
        }
    }
}
