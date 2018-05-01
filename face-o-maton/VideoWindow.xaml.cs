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

namespace face_o_maton
{
    /// <summary>
    /// Logique d'interaction pour VideoWindow.xaml
    /// </summary>
    public partial class VideoWindow : Window
    {
        public ICameraDevice CameraDevice { get; set; }

        public VideoWindow(CameraDeviceManager DeviceManager)
        {
            InitializeComponent();

            CameraDevice = DeviceManager.SelectedCameraDevice;
        }

        public void Open()
        {
            Show();
            VideoButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            StartLiveView();
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

                    GridVideo.Dispatcher.Invoke(() => Video.Source = BitmapUtils.BitmapToImageSource(bitmap));
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












        private DateTime _recordStartTime;
        private bool _recording;
        

        private void RecordMovie()
        {
            try
            {
                string resp = _recording ? "" : CameraDevice.GetProhibitionCondition(OperationEnum.RecordMovie);
                if (string.IsNullOrEmpty(resp))
                {
                    var thread = new Thread(RecordMovieThread);
                    thread.Start();
                }
                else
                {
                    // TODO
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Start movie record error", ex.Message);
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
                MessageBox.Show("Recording error", ex.Message);
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
                CameraDevice.StopRecordMovie();
                _recording = false;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Recording error", exception.Message);
            }
        }

        private void Video_Button_Click(object sender, RoutedEventArgs e)
        {
            VideoButton.IsEnabled = false;

            // TODO Count to 3 before recording
            RecordMovie();

            // TODO Max time 10 seconds
        }

        private void Button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            if (_recording)
            {
                StopRecordMovie();
            }
            StopLiveView();
            Hide();
        }
    }
}
