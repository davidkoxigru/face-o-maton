using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Photoshop;

namespace FacesCreationLib
{
    public class FaceFeatures
    {
        public Rectangle ProbableEyeLocation { get; private set; }
        public Rectangle ProbableLeftEyeLocation { get; private set; }
        public Rectangle ProbableRightEyeLocation { get; private set; }
        public Rectangle ProbableNoseLocation { get; private set; }
        public Rectangle ProbableMouthLocation { get; private set; }
        public Rectangle FaceLocation { get; private set; }
        public Rectangle LeftEyeLocation { get; private set; }
        public Rectangle RightEyeLocation { get; private set; }
        public Rectangle NoseLocation { get; private set; }
        public Rectangle MouthLocation { get; private set; }

        public FaceFeatures(Rectangle face, int frameWidth, int frameHeight)
        {
            this.FaceLocation = face;
            CalcProbableEyeLocation(frameWidth, frameHeight);
            CalcProbableNoseLocation(frameWidth, frameHeight);
            CalcProbableMouthLocation(frameWidth, frameHeight);
        }

        private void CalcProbableEyeLocation(int width, int height)
        {
            var probableEyeLocation = FaceLocation;
            int originalHeight = probableEyeLocation.Height;
            probableEyeLocation.Height = (int) (probableEyeLocation.Height / 2.7f); //only search a part of the face

            //shif the frame a little bit down.
            int shiftOverY = (int) ((originalHeight / 1.7) - probableEyeLocation.Height);

            //now shift the window a little bit down
            probableEyeLocation.Y += shiftOverY;
            ProbableEyeLocation = probableEyeLocation;

            ProbableEyeLocation = FixBoundings(probableEyeLocation, width, height);
        }

        private void CalcProbableMouthLocation(int width, int height)
        {
            //these values are based on tests
            var probableMouthLocation = FaceLocation;
            probableMouthLocation.Width /= 2;
            probableMouthLocation.Height /= 3;

            //shif the frame a little bit down.
            int shiftOverX = ((FaceLocation.Width - probableMouthLocation.Width) / 2);
            int shiftOverY = probableMouthLocation.Height * 2;

            //now shift the window a little bit down
            probableMouthLocation.X += shiftOverX;
            probableMouthLocation.Y += shiftOverY;

            ProbableMouthLocation = FixBoundings(probableMouthLocation, width, height);
        }

        private void CalcProbableNoseLocation(int width, int height)
        {
            Rectangle probableNoseLocation = FaceLocation;

            // These values are based on tests
            probableNoseLocation.Width = (int)(0.45 * probableNoseLocation.Width);
            probableNoseLocation.Height = (int)(0.45 * probableNoseLocation.Height);

            //shif the frame a little bit down.
            int shiftOverX = ((FaceLocation.Width - probableNoseLocation.Width) / 2);
            int shiftOverY = ((FaceLocation.Height - probableNoseLocation.Height) / 2) + (int)( FaceLocation.Height / 50) ;
            probableNoseLocation.X += shiftOverX;
            probableNoseLocation.Y += shiftOverY;
            ProbableNoseLocation = probableNoseLocation;

            ProbableNoseLocation = FixBoundings(probableNoseLocation, width, height);
        }

        Rectangle FixBoundings(Rectangle rect, int width, int height)
        {
            if (rect.Left < 0)
            {
                rect.X = 0;
            }

            if (rect.Top < 0)
            {
                rect.Y = 0;
            }

            if (rect.Bottom > height)
            {
                rect.Height = rect.Height - (rect.Bottom - height);
            }

            if (rect.Right > width)
            {
                rect.Width = rect.Width - (rect.Right - width);
            }

            return rect;
        }

