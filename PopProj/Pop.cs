using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AForge.Imaging.Filters;
using InterfaceProj;

namespace PopProj
{
    public class Pop : InterfaceImage
    {
        private static int bufferSize = 20;
        private static int minFaces = 10;

        private System.Threading.Timer _timerGetImagesPath;
        private System.Threading.Timer _timerGetImages;

        private List<String> _faces = new List<String>();

        private List<List<Tuple<Bitmap, Json>>> _bitmaps = new List<List<Tuple<Bitmap, Json>>>();
        private int _listIndex = 0;
        private Tuple<Bitmap, Json> _image = null;

        private Random _rndGetImages = new Random();
        private Random _rndCreateImage = new Random();
        private Random _rndCreateImageColor = new Random();

        private int _screenWidth;
        private int _screenHeight;

        private int _nbX;
        private int _nbY;
        private int _posX;
        private int _posY;
        private PixelFormat _pixelFormat;
        private float _horizontalResolution;
        private float _verticalResolution;
        private double _factor;
        private double _factorMin;
        private double _factorMax;
        private string _element;

        public Pop(int screenWidth, int screenHeight, String element, double factor, double factorMin, double factorMax)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _element = element;
            _factor = factor;
            _factorMin = factorMin;
            _factorMax = factorMax;

            _bitmaps.Add(new List<Tuple<Bitmap, Json>>());
            _bitmaps.Add(new List<Tuple<Bitmap, Json>>());

            _timerGetImages = new System.Threading.Timer(OnGetImagesEvent, null, 0, Timeout.Infinite);
            _timerGetImagesPath = new System.Threading.Timer(OnGetImagesPathEvent, null, 0, Timeout.Infinite);
            
            _timerGetImagesPath.Change(1, Timeout.Infinite);
            _timerGetImages.Change(5000, Timeout.Infinite); // Wait path are created before creeating images
        }

        private void SetParameters()
        {
            // Get nbElements
            var face = FastLoad(_faces.ElementAt(0));

            float width = (float)(face.Width / _factor);
            float height = (float)(face.Height / _factor);
            _nbX = (int)(_screenWidth / width);
            _nbY = (int)(_screenHeight / height);
            _posX = (int)(_screenWidth - _nbX * width) / 2;
            _posY = (int)(_screenHeight - _nbY * height) / 2;
            _pixelFormat = face.PixelFormat;
            _horizontalResolution = face.HorizontalResolution;
            _verticalResolution = face.VerticalResolution;
        }

        public Boolean Ready()
        {
            return _bitmaps[0].Count > bufferSize / 2 || _bitmaps[1].Count > bufferSize / 2; 
        }

        public Tuple<Bitmap, Json> GetPrintImage()
        {
            return _image;
        }

        private void OnGetImagesPathEvent(object status)
        {
            GetImagesPath();
            _timerGetImagesPath.Change(60000, Timeout.Infinite);
        }

        private void GetImagesPath()
        {
            // Load new faces
            DirSearch(PopProj.Properties.Settings.Default.FacesPath, _element)
                .ForEach(i => { if (!_faces.Any(s => s == i)) { _faces.Add(i); } });
        }

        private void OnGetImagesEvent(object status)
        {
            if (_faces.Count() >= minFaces
                && (_bitmaps[0].Count < bufferSize-1 || _bitmaps[1].Count < bufferSize-1))
            {
                LoadBitmaps();
            }
            _timerGetImages.Change(300, Timeout.Infinite);
        }

        public void LoadBitmaps()
        {
            SetParameters();
            var factor = _factor;
            var newListIndex = _listIndex == 0 ? 1 : 0;
            _bitmaps[newListIndex].Clear();
            for (var i = 0; i < bufferSize; i++) _bitmaps[newListIndex].Add(CreateImage(factor));
            _listIndex = newListIndex;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private Image FastLoad(string path)
        {
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
                return Image.FromStream(ms);
        }

        public Bitmap GetNextImage()
        {
            Tuple<Bitmap, Json> image = null;
            if (_bitmaps.Count >= _listIndex && _bitmaps[_listIndex].Count > 0)
            {
                image = _bitmaps[_listIndex][_rndGetImages.Next(0, _bitmaps[_listIndex].Count() - 1)];
            }
            if (image != null) _image = image;
            return _image == null ? null : _image.Item1;
        }

        public Tuple<Bitmap, Json> CreateImage(double factor)
        {
            var target = new Bitmap(_screenWidth, _screenHeight, _pixelFormat);
            target.SetResolution(_horizontalResolution, _verticalResolution);
            var graphics = Graphics.FromImage(target);
            graphics.CompositingMode = CompositingMode.SourceOver;

            Json json = new Json();
            json.angle = 0;
            json.type = "Collection pop";
            for (var i = 0; i < _nbX; i++)
            {
                for (var j = 0; j < _nbY; j++)
                {
                    var path = _faces.ElementAt(_rndCreateImage.Next(0, _faces.Count() - 1));
                    json.addItem(path, "Tête " + (i+1) + "-" + (j+1));

                    var face = FastLoad(path);
                    var filter = new HueModifier(_rndCreateImage.Next(0, 359));
                    float width = (float)(face.Width / factor);
                    float height = (float)(face.Height / factor);
                    graphics.DrawImage(filter.Apply(new Bitmap(face)), _posX + i * width, _posY + j * height, width, height);
                }
            }

            return new Tuple<Bitmap, Json>(target, json);
        }

        private List<String> DirSearch(string sDir, string discriminator)
        {
            List<String> files = new List<String>();
            try
            {
                var f = Directory.GetFiles(sDir, discriminator);
                if (f.Count() > 0)
                {
                    files.AddRange(f);
                }
                else
                {
                    Directory.GetDirectories(sDir).ToList().ForEach(d =>
                        files.AddRange(DirSearch(d, discriminator)));
                }

                return files;
            }
            catch
            {
                return null;
            }
        }


        public void Plus()
        {
            if (_factor < _factorMax)
            {
                _factor += 0.25;
            }

        }

        public void Minus()
        {
            if (_factor > _factorMin)
            {
                _factor -= 0.25;
            }
        }
    }
}