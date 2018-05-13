//----------------------------------------------------------------------------
//  Copyright (C) 2004-2013 by EMGU. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace FacesCreationLib
{
    public static class DetectFace
    {

        public static Rectangle[] Detect(Image<Bgr, Byte> image)
        {
            var haarcascadesPath = @"haarcascades\";

            //Load haarcascades for face detection
            using (CascadeClassifier faceCascadeClassifier = new CascadeClassifier(haarcascadesPath + "frontalface_alt.xml"))
            {
                using (Image<Gray, Byte> grayImage = image.Convert<Gray, Byte>()) //Convert it to Grayscale
                {
                    //Convert it to Grayscale
                    Image<Gray, byte> gray = image.Convert<Gray, Byte>();
                    return faceCascadeClassifier.DetectMultiScale(grayImage, 1.1, 10, new Size(100, 100), Size.Empty);
                }
            }
        }

        public static List<FaceFeatures> DetectAll(Image<Bgr, Byte> image)
        {
            var faces = new List<FaceFeatures>();
            var haarcascadesPath = @"haarcascades\";

            //Read the HaarCascade objects
            using (CascadeClassifier faceCascadeClassifier = new CascadeClassifier(haarcascadesPath + "frontalface_alt.xml"))
            using (CascadeClassifier eyeCascadeClassifier = new CascadeClassifier(haarcascadesPath + "eye.xml"))
            using (CascadeClassifier noseCascadeClassifier = new CascadeClassifier(haarcascadesPath + "nose.xml"))
            using (CascadeClassifier mouseCascadeClassifier = new CascadeClassifier(haarcascadesPath + "mouth.xml"))
            {
                using (Image<Gray, Byte> grayImage = image.Convert<Gray, Byte>()) //Convert it to Grayscale
                {
                    //normalizes brightness and increases contrast of the image
                    grayImage._EqualizeHist();

                    //Detect the faces  from the gray scale image and store the locations as rectangle
                    //The first dimensional is the channel
                    //The second dimension is the index of the rectangle in the specific channel
                    Rectangle[] facesDetected = faceCascadeClassifier.DetectMultiScale(grayImage, 1.1, 10, new Size(100, 100), Size.Empty);
                    foreach (Rectangle facerect in facesDetected)
                    { 
                        FaceFeatures face = new FaceFeatures(facerect, grayImage.Cols, grayImage.Rows);
 
                        //Set the region of interest on the probable eyes location 
                        grayImage.ROI = face.ProbableEyeLocation;
                        Rectangle[] eyesDetected = eyeCascadeClassifier.DetectMultiScale(grayImage, 1.1, 10, new Size(10, 10), Size.Empty);
                        face.AddEyes(eyesDetected);

                        //Set the region of interest on the probable nose location 
                        grayImage.ROI = face.ProbableNoseLocation;
                        Rectangle[] noseDetected = noseCascadeClassifier.DetectMultiScale(grayImage, 1.1, 10, new Size(5, 5), Size.Empty);
                        grayImage.ROI = Rectangle.Empty;
                        face.AddNose(noseDetected);

                        //Set the region of interest on the probable mouth location 
                        grayImage.ROI = face.ProbableMouthLocation;
                        Rectangle[] mouthDetected = mouseCascadeClassifier.DetectMultiScale(grayImage, 1.1, 10, new Size(20, 10), Size.Empty);
                        face.AddMouth(mouthDetected);

                        grayImage.ROI = Rectangle.Empty;
 
                        faces.Add(face);
                    }
                }
            }
            return faces;
        }
    }
}
