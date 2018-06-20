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
    public class LiveView
    {
        private System.Timers.Timer _timerLiveView;
        ICameraDevice _cameraDevice;
        Action _stopCallback;
        System.Windows.Controls.Image _image;

        public LiveView(ICameraDevice cameraDevice, Action stopCallback)
        {
            _cameraDevice = cameraDevice;
            _stopCallback = stopCallback;
        }

        public void Start(System.Windows.Controls.Image image, bool show)
        {
            try
            {
                _image = image;
                if (show)
                {
                    _image.Visibility = Visibility.Visible;
                }

                _timerLiveView = new System.Timers.Timer(1000 / 15); // Display 15 images per seconds

                string resp = _cameraDevice.GetProhibitionCondition(OperationEnum.LiveView);
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
                        _cameraDevice.StartLiveView();
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
                            _stopCallback();
                        }
                    }
                } while (retry && retryNum < 35);
                
                _timerLiveView.Elapsed += TimerLiveViewElapsed;
                _timerLiveView.Start();
                

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
                LiveViewData = _cameraDevice.GetLiveViewImage();
                if (LiveViewData != null && LiveViewData.ImageData != null)
                {
                    Bitmap bitmap = new Bitmap(new MemoryStream(LiveViewData.ImageData,
                        LiveViewData.ImageDataPosition,
                        LiveViewData.ImageData.Length - LiveViewData.ImageDataPosition));
                    _image.Dispatcher.Invoke(() => _image.Source = BitmapUtils.BitmapToImageSource(bitmap));
                }
            }
            catch (Exception)
            {
            }
        }

        public void Stop()
        {
            try
            {
                if (_timerLiveView != null)
                {
                    _timerLiveView.Dispose();
                    _timerLiveView = null;
                }

                if (_cameraDevice != null)
                {
                    var liveViewData = _cameraDevice.GetLiveViewImage();
                    if (liveViewData != null && liveViewData.IsLiveViewRunning)
                    {
                        _cameraDevice.StopLiveView();
                    }
                }
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