        public bool IsValid
        {
            get 
            {
                if (LeftEyeLocation.IsEmpty || RightEyeLocation.IsEmpty || NoseLocation.IsEmpty && MouthLocation.IsEmpty) 
                {
                    return false;
                }

                // Check that eyes are aligned 
                if (Math.Abs(LeftEyeLocation.Y - RightEyeLocation.Y) > FaceLocation.Height / 50)
                {
                    return false;
                }

                // Check eyes seems ok on the face
                if (Math.Abs((LeftEyeLocation.X + LeftEyeLocation.Width / 2) - (FaceLocation.X + FaceLocation.Width / 2 - FaceLocation.Width / 6)) > FaceLocation.Width / 20)
                {
                    return false;
                }

                if (Math.Abs((RightEyeLocation.X + RightEyeLocation.Width / 2) - (FaceLocation.X + FaceLocation.Width / 2 + FaceLocation.Width / 6)) > FaceLocation.Width / 20)
                {
                    return false;
                }

                var eyesRef = Math.Abs((LeftEyeLocation.Y + LeftEyeLocation.Height / 2) - (RightEyeLocation.Y + LeftEyeLocation.Height / 2)) / 2;
                
                // Check distance between eyes and nose
                if (!NoseLocation.IsEmpty)
                {
                    // Check center         
                    if (Math.Abs((FaceLocation.X + FaceLocation.Width / 2) - (NoseLocation.X + NoseLocation.Width / 2)) > FaceLocation.Width / 50)
                    {
                        return false;
                    }

                    var noseEyesDist = NoseLocation.Y + NoseLocation.Height - eyesRef;
                }

                // Check distance between eyes and mouth
                if (!MouthLocation.IsEmpty)
                {

                    // Check center         
                    if (Math.Abs((FaceLocation.X + FaceLocation.Width / 2) - (MouthLocation.X + MouthLocation.Width / 2)) > FaceLocation.Width / 40)
                    {
                        return false;
                    }

                    var mouthEyesDist = MouthLocation.Y + MouthLocation.Height / 2 - eyesRef;
                 }

                return true; 
            }
        }

        public int Count
        {
            get
            {
                var count = 0;
                if (!FaceLocation.IsEmpty) count++;
                if (!LeftEyeLocation.IsEmpty) count++;
                if (!RightEyeLocation.IsEmpty) count++;
                if (!NoseLocation.IsEmpty) count++;
                if (!MouthLocation.IsEmpty) count++;

                // Full face is the best what ever if several faces
                if (count == 5) count = 15; 
                return count;
            }
        }

        public void AddEyes(Rectangle[] eyes)
        {
            if (eyes.Length > 0)
            {
                Rectangle eyeRect1 = eyes[0];
                eyeRect1.Offset(ProbableEyeLocation.X, ProbableEyeLocation.Y);

                if (eyes.Length > 1)
                {
                    Rectangle eyeRect2 = eyes[1];
                    eyeRect2.Offset(ProbableEyeLocation.X, ProbableEyeLocation.Y);
                    
                    // Check eyes detected are not both on left or on right 
                    if ((eyeRect1.X < FaceLocation.X + (FaceLocation.Width / 2)
                        && eyeRect2.X < FaceLocation.X + (FaceLocation.Width / 2))
                        ||(eyeRect1.X > FaceLocation.X + (FaceLocation.Width / 2)
                        && eyeRect2.X > FaceLocation.X + (FaceLocation.Width / 2)))
                    {
                        // Do not add eyes
                        return;
                    }

                    if (eyeRect1.X < eyeRect2.X)
                    {
                        LeftEyeLocation = eyeRect1;
                        RightEyeLocation = eyeRect2;
                    }
                    else
                    {
                        LeftEyeLocation = eyeRect2;
                        RightEyeLocation = eyeRect1;
                    }

                    // Set same zone size for both eyes
                    if (LeftEyeLocation.Width > RightEyeLocation.Width)
                    {
                        RightEyeLocation = Rectangle.Inflate(RightEyeLocation, (LeftEyeLocation.Width - RightEyeLocation.Width) / 2, 0);
                    }
                    else
                    {
                        LeftEyeLocation = Rectangle.Inflate(LeftEyeLocation, (RightEyeLocation.Width - LeftEyeLocation.Width) / 2, 0);
                    }

                    if (LeftEyeLocation.Height > RightEyeLocation.Height)
                    {
                        RightEyeLocation = Rectangle.Inflate(RightEyeLocation, 0, (LeftEyeLocation.Height - RightEyeLocation.Height) / 2);
                    }
                    else
                    {
                        LeftEyeLocation = Rectangle.Inflate(LeftEyeLocation, 0, (RightEyeLocation.Height - LeftEyeLocation.Height) / 2);
                    }
                }
                else
                {
                    if (eyeRect1.X < FaceLocation.X + (FaceLocation.Width / 2))
                    {
                        LeftEyeLocation = eyeRect1;
                    }
                    else
                    {
                        RightEyeLocation = eyeRect1;
                    }
                }
            }
        }

