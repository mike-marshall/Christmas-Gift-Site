using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace PolarExpress3.Utils
{
    public class ImgUtils
    {
        public static string CropToBase64Circle(byte[] rawImg)
        {            
            string base64 = "";

            using (MemoryStream msOrig = new MemoryStream())
            {
                msOrig.Write(rawImg, 0, rawImg.Length);

                using (Bitmap bm = (Bitmap)Image.FromStream(msOrig))
                {
                    using (Bitmap bt = new Bitmap(bm.Width, bm.Height))
                    { 
                        Circle circ = CircleDimensions(bm.Width, bm.Height);
                        Graphics g = Graphics.FromImage(bt);
                        GraphicsPath gp = new GraphicsPath();
                        gp.AddEllipse(circ.X, circ.Y, circ.Width, circ.Height);
                        g.Clear(Color.Magenta);
                        g.SetClip(gp);
                        g.DrawImage(bm, new
                        Rectangle(0, 0, bm.Width, bm.Height), 0, 0, bm.Width, bm.Height, GraphicsUnit.Pixel);
                        g.Dispose();
                        bt.MakeTransparent(Color.Magenta);

                        using (MemoryStream ms = new MemoryStream())
                        {
                            bt.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                            byte[] imgBytes = ms.ToArray();
                            base64 = Convert.ToBase64String(imgBytes);
                        }
                    }
                }

                return base64;
            }            
        }

        public static Circle CircleDimensions(int width, int height)
        {
            Circle c = new Circle();
            if (width <= height)
            {
                int halfWidth = width / 2;
                int halfHeight = height / 2;

                c.Radius = halfWidth;
                c.X = 0;
                c.Y = halfHeight - halfWidth;
                c.Width = width;
                c.Height = width;
            }
            else
            {
                int halfWidth = width / 2;
                int halfHeight = height / 2;

                c.Radius = halfWidth;
                c.X = halfWidth - halfHeight;
                c.Y = 0;
                c.Width = height;
                c.Height = height;
            }

            return c;
        }
    }

    public class Circle
    {
        public int X;
        public int Y;
        public int Width;
        public int Height; 
        public int Radius;
    }
}
