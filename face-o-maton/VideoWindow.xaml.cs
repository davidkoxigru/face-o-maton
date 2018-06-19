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
        private int _counterBeforeVideo;
        private int _counterVideo;
        private Action _playMain;
        private LiveView _liveView;
        private Timer _timerStartVideo;
        private Timer _timerVideo;
        private bool _recording;

        private System.Timers.Timer _timerBeforeStopping;

        public ICameraDevice _cameraDevice { get; set; }

        public VideoWindow(CameraDeviceManager DeviceManager, Action PlayMain)
        {
            InitializeComponent();

#if !DEBUG
            Topmost = true;
#endif
            _playMain = PlayMain;
            _cameraDevice = DeviceManager.SelectedCameraDevice;

        }

        public void Open()
        {
            Show();
            TextBeforeVideo.Visibility = Visibility.Hidden;
            EndMessage.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            Video.Visibility = Visibility.Hidden;
            ButtonImageStop.Visibility = Visibility.Hidden;
            ButtonImage5.Visibility = Visibility.Hidden;
            ButtonImage4.Visibility = Visibility.Hidden;
            ButtonImage3.Visibility = Visibility.Hidden;
            ButtonImage2.Visibility = Visibility.Hidden;
            ButtonImage1.Visibility = Visibility.Hidden;
            ButtonImage0.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Hidden;
            StopButton.IsEnabled = false;

            _liveView = new LiveView(_cameraDevice, StopWithEndMessage);
            _liveView.Start(Video, false);

            LaunchTimerBeforeCapture(3);
        }

        private void LaunchTimerBeforeCapture(int duration)
        {
            _counterBeforeVideo = duration;
            TextBeforeVideo.Visibility = Visibility.Visible;
            _timerStartVideo = new Timer(OnTimerStartVideo, null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1));
        }

        private void OnTimerStartVideo(object status)
        {
            GridVideo.Dispatcher.Invoke(() =>
            {
                if (_counterBeforeVideo-- < 1)
                { 
                    _timerStartVideo.Dispose();
                    _timerStartVideo = null;

                    RecordMovie();
                }
            });
        }


        private void LaunchTimerVideo(int duration)
        {
            StopButton.IsEnabled = true;
            _counterVideo = duration;
            _timerVideo = new Timer(OnTimerVideo, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        private void OnTimerVideo(object status)
        {
            GridVideo.Dispatcher.Invoke(() =>
            {
                _counterVideo--;
                if (_counterVideo < 1)
                {
                    _timerVideo.Dispose();
                    _timerVideo = null;

                    StopWithEndMessage();
                }
                else if (_counterVideo == 1)
                {
                    ButtonImageStop.Visibility = Visibility.Hidden;
                    ButtonImage5.Visibility = Visibility.Hidden;
                    ButtonImage4.Visibility = Visibility.Hidden;
                    ButtonImage3.Visibility = Visibility.Hidden;
                    ButtonImage2.Visibility = Visibility.Hidden;
                    ButtonImage1.Visibility = Visibility.Visible;

                }
                else if (_counterVideo == 2)
                {
                    ButtonImageStop.Visibility = Visibility.Hidden;
                    ButtonImage5.Visibility = Visibility.Hidden;
                    ButtonImage4.Visibility = Visibility.Hidden;
                    ButtonImage3.Visibility = Visibility.Hidden;
                    ButtonImage2.Visibility = Visibility.Visible;
                    ButtonImage1.Visibility = Visibility.Hidden;
                }
                else if (_counterVideo == 3)
                {
                    ButtonImageStop.Visibility = Visibility.Hidden;
                    ButtonImage5.Visibility = Visibility.Hidden;
                    ButtonImage4.Visibility = Visibility.Hidden;
                    ButtonImage3.Visibility = Visibility.Visible;
                    ButtonImage2.Visibility = Visibility.Hidden;
                    ButtonImage1.Visibility = Visibility.Hidden;
                }
                else if (_counterVideo == 4)
                {
                    ButtonImageStop.Visibility = Visibility.Hidden;
                    ButtonImage5.Visibility = Visibility.Hidden;
                    ButtonImage4.Visibility = Visibility.Visible;
                    ButtonImage3.Visibility = Visibility.Hidden;
                    ButtonImage2.Visibility = Visibility.Hidden;
                    ButtonImage1.Visibility = Visibility.Hidden;
                }
                else if (_counterVideo == 5)
                {
                    ButtonImageStop.Visibility = Visibility.Hidden;
                    ButtonImage5.Visibility = Visibility.Visible;
                    ButtonImage4.Visibility = Visibility.Hidden;
                    ButtonImage3.Visibility = Visibility.Hidden;
                    ButtonImage2.Visibility = Visibility.Hidden;
                    ButtonImage1.Visibility = Visibility.Hidden;
                }
                else
                {
                    ButtonImageStop.Visibility = Visibility.Visible;
                    ButtonImage5.Visibility = Visibility.Hidden;
                    ButtonImage4.Visibility = Visibility.Hidden;
                    ButtonImage3.Visibility = Visibility.Hidden;
                    ButtonImage2.Visibility = Visibility.Hidden;
                    ButtonImage1.Visibility = Visibility.Hidden;
                    
                    TextBeforeVideo.Visibility = Visibility.Hidden;
                    StopButton.Visibility = Visibility.Visible;
                    Video.Visibility = Visibility.Visible;
                }
            });
        }

        private void RecordMovie()
        {
            try
            {
                string resp = _recording ? "" : _cameraDevice.GetProhibitionCondition(OperationEnum.RecordMovie);
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
                _cameraDevice.StartRecordMovie();
                _recording = true;
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
                _cameraDevice.StopRecordMovie();
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
            TextBeforeVideo.Visibility = Visibility.Hidden;

            ButtonImageStop.Visibility = Visibility.Hidden;
            ButtonImage5.Visibility = Visibility.Hidden;
            ButtonImage4.Visibility = Visibility.Hidden;
            ButtonImage3.Visibility = Visibility.Hidden;
            ButtonImage2.Visibility = Visibility.Hidden;
            ButtonImage1.Visibility = Visibility.Hidden;
            ButtonImage0.Visibility = Visibility.Hidden;

            Video.Visibility = Visibility.Hidden;

            StopAfterTimer();
        }



        private void StopAfterTimer()
        {
            // Launch timer
            _timerBeforeStopping = new System.Timers.Timer(2000);
            _timerBeforeStopping.Elapsed += TimerBeforeStoppingElapsed;
            _timerBeforeStopping.Start();
        }

        void TimerBeforeStoppingElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timerBeforeStopping.Dispose();
            _timerBeforeStopping = null;

            GridVideo.Dispatcher.Invoke(() => Stop());
        }

        private void StopWithEndMessage()
        {
            // Display end message
            EndMessage.Visibility = Visibility.Visible;
            
            // Mask all
            Video.Visibility = Visibility.Hidden;
            TextBeforeVideo.Visibility = Visibility.Hidden;
            ButtonImageStop.Visibility = Visibility.Hidden;
            ButtonImage5.Visibility = Visibility.Hidden;
            ButtonImage4.Visibility = Visibility.Hidden;
            ButtonImage3.Visibility = Visibility.Hidden;
            ButtonImage2.Visibility = Visibility.Hidden;
            ButtonImage1.Visibility = Visibility.Hidden;
            ButtonImage0.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Hidden;
            StopButton.IsEnabled = false;

            StopAfterTimer();
        }
    

        private void Stop()
        {
            StopButton.IsEnabled = false;
            _liveView.Stop();
            if (_recording)
            {
                StopRecordMovie();
            }
            Hide();
            TextBeforeVideo.Visibility = Visibility.Hidden;
            EndMessage.Visibility = Visibility.Hidden;
            ErrorMessage.Visibility = Visibility.Hidden;
            Video.Visibility = Visibility.Hidden;
            ButtonImageStop.Visibility = Visibility.Hidden;
            ButtonImage5.Visibility = Visibility.Hidden;
            ButtonImage4.Visibility = Visibility.Hidden;
            ButtonImage3.Visibility = Visibility.Hidden;
            ButtonImage2.Visibility = Visibility.Hidden;
            ButtonImage1.Visibility = Visibility.Hidden;
            ButtonImage0.Visibility = Visibility.Hidden;

            StopButton.Visibility = Visibility.Hidden;
            StopButton.IsEnabled = false;
            _playMain();
        }

        private void Button_stop_Click(object sender, RoutedEventArgs e)
        {
            _timerVideo.Dispose();
            _timerVideo = null;

            GridVideo.Dispatcher.Invoke(() => StopWithEndMessage());
        }
    }
}
