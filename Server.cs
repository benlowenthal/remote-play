using System;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;
using CoreDX.vJoy.Wrapper;

namespace waninput2
{
    class Server
    {
        private static int capWidth = 1280;
        private static int capHeight = 720;

        private static UdpClient udp;
        private static Dictionary<IPEndPoint, uint> connections = new Dictionary<IPEndPoint, uint>(4);

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

            //Thread cl = new Thread(new ThreadStart(StartLocalClient));
            //cl.Start();

            uint[] availableVJ = new uint[] { 1, 2, 3, 4 };
            VJoyControllerManager vjManager = VJoyControllerManager.GetManager();
            while (true)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                byte[] dgram = udp.Receive(ref ep);
                System.Diagnostics.Debug.WriteLine("Datagram received from " + ep.ToString());

                if (dgram[0] == Protocol.CONNECT)
                {
                    //assign available vJoy device
                    uint vjID = 0;
                    foreach (uint id in availableVJ)
                        if (id != 0)
                        {
                            vjID = id;
                            break;
                        }

                    if (vjID != 0)
                    {
                        //IVJoyController vj = vjManager.AcquireController(vjID);
                        System.Diagnostics.Debug.WriteLine((vjManager.IsVJoyEnabled ? "Acquired" : "Failed to acquire") + " vJoy device for " + ep.Address.ToString());
                        availableVJ[vjID - 1] = 0;
                        connections.Add(ep, vjID);
                    }
                    else System.Diagnostics.Debug.WriteLine("No vJoy devices remaining for " + ep.Address.ToString());
                }
                else if (dgram[0] == Protocol.DISCONNECT)
                {
                    //clean dict and vJoy device
                    System.Diagnostics.Debug.WriteLine(ep.ToString() + " disconnected");
                    availableVJ[connections[ep] - 1] = connections[ep];
                    connections.Remove(ep);
                    break;
                }
                else if (dgram[0] == Protocol.CONTROL)
                {
                    ParseControl(vjManager, connections[ep], dgram[1..]);
                }
            }

            //cleanup
            udp.Close();
            vjManager.Dispose();
        }

        private static void StartLocalClient()
        {
            Client.Main(new string[] { "127.0.0.1", "4242" });
        }

        private static void Capture(out Bitmap capture)
        {
            Rectangle r = Screen.GetBounds(Point.Empty);
            capture = new Bitmap(r.Width, r.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(capture))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, r.Size);
            }

            Protocol.Rescale(ref capture, capWidth, capHeight);
        }

        private static void Broadcast()
        {
            while (udp.Client != null)
            {
                Capture(out Bitmap f);
                byte[] frame = Protocol.Encode(ref f);

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

        private static void ParseControl(VJoyControllerManager manager, uint id, byte[] data)
        {
            IVJoyController vj = manager.AcquireController(id);
            System.Diagnostics.Debug.WriteLine("Received " + string.Join(" ", data));

            if (data[0] == Protocol.AXIS)
            {
                vj.SetAxisX(BitConverter.ToUInt16(data.AsSpan()[1..3]));
                vj.SetAxisY(BitConverter.ToUInt16(data.AsSpan()[3..5]));
                vj.SetAxisZ(BitConverter.ToUInt16(data.AsSpan()[5..7]));
                vj.SetAxisRx(BitConverter.ToUInt16(data.AsSpan()[7..9]));
                vj.SetAxisRy(BitConverter.ToUInt16(data.AsSpan()[9..11]));
                vj.SetAxisRz(BitConverter.ToUInt16(data.AsSpan()[11..]));
            }
            else if (data[0] == Protocol.BUTTON)
            {
                for (int i = 1; i < 11; i++)
                {
                    if (BitConverter.ToBoolean(new byte[] { data[i] })) vj.PressButton((byte)i);
                    else vj.ReleaseButton((byte)i);
                }
            }
            else if (data[0] == Protocol.HAT)
            {
                System.Diagnostics.Debug.WriteLine(vj.AxisMaxValue);
                vj.SetContPov(BitConverter.ToUInt16(data.AsSpan()[1..]), 1);
            }

            manager.RelinquishController(vj);
        }
    }
}