        public void AddNose(Rectangle[] nose)
        {
            if (nose.Length > 0)
            {
                Rectangle noseRect = nose[0];
                noseRect.Offset(ProbableNoseLocation.X, ProbableNoseLocation.Y);
                NoseLocation = noseRect;
            }
        }

        public void AddMouth(Rectangle[] mouth)
        {
            if (mouth.Length > 0)
            {
                Rectangle mouthRect = mouth[0];
                mouthRect.Offset(ProbableMouthLocation.X, ProbableMouthLocation.Y);
                MouthLocation = mouthRect;
            }
        }

        public void DrawToImage(Image<Bgr, byte> image, int thickness, bool includeEyesNoseMouth, bool includeInterestAreas)
        {
            image.Draw(FaceLocation, new Bgr(Color.Blue), thickness);

            if (includeEyesNoseMouth)
            {
                if (!LeftEyeLocation.IsEmpty)
                    image.Draw(LeftEyeLocation, new Bgr(Color.Blue), thickness);

                if (!RightEyeLocation.IsEmpty)
                    image.Draw(RightEyeLocation, new Bgr(Color.Blue), thickness);

                if (!NoseLocation.IsEmpty)
                    image.Draw(NoseLocation, new Bgr(Color.Blue), thickness);

                if (!MouthLocation.IsEmpty)
                    image.Draw(MouthLocation, new Bgr(Color.Blue), thickness);
            }

            if (includeInterestAreas)
            {
                image.Draw(ProbableEyeLocation, new Bgr(Color.Magenta), thickness);
                image.Draw(ProbableNoseLocation, new Bgr(Color.Magenta), thickness);
                image.Draw(ProbableMouthLocation, new Bgr(Color.Magenta), thickness);    
            }
        }

        private static int _faceWidth = 625;
        private static int _faceHeight = 800;

        private static int _eyeWidth = 170;
        private static int _eyeHeight = 210;

        private static int _noseWidth = 185;
        private static int _noseHeight = 245;

        private static int _mouthWidth = 385;
        private static int _mouthHeight = 240;

        private static Rectangle CalcRectangleSize(Rectangle rectangleIn, float scale, int width, int height)
        {
            Rectangle rectangleOut = new Rectangle();
            rectangleOut.Height = (int)(height / scale);
            rectangleOut.Width = (int)(width / scale);
            rectangleOut.X = rectangleIn.X + (rectangleIn.Width - rectangleOut.Width) / 2;
            rectangleOut.Y = rectangleIn.Y + (rectangleIn.Height - rectangleOut.Height) / 2;
            return rectangleOut;
        }

        public Image<Bgr, byte> GetFaceImage(Image<Bgr, byte> image)
        {
            float scale = (float)_faceHeight / (float)(FaceLocation.Height * 1.4);
            return ScaleImage(image, CalcRectangleSize(FaceLocation, scale, _faceWidth, _faceHeight), 0, scale);
        }

        public String Save(String path, int i, Image<Bgr, byte> image)
        {
            try
            {
                var dirPath = path + Path.DirectorySeparatorChar + i;
                Directory.CreateDirectory(dirPath);
            
                float scale = (float)_faceHeight / (float)(FaceLocation.Height*1.4);

                ScaleSaveAndRunPhotoshopAction(image, CalcRectangleSize(FaceLocation, scale, _faceWidth, _faceHeight),
                    0, scale, _faceWidth, _faceHeight, -1, -1, "face", dirPath);
               
                String result = "Résultat accquision face " + i + " :";
                
                if (!LeftEyeLocation.IsEmpty)
                {
                    ScaleSaveAndRunPhotoshopAction(image, CalcRectangleSize(LeftEyeLocation, scale, _eyeWidth, _eyeHeight),
                        0, scale, _eyeWidth, _eyeHeight, 0, 0, "leftEye", dirPath);
                    result += " oeil gauche OK - ";
                }
                else
                {
                    result += " oeil gauche KO - ";
                }

                if (!RightEyeLocation.IsEmpty)
                {
                    ScaleSaveAndRunPhotoshopAction(image, CalcRectangleSize(RightEyeLocation, scale, _eyeWidth, _eyeHeight),
                        0, scale, _eyeWidth, _eyeHeight, 0, 0, "rightEye", dirPath);
                    result += " oeil droit OK - ";
                }
                else
                {
                    result += " oeil droit KO - ";
                }

                if (!NoseLocation.IsEmpty)
                {
                    ScaleSaveAndRunPhotoshopAction(image, CalcRectangleSize(NoseLocation, scale, _noseWidth, _noseHeight),
                        -(image.Height - FaceLocation.Height) / 12, scale, _noseWidth, _noseHeight, 0, 0, "nose", dirPath);
                    result += " nez OK - ";
                }
                else
                {
                    result += " nez KO - ";
                } 
                
                if (!MouthLocation.IsEmpty)
                {
                    ScaleSaveAndRunPhotoshopAction(image, CalcRectangleSize(MouthLocation, scale, _mouthWidth, _mouthHeight),
                        0, scale, _mouthWidth, _mouthHeight, 0, 0, "mouth", dirPath);
                    result += " bouche OK / ";
                }
                else
                {
                    result += " bouche KO / ";
                }
                return result;
            }
            catch
            {
                return "Erreur d'acquisition. Veuillez recommencer";
            }
        }

