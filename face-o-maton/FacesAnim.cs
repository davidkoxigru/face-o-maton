using InterfaceProj;
using StarProj;
using FacesProj;
using PopProj;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;

namespace face_o_maton
{
    class FacesAnim
    {
        private bool _play = true;
        private Action<Bitmap> _callback;
        private System.Timers.Timer _timerDisplayImage;
        private int _imageAnimInterfaceIndex = 0;
        private List<InterfaceImage> _imageAnim = new List<InterfaceImage>();

        public void Initialize(Action<Bitmap> callback, int width, int height)
        {
            _callback = callback;

            _imageAnim.Add(new Star(width, height));
            _imageAnim.Add(new FacesMix(width, height));
            _imageAnim.Add(new Pop(width, height, "face.png", 2.1, 2, 3.5));

            // Launch timer
            _timerDisplayImage = new System.Timers.Timer(200); // 143 ms => ~7 images per secondes
            _timerDisplayImage.Elapsed += OnDisplayImageEvent;
            _timerDisplayImage.AutoReset = true;
        }

        private void OnDisplayImageEvent(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_play /*&& SoundSyncClient.Display()*/) Display();
        }
        
        public bool Ready()
        {
            return _imageAnim.Exists(i => i.Ready());
        }

        public void Start()
        {
            Random rnd = new Random();
            _imageAnimInterfaceIndex = rnd.Next(0, _imageAnim.Count - 1);
            if (!_imageAnim[_imageAnimInterfaceIndex].Ready()) Change();

            _play = true;
            _timerDisplayImage.Start();
            SoundSyncClient.Play();
            Display();
        }

        public void Stop()
        {
            SoundSyncClient.Pause();
            _play = false;
            _timerDisplayImage.Stop();
        }

        public void Change()
        {
            int index = _imageAnimInterfaceIndex;
            do
            {
                index = index >= _imageAnim.Count - 1 ? 0 : index + 1;
            }
            while (!(_imageAnim[index].Ready() || index == _imageAnimInterfaceIndex));
            _imageAnimInterfaceIndex = index;
        }

        public Tuple<Bitmap, Json> GetPrintImage()
        {
            return _imageAnim[_imageAnimInterfaceIndex].GetPrintImage();
        }

        public string Save(Tuple<Bitmap, Json> imageAnim)
        {
            var date = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            var dirPath = Properties.Settings.Default.FacesPath + @"ImageAnim\" + _imageAnim[_imageAnimInterfaceIndex].GetType().Name;
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
            var imageAnimPath = dirPath + @"\" + date;
            imageAnim.Item1.Save(imageAnimPath + @".jpg");

            //Save sources:
            SaveJson(imageAnimPath, imageAnim.Item2);

            return imageAnimPath;
        }

        private void SaveJson(String path, Json json)
        {
            json.photo = path.Remove(0, Path.GetPathRoot(path).Length) + @".jpg";
            string str = JsonConvert.SerializeObject(json);
            using (StreamWriter file = File.CreateText(path + @".json"))
            {
                file.Write(str);
            }
        }
        
        private void Display()
        {
            Bitmap bitmap = _imageAnim[_imageAnimInterfaceIndex].GetNextImage();
            if (bitmap != null) _callback(bitmap);
        }
    }
}
