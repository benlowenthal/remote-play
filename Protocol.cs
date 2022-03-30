using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace waninput2
{
    class Protocol
    {
        public static byte HANDSHAKE = 1;
        public static byte CONTROL = 2;
        public static byte FRAME = 3;

        public static byte[] Datagram(byte command, int data)
        {
            byte[] bytes = new byte[32];

            bytes[0] = command;

            int idx = 1;
            foreach (byte b in BitConverter.GetBytes(data))
            {
                bytes[idx] = b;
                idx++;
            }

            Console.WriteLine(string.Join(' ', bytes));
            return bytes;
        }

        public static byte[] Encode(Bitmap b)
        {
            using MemoryStream inp = new MemoryStream();
            b.Save(inp, ImageFormat.Bmp);            
            b.Dispose();

            return inp.ToArray();
        }

        public static Bitmap Decode(byte[] s)
        {
            using MemoryStream m = new MemoryStream();
            m.Write(s);

            return new Bitmap(m);
        }

        public static Bitmap Rescale(Bitmap b, int width, int height)
        {
            Rectangle r = new Rectangle(0, 0, width, height);
            Bitmap scaled = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            using (Graphics g2 = Graphics.FromImage(scaled))
            {
                g2.InterpolationMode = InterpolationMode.High;
                g2.SmoothingMode = SmoothingMode.HighQuality;
                g2.DrawImage(b, r, 0, 0, b.Width, b.Height, GraphicsUnit.Pixel);
            }

            return scaled;
        }
    }
}
