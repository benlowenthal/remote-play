using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Desktop;
using vJoy.Wrapper;

namespace waninput2
{
    class Server
    {
        private static int capWidth = 1920;
        private static int capHeight = 1080;

        static void Main(string[] args)
        {
            VirtualJoystick vj = new VirtualJoystick(0);
            vj.Aquire();

            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            UdpClient udp = new UdpClient(int.Parse(args[0]));

            IPEndPoint ep = new IPEndPoint(0, 0);
            byte[] data = udp.Receive(ref ep);

            capWidth = int.Parse(args[1]);
            capHeight = int.Parse(args[2]);
        }

        public static Bitmap Capture()
        {
            Rectangle r = Screen.GetBounds(Point.Empty);
            Bitmap capture = new Bitmap(r.Width, r.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(capture))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, r.Size);
            }

            return Protocol.Rescale(capture, capWidth, capHeight);
        }
    }
}
