using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.Drawing;

namespace FacesCreationLib
{
    public class Faces
    {
        public Image<Bgr, Byte> _image;
        List<FaceFeatures> _faces = null;

        public Faces(Image<Bgr, Byte> image)
        {
            _image = image;
        }

        public void Draw(bool includeEyesNoseMouth)
        {
            if (_faces == null) _faces = DetectFace.DetectAll(_image);
            foreach (FaceFeatures f in _faces)
            {
                // Draw even if it is not valid
                f.DrawToImage(_image, 1, includeEyesNoseMouth, false);
            }
        }

        public List<Bitmap> GetFacesBitmap(bool draw)
        {
            var facesBitmap = new List<Bitmap>();
            if (_faces == null) _faces = DetectFace.DetectAll(_image);

            var imageCpy = _image.Copy();
            foreach (FaceFeatures f in _faces)
            {
                if (f.IsValid)
                {
                    // Get face and display on a picture box
                    if (draw)
                    {
                        f.DrawToImage(imageCpy, 2, true, false);
                    }
                    var image = f.GetFaceImage(imageCpy);
                    var target = image.Bitmap;

                    facesBitmap.Add(new Bitmap(target, new Size(target.Width / 4, target.Height / 4)));
                }
            }
            return facesBitmap;
        }

        public int Count()
        {
            var count = 0;
            foreach (FaceFeatures f in _faces)
            {
                if (f.IsValid) count += f.Count;
            }
            return count;
        }

        public void Save(String folderPath)
        {
            if (_faces == null) _faces = DetectFace.DetectAll(_image);
            int i = 1;
            foreach (FaceFeatures f in _faces)
            {
                if (f.IsValid) f.Save(folderPath, i++, _image);
            }
        }
    }
}
