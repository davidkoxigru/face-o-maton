using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Drawing.Drawing2D;
using System.IO;
using InterfaceProj;

namespace StarProj
{
    public class Star : InterfaceImage
    {
        private static int bufferSize = 100;
        private static int minFaces = 10;

        private System.Threading.Timer _timerGetImagesPath;
        private System.Threading.Timer _timerGetImages;

        private Random _rndGetImage = new Random();

        private List<List<String>> _faces = new List<List<String>>();
        private int[] _currentIds = new int[4];
        private int _screenWidth;
        private int _screenHeight;
        private Image _fondDessus;
        private Image _fondDessous;

        Queue<Tuple<Bitmap, Json>> _bitmapsQueue = new Queue<Tuple<Bitmap, Json>>();
        Tuple<Bitmap, Json> _image = null;
        
        public Star(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _fondDessus = FastLoad("fond-dessus.png");
            _fondDessous = FastLoad("fond-dessous.png");
            
            _timerGetImagesPath = new System.Threading.Timer(OnGetImagesPathEvent, null, 0, Timeout.Infinite);
            _timerGetImages = new System.Threading.Timer(OnGetImagesEvent, null, 0, Timeout.Infinite);

            _timerGetImagesPath.Change(1, Timeout.Infinite);
            _timerGetImages.Change(5000, Timeout.Infinite); // Wait path are created before creeating images 
        }

        public Boolean Ready()
        {
            return _bitmapsQueue.Count > bufferSize / 2;
        }

        public Tuple<Bitmap, Json> GetPrintImage()
        {
            Rectangle cloneRect = new Rectangle(_image.Item1.Width / 2 - _image.Item1.Height * 3 / 8, 0, _image.Item1.Height * 3 / 4, _image.Item1.Height);
            System.Drawing.Imaging.PixelFormat format = _image.Item1.PixelFormat;
            Bitmap printImage = _image.Item1.Clone(cloneRect, format);
            printImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
            return new Tuple<Bitmap, Json>(printImage, _image.Item2);
        }

        private void OnGetImagesPathEvent(object status)
        {
            GetImagesPath();
            _timerGetImagesPath.Change(60000, Timeout.Infinite);
        }

        private void GetImagesPath()
        {
            DirSearch(StarProj.Properties.Settings.Default.FacesPath, "Star", "face", false)
                .ForEach(i => { if (!_faces.Any(s => s == i)) { _faces.Add(i); } });
        }

        private void OnGetImagesEvent(object status)
        {
            GetImages();
            _timerGetImages.Change(1, Timeout.Infinite);
        }

        private void GetImages()
        {
            if (_faces.Count() >= minFaces)
            {
                while (_bitmapsQueue.Count < bufferSize)
                {
                    Tuple<Bitmap, Json> bitmap = CreateBitmap();
                    if (bitmap != null) _bitmapsQueue.Enqueue(bitmap);
                }
            }
        }

        public Bitmap GetNextImage()
        {
            Tuple<Bitmap, Json> image = null;
            if (_bitmapsQueue.Count > 0)
            {
                image = _bitmapsQueue.Dequeue();
            }
            if (image != null) _image = image;
            return _image == null ? null : _image.Item1;
        }


        public Tuple<Bitmap, Json> CreateBitmap()
        {
            try
            {
                if (_faces.Count == 0)
                    return null;

                for (var i = 3; i > 0; i--) _currentIds[i] = _currentIds[i - 1];

                _currentIds[0] = _rndGetImage.Next(0, _faces.Count() - 1);
                while (_faces[_currentIds[0]] == null || _faces[_currentIds[0]].Count < 4)
                {
                    _currentIds[0] = _rndGetImage.Next(0, _faces.Count() - 1);
                }

                // haut
                var topPath = _faces[_currentIds[0]][0];
                // droite
                var rightPath = _faces[_currentIds[1]][1];
                // bas
                var bottomPath = _faces[_currentIds[2]][2];
                // gauche
                var leftPath = _faces[_currentIds[3]][3];


                Json json = new Json();
                json.angle = 270;
                json.type = "Star";
                
                json.addItem(topPath, "Haut");
                
                json.addItem(rightPath, "Droite");
                                
                json.addItem(bottomPath, "Bas");

                json.addItem(leftPath, "Gauche");

                var face0 = FastLoad(topPath);
                var face1 = FastLoad(rightPath);
                var face2 = FastLoad(bottomPath);
                var face3 = FastLoad(leftPath);

                var target = new Bitmap(face0.Width, face0.Height, face0.PixelFormat);
                target.SetResolution(face0.HorizontalResolution, face0.VerticalResolution);

                var graphics = Graphics.FromImage(target);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.DrawImage(_fondDessous, 0, 0);
                graphics.DrawImage(face0, 0, 0);
                graphics.DrawImage(face2, 0, 0);
                graphics.DrawImage(face3, 0, 0);
                graphics.DrawImage(face1, 0, 0);
                graphics.DrawImage(_fondDessus, 0, 0);

                return new Tuple<Bitmap, Json>(target, json);
            }
            catch
            {
                return null;
            }
        }

        private Image FastLoad(string path)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
                    return Image.FromStream(ms);
            } catch
            {
                return null;
            }
        }

        private List<List<String>> DirSearch(string sDir, string filter, string discriminator, Boolean filterFound)
        {
            List<List<String>> files = new List<List<String>>();
            try
            {
                if (filterFound)
                {
                    List<String> face = new List<string>();
                    for (var i = 0; i < 4; i++)
                    {
                        face.AddRange(Directory.GetFiles(sDir, discriminator + i + ".png"));
                    }
                    files.Add(face);
                }

                Directory.GetDirectories(sDir).ToList().ForEach(d =>
                    files.AddRange(DirSearch(d, filter, discriminator, d.EndsWith(filter) | filterFound)));

                return files;
            }
            catch
            {
                return null;
            }
        }

        public void Plus() { }
        public void Minus() { }
    }
}
