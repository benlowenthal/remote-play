using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using vJoyWrapper;

namespace waninput2
{
    class Server
    {
        private static int capWidth = 768;
        private static int capHeight = 432;

        private static UdpClient udp;
        private static Dictionary<IPEndPoint, VJoy> connections = new Dictionary<IPEndPoint, VJoy>(4);

        [DllImport("vJoyWrapper.dll")]
        private static extern void Update();

        public static void Main(string[] args)
        {
            int port = int.Parse(args[0]);
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            udp = new UdpClient(ip);

            capWidth = int.Parse(args[1]);
            capHeight = int.Parse(args[2]);

            //spawn threads
            Thread t = new Thread(new ThreadStart(Broadcast));
            t.Start();

            Thread cl = new Thread(new ThreadStart(StartLocalClient));
            cl.Start();

            //bool[] availableVJs = new bool[] { true, true, true, true };
            while (true)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                byte[] dgram = udp.Receive(ref ep);
                System.Diagnostics.Debug.WriteLine("Datagram received from " + ep.ToString());

                if (dgram[0] == Protocol.CONNECT)
                {
                    //uint vjIdx = (uint) Array.IndexOf(availableVJs, true);
                    //availableVJs[vjIdx] = false;

                    VJoy vj = new VJoy();
                    System.Diagnostics.Debug.WriteLine("Acquired vJoy device for " + ep.Address.ToString());

                    connections.Add(ep, vj);
                }
                else if (dgram[0] == Protocol.DISCONNECT)
                {
                    //uint idx = connections[ep].JoystickId;
                    //availableVJs[idx] = false;
                    System.Diagnostics.Debug.WriteLine(ep.ToString() + " disconnected");
                    break;
                }
                else if (dgram[0] == Protocol.CONTROL)
                {
                    ParseControl(connections[ep], dgram[1..]);
                }
            }

            //cleanup
            udp.Close();

            foreach ((_, VJoy v) in connections)
            {
                v.Dispose();
            }
        }

        private static void StartLocalClient()
        {
            Client.Main(new string[] { "127.0.0.1", "4242" });
        }

        private static Bitmap Capture()
        {
            Rectangle r = Screen.GetBounds(Point.Empty);
            Bitmap capture = new Bitmap(r.Width, r.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(capture))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, r.Size);
            }

            return Protocol.Rescale(capture, capWidth, capHeight);
        }

        private static void Broadcast()
        {
            while (true)
            {
                byte[] dgram = Protocol.Datagram(Protocol.FRAME, Protocol.Encode(Capture()));

                foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Sending " + dgram.Length.ToString() + " bytes to " + endpoint.ToString());
                        //udp.Send(dgram, dgram.Length, endpoint);
                    }

                Thread.CurrentThread.Join(1000 / 30); //hertz
            }
        }

        private static void ParseControl(VJoy vj, byte[] data)
        {
            if (data[0] == Protocol.AXIS)
            {
                vj.SetAxis(BitConverter.ToInt16(data.AsSpan()[1..3]), BitConverter.ToUInt16(data.AsSpan()[3..]));
            }
            else if (data[0] == Protocol.BUTTON)
            {
                vj.SetButton(data[1], BitConverter.ToBoolean(data.AsSpan()[2..]));
            }
            else if (data[0] == Protocol.HAT)
            {
                vj.SetAxis(VJoy.JoystickAxis.Dial, BitConverter.ToUInt16(data.AsSpan()[1..]));
            }

            vj.UpdateJoystick();
        }
    }
}
