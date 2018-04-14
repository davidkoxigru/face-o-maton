using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using libZPlay;

namespace SoundLib
{
    public class SoundSync
    {
        private bool _continue = true;
        private bool _play = true;
        private bool _display;
        private ZPlay _player = new ZPlay();
        private Boolean _stopPlayer;
        
        public SoundSync()
        {
            Thread thread = new Thread(StartLibZPlay);
            thread.Start();
            Pause();


            for (int i = 0; i < 50; i++)
                VolumeChanger.VolumeDown();
            for (int i = 0; i < Properties.Settings.Default.VolumeMin / 2; i++)
                VolumeChanger.VolumeUp();
        }

        public void Pause()
        {
            if (_play)
            {
                _play = false;
                for (int i = 0; i < (Properties.Settings.Default.VolumeMax - Properties.Settings.Default.VolumeMin) / 2; i++)
                    VolumeChanger.VolumeDown();
            }
        }

        public void Play()
        {
            if (!_play)
            {
                _play = true;
                for (int i = 0; i < (Properties.Settings.Default.VolumeMax - Properties.Settings.Default.VolumeMin) / 2; i++)
                    VolumeChanger.VolumeUp();
            }
        }

        public Boolean Display()
        {
            if (_display)
            {
                _display = false;
                if (_play) return true;
            }
            return false;
        }

        public void StartLibZPlay()
        {
            _stopPlayer = false;

            // open file
            var fileName =
            "wavein://src=microphone;volume=50;";
            // @"C:\Users\david_000\Music\my best remixes\10 vibromatic.mp3";
            
            FixedSizedQueue<FFTData> fftDatas = new FixedSizedQueue<FFTData>();
            fftDatas.Limit = 5000;

            if (!_player.OpenFile(fileName, TStreamFormat.sfAutodetect)) return;
            _player.StartPlayback();
            Pause();
            while (_continue && !_stopPlayer)
            {
                FFTData fftData = new FFTData();
                _player.GetFFTData(512, TFFTWindow.fwRectangular, ref fftData.HarmonicNumber, ref fftData.HarmonicFreq, ref fftData.LeftAmplitude, ref fftData.RightAmplitude, ref fftData.LeftPhase, ref fftData.RightPhase);
                fftDatas.Enqueue(fftData);
                if (change(fftDatas))
                {
                    _display = true;
                }

                System.Threading.Thread.Sleep(1);
            }

            _player.Close();

            if (_continue) StartLibZPlay();
        }

        public void RestartLibZplay()
        {
            _stopPlayer = true;
        }

        public void Stop()
        {
            _continue = false;
        }

        public List<double> _bassSmoothed;
        Boolean change(FixedSizedQueue<FFTData> fftDatas)
        {
            if (fftDatas.q.Count > fftDatas.Limit / 2)
            {
                List<int> bass = new List<int>();
                fftDatas.q.ToList().ForEach(f => bass.Add(getBassIntensity(f)));
                _bassSmoothed = smooth(bass);

                var averageBassIntesity = _bassSmoothed.Average();
                if (averageBassIntesity < 10000) return false;

                var averageMaxBassIntesity = _bassSmoothed.Where(b => b > averageBassIntesity).Average();
                if (_bassSmoothed.Last() > averageMaxBassIntesity)
                {
                    return true;
                }
            }
            return false;
        }

        private static int getBassIntensity(FFTData fftData)
        {
            int bassIntensity = 0;
            for (int i = 8; i < 48; i++)
            {
                bassIntensity += fftData.LeftAmplitude[i] * fftData.RightAmplitude[i];
            }
            return bassIntensity;
        }

        private static List<double> smooth(List<int> data)
        {
            // For np = 5 = 5 data points
            var h =
                //5175; 
                35.0;
            var easyCoeff = new float[]
                // { -253, -138, -33, 62, 147, 222, 287, 343, 387, 422, 447, 462, 467, 462, 447, 422, 387, 343, 287, 222, 147, 62, -33, -138, -253 }; 
                { -3, 12, 17, 12, -3 }; // Its symmetrical
            var center = (easyCoeff.Count() - 1) / 2;
            List<double> smoothed = new List<double>();
            for (int x = center; x < data.Count - center; x++)
            {
                double val = 0;
                for (int i = 0; i < easyCoeff.Count(); i++)
                {
                    val += (data[x + i - center] * easyCoeff[i]);
                }
                smoothed.Add(val / h);
            }
            return smoothed;
        }
    }

    public class FFTData
    {
        public int HarmonicNumber;
        public int[] HarmonicFreq;
        public int[] LeftAmplitude;
        public int[] RightAmplitude;
        public int[] LeftPhase;
        public int[] RightPhase;

        public FFTData()
        {
            this.HarmonicNumber = new int();
            this.HarmonicFreq = new int[257];
            this.LeftAmplitude = new int[257];
            this.RightAmplitude = new int[257];
            this.LeftPhase = new int[257];
            this.RightPhase = new int[257];
        }
    }

    public class FixedSizedQueue<T>
    {
        public ConcurrentQueue<T> q { get; set; }
        public int Limit { get; set; }

        public FixedSizedQueue()
        {
            q = new ConcurrentQueue<T>();
        }

        public void Enqueue(T obj)
        {
            q.Enqueue(obj);
            lock (this)
            {
                T overflow;
                while (q.Count > Limit && q.TryDequeue(out overflow)) ;
            }
        }
    }
}