        private static void ScaleSaveAndRunPhotoshopAction(Image<Bgr, byte> image, Rectangle rect, int moveRectY, float scale, int targetWidth, int targetHeight, int destX, int destY, String nameFacePart, string dirPath) 
        {
            var imageCutedAndScale = ScaleImage(image, rect, moveRectY, scale);
            var target = SetOnTarget(imageCutedAndScale, targetWidth, targetHeight, destX, destY);
            SaveImage(target, nameFacePart, dirPath);

            if (!nameFacePart.Equals("face"))
            {
                var filter = FacesCreationLib.Properties.Settings.Default.Filter;

                if (!Directory.Exists(dirPath + @"/" + filter))
                {
                    Directory.CreateDirectory(dirPath + @"/" + filter);
                }
                RunPhotoshopAction(dirPath + @"/" + nameFacePart + ".png",
                    dirPath + @"/" + filter + @"/" + nameFacePart + ".png",
                    nameFacePart,
                    filter);
            }
            else
            {
                var filter = "Star";

                if (!Directory.Exists(dirPath + @"/" + filter))
                {
                    Directory.CreateDirectory(dirPath + @"/" + filter);
                }

                for (var i=0; i < 4; i++) 
                {
                    var nameDest = nameFacePart + i;
                    RunPhotoshopAction(dirPath + @"/" + nameFacePart + ".png",
                        dirPath + @"/" + filter + @"/" + nameDest + ".png",
                        nameDest,
                        filter);
                }
             }
        }

        public static Image<Bgr, byte> ScaleImage(Image<Bgr, byte> image, Rectangle rect, int moveRectY, float scale)
        {
            if (rect.Y + (int)(moveRectY / scale) > 0)
            {
                rect.Y += (int)(moveRectY / scale);
            }
            else 
            { 
                rect.Y = 0; 
            }

            return image.Copy(rect)
                .Resize((int)(rect.Width * scale), (int)(rect.Height * scale), Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
        }

        public static Bitmap SetOnTarget(Image<Bgr, byte> image, int targetWidth, int targetHeight, int destX, int destY)
        {
            // Create image finalHeight x finalWidth
            var target = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);

            var graphics = Graphics.FromImage(target);
            graphics.CompositingMode = CompositingMode.SourceOver;

            if (destX == -1)
            {   
                // Center
                destX = (targetWidth - image.Width) / 2;
            }

            if (destY == -1)
            {
                // Center
                destY = (targetHeight - image.Height) / 2;
            }

            graphics.DrawImage(image.ToBitmap(), destX, destY);

            return target;
        }

        public static void SaveImage(Bitmap target, String nameFacePart, string dirPath) 
        {
            // Set resolution to 72 dpi before saving image
            target.SetResolution(72.0f, 72.0f);
            target.Save(dirPath + @"/" + nameFacePart + ".png");
        }

        public static void RunPhotoshopAction(string orig, string target, string action, string group)
        {
            Application app = new Application();
            var doc = app.Open(orig);
            app.DoAction(action, group);
            var options = new PNGSaveOptions();
            doc.SaveAs(target, options, true, PsExtensionType.psLowercase);
            doc.Close(PsSaveOptions.psDoNotSaveChanges);
        }
    }
}
