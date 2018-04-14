using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using AForge.Imaging.Filters;
using InterfaceProj;
using System.Threading;

namespace FacesProj
{
    public class FacesMix : InterfaceImage
    {
        private static int bufferSize = 20;
        private static int minFaces = 10;

        private System.Threading.Timer _timerGetImagesPath;
        private System.Threading.Timer _timerGetImages;

        private Random _rndGetImages = new Random();

        private List<String> _leftEyes = new List<String>();
        private List<String> _rightEyes = new List<String>();
        private List<String> _noses = new List<String>();
        private List<String> _mouths = new List<String>();

        private List<List<Tuple<Bitmap, Json>>> _bitmaps = new List<List<Tuple<Bitmap, Json>>>();
        private int _listIndex = 0;
        private Tuple<Bitmap, Json> _image = null;

        private int _screenWidth;
        private int _screenHeight;

        public FacesMix(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _bitmaps.Add(new List<Tuple<Bitmap, Json>>());
            _bitmaps.Add(new List<Tuple<Bitmap, Json>>());

            _timerGetImages = new System.Threading.Timer(OnGetImagesEvent, null, 0, Timeout.Infinite);
            _timerGetImagesPath = new System.Threading.Timer(OnGetImagesPathEvent, null, 0, Timeout.Infinite);

            _timerGetImagesPath.Change(1, Timeout.Infinite);
            _timerGetImages.Change(5000, Timeout.Infinite); // Wait path are created before creeating images
        }

        public Boolean Ready()
        {
            return _bitmaps[0].Count > bufferSize / 2 || _bitmaps[1].Count > bufferSize / 2;
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
            String path = FacesProj.Properties.Settings.Default.FacesPath;
            String filter = FacesProj.Properties.Settings.Default.Filter;

            DirSearch(path, "leftEye.png", filter, false).ForEach(i => { if (!_leftEyes.Any(s => s == i)) { _leftEyes.Add(i); } });
            DirSearch(path, "rightEye.png", filter, false).ForEach(i => { if (!_rightEyes.Any(s => s == i)) { _rightEyes.Add(i); } });
            DirSearch(path, "nose.png", filter, false).ForEach(i => { if (!_noses.Any(s => s == i)) { _noses.Add(i); } });
            DirSearch(path, "mouth.png", filter, false).ForEach(i => { if (!_mouths.Any(s => s == i)) { _mouths.Add(i); } });
        }

        private void OnGetImagesEvent(object status)
        {
            if (_leftEyes.Count() >= minFaces && _rightEyes.Count() >= minFaces
            && (_bitmaps[0].Count < bufferSize - 1 || _bitmaps[1].Count < bufferSize - 1))
            {
                LoadBitmaps();
            }
            _timerGetImages.Change(300, Timeout.Infinite);
        }

        public void LoadBitmaps()
        {
            var newListIndex = _listIndex == 0 ? 1 : 0;
            _bitmaps[newListIndex].Clear();
            for (var i = 0; i < bufferSize; i++) _bitmaps[newListIndex].Add(CreateImage());
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

        public Tuple<Bitmap, Json> CreateImage()
        {
            if (_leftEyes.Count == 0 || _rightEyes.Count == 0 || _noses.Count == 0 || _mouths.Count == 0)
                return null;

            Random rnd = new Random();

            Json json = new Json();
            json.angle = 270;
            json.type = "Pop face mix";

            var leftEyePath = _leftEyes.ElementAt(rnd.Next(0, _leftEyes.Count() - 1));
            json.addItem(leftEyePath, "Oeil gauche");
            var leftEye = FastLoad(leftEyePath);

            var rightEyePath = _rightEyes.ElementAt(rnd.Next(0, _rightEyes.Count() - 1));
            json.addItem(rightEyePath, "Oeil droit");
            var rightEye = FastLoad(rightEyePath);

            var nosePath = _noses.ElementAt(rnd.Next(0, _noses.Count() - 1));
            json.addItem(nosePath, "Nez");
            var nose = FastLoad(nosePath);

            var mouthPath = _mouths.ElementAt(rnd.Next(0, _mouths.Count() - 1));
            json.addItem(mouthPath, "Bouche");
            var mouth = FastLoad(mouthPath);

            var target = new Bitmap(mouth.Width, mouth.Height, mouth.PixelFormat);
            target.SetResolution(mouth.HorizontalResolution, mouth.VerticalResolution);

            var graphics = Graphics.FromImage(target);
            graphics.CompositingMode = CompositingMode.SourceOver;

            pop(graphics, mouth);
            pop(graphics, nose);
            pop(graphics, leftEye);
            pop(graphics, rightEye);

            var filterHueModifier = new HueModifier(rnd.Next(0, 359));
            filterHueModifier.ApplyInPlace(target);
            
            return new Tuple<Bitmap, Json>(target, json);
        }


        private static void pop(Graphics graphics, Image bmp)
        {
            var popBmp = new Bitmap(bmp, bmp.Width, bmp.Height);
            popBmp.SetResolution(bmp.HorizontalResolution, bmp.VerticalResolution);
            graphics.DrawImage(popBmp, 0, 0);
        }

        private List<String> DirSearch(string sDir, string discriminator, string filter, Boolean filterFound)
        {
            List<String> files = new List<String>();
            try
            {
                if (filterFound) files.AddRange(Directory.GetFiles(sDir, discriminator));

                Directory.GetDirectories(sDir).ToList().ForEach(d =>
                    files.AddRange(DirSearch(d, discriminator, filter, d.EndsWith(filter) | filterFound)));

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
