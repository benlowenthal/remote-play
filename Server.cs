using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;
using vJoy.Wrapper;

namespace waninput2
{
    class Server
    {
        private static int capWidth = 1280;
        private static int capHeight = 720;

        private static UdpClient udp;
        private static Dictionary<IPEndPoint, VirtualJoystick> connections = new Dictionary<IPEndPoint, VirtualJoystick>(4);

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

            while (true)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                byte[] dgram = udp.Receive(ref ep);
                System.Diagnostics.Debug.WriteLine("Datagram received from " + ep.ToString());

                if (dgram[0] == Protocol.CONNECT)
                {
                    VirtualJoystick vj = new VirtualJoystick(1);
                    vj.Aquire();
                    System.Diagnostics.Debug.WriteLine((vj.Aquired ? "Acquired" : "Failed to acquire") + " vJoy device for " + ep.Address.ToString());

                    connections.Add(ep, vj);
                }
                else if (dgram[0] == Protocol.DISCONNECT)
                {
                    System.Diagnostics.Debug.WriteLine(ep.ToString() + " disconnected");
                    break;
                }
                else if (dgram[0] == Protocol.CONTROL)
                {
                    if (connections[ep].Aquired)
                        ParseControl(connections[ep], dgram[1..]);
                }
            }

            //cleanup
            udp.Close();

            foreach ((_, VirtualJoystick v) in connections)
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
            while (udp.Client != null)
            {
                byte[] frame = Protocol.Encode(Capture());

                if (frame.Length < 60000)
                {
                    foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                        {
                            byte[] dgram = Protocol.Datagram(Protocol.FRAME, Protocol.COMPLETE, frame);
                            System.Diagnostics.Debug.WriteLine("Sending " + dgram.Length.ToString() + " bytes to " + endpoint.ToString());
                            if (udp.Client != null) udp.Send(dgram, dgram.Length, endpoint);
                        }
                }
                else
                {
                    for (int i = 0; i < frame.Length; i += 60000)
                        foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                            {
                                byte[] dgram = Protocol.Datagram(Protocol.FRAME, (byte)(i/60000), (byte)(frame.Length/60000 + 1), frame[i..Math.Min(i + 60000, frame.Length)]);
                                System.Diagnostics.Debug.WriteLine("Sending " + dgram.Length.ToString() + " bytes to " + endpoint.ToString());
                                if (udp.Client != null) udp.Send(dgram, dgram.Length, endpoint);
                            }
                }

                Thread.CurrentThread.Join(1000 / 30); //hertz
            }
        }

        private static void ParseControl(VirtualJoystick vj, byte[] data)
        {
            System.Diagnostics.Debug.WriteLine("Received " + string.Join(" ", data));

            if (data[0] == Protocol.AXIS)
            {
                for (int i = 0; i < 6; i++)
                {
                    vj.SetJoystickAxis(BitConverter.ToUInt16(data.AsSpan()[((i * 2) + 1)..((i * 2) + 3)]), Enum.Parse<Axis>(i.ToString()));
                }
            }
            else if (data[0] == Protocol.BUTTON)
            {
                for (int i = 1; i < 11; i++)
                {
                    vj.SetJoystickButton(BitConverter.ToBoolean(new byte[] { data[i] }), (byte)(i - 1));
                }
            }
            else if (data[0] == Protocol.HAT)
            {
                vj.SetJoystickHat(BitConverter.ToUInt16(data.AsSpan()[1..]), Hats.Hat);
            }

            vj.Update();
        }
    }
}
