using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;

namespace FacesCreationLib
{
    public class FacesCreation
    {
        System.Threading.Timer _timerQueue;
        Queue _photosToProcess = new Queue();

        public FacesCreation()
        {
            // Start thread timer for dequeing new images
            _timerQueue = new System.Threading.Timer(DequeueImages, null, 5000, Timeout.Infinite);
        }
       

        public void Enqueue(Tuple<String, List<Bitmap>> photos)
        {
            // Add image path into queue to process it 
            _photosToProcess.Enqueue(photos);
        }

        public void EnqueueFileName(String photoFileName)
        {
            // Add image path into queue to process it 
            _photosToProcess.Enqueue(photoFileName);
        }

        private readonly object dequeueLock = new object();
        private void DequeueImages(object state)
        {
            while (_photosToProcess.Count > 0)
            {
                try
                {
                    var dequeuedObject = _photosToProcess.Dequeue();

                    if (dequeuedObject is String)
                    {
                        ProcessFromPath(dequeuedObject);
                    }
                    else
                    {
                        ProcessFromBitmaps(dequeuedObject);
                    }


                }
                catch
                {
                }
            }
            _timerQueue.Change(1000, Timeout.Infinite);
        }

        public static void ProcessFromPath(object dequeuedObject)
        {
            var photoName= (String)dequeuedObject;
            var photoPath = Path.GetDirectoryName(photoName) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(photoName);
            DirectoryInfo directoryInfo = Directory.CreateDirectory(photoPath);
            
            Image<Bgr, Byte> image = null;
            // Load image
            image = new Image<Bgr, Byte>(photoName); //Read the files as an 8-bit Bgr image   

            // Get faces in image and save it
            var f = new Faces(image);
            f.GetFacesBitmap(false);
            if (f.Count() > 0)
            {
                f.Save(photoPath + Path.DirectorySeparatorChar + "0");
            }
        }

        private static void ProcessFromBitmaps(object dequeuedObject)
        {
            var photos = (Tuple<String, List<Bitmap>>)dequeuedObject;
            var photoPath = photos.Item1;
            DirectoryInfo directoryInfo = Directory.CreateDirectory(photoPath);
            int i = 0;
            foreach (var photo in photos.Item2)
            {
                // Save image in directory
                photo.Save(photoPath + "\\" + directoryInfo.Name + "-" + (i++).ToString() + @".jpg");
            }

            Image<Bgr, Byte> image = null;
            Faces faces = null;
            int nbPoints = 0;
            int index = 0;
            for (; index < photos.Item2.Count; index++)
            {
                var photo = photos.Item2[index];
                // Load image
                image = new Image<Bgr, Byte>(photo); //Read the files as an 8-bit Bgr image   

                // Get faces in image and save it
                var f = new Faces(image);
                f.GetFacesBitmap(false);
                if (f.Count() > nbPoints)
                {
                    nbPoints = f.Count();
                    faces = f;
                }
            }

            if (faces != null)
            {
                faces.Save(photoPath + Path.DirectorySeparatorChar + index);
            }
        }
    }
}
