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
        public static byte CONNECT = 1;
        public static byte DISCONNECT = 2;
        public static byte FRAME = 3;
        public static byte CONTROL = 4;

        public static byte BUTTON = 11;
        public static byte AXIS = 12;
        public static byte HAT = 13;

        public static byte[] Datagram(byte command, byte[] data)
        {
            List<byte> d = new List<byte>(data);
            d.Insert(0, command);
            Console.WriteLine(string.Join(' ', d));
            return d.ToArray();
        }

        public static byte[] Datagram(byte command, byte subcommand, byte[] data)
        {
            List<byte> d = new List<byte>(data);
            d.Insert(0, subcommand);
            d.Insert(0, command);
            Console.WriteLine(string.Join(' ', d));
            return d.ToArray();
        }

        public static byte[] Encode(Bitmap b)
        {
            using MemoryStream temp = new MemoryStream();
            using (DeflateStream ds = new DeflateStream(temp, CompressionMode.Compress))
            {
                b.Save(ds, ImageFormat.Jpeg);
            }

            b.Dispose();

            return temp.ToArray();
        }

        public static Bitmap Decode(byte[] s)
        {
            using MemoryStream m = new MemoryStream();
            using MemoryStream temp = new MemoryStream(s);
            using DeflateStream ds = new DeflateStream(temp, CompressionMode.Decompress);
            ds.CopyTo(m);

            return new Bitmap(m);
        }

        public static Bitmap Rescale(Bitmap b, int width, int height)
        {
            Rectangle r = new Rectangle(0, 0, width, height);
            Bitmap scaled = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(scaled))
            {
                g.CompositingQuality = CompositingQuality.HighSpeed;
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(b, r, 0, 0, b.Width, b.Height, GraphicsUnit.Pixel);
            }

            return scaled;
        }
    }
}
