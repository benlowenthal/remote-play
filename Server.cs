#pragma warning disable IDE0090
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
        private static Dictionary<IPEndPoint, uint> connections = new Dictionary<IPEndPoint, uint>();
        private static VJoyControllerManager vjManager;

        private static CancellationTokenSource broadcastToken = new CancellationTokenSource();

        private static bool open = false;
        private static uint[] availableVJ = new uint[] { 1, 2, 3, 4 };

        public static void Main(string[] _)
        {
            ServerForm f = new ServerForm();
            f.ShowDialog();
        }

        public static void Run(int port, int w, int h)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            udp = new UdpClient(ip);

            open = true;
            capWidth = w;
            capHeight = h;

            //spawn threads
            Thread t = new Thread(new ParameterizedThreadStart(Broadcast));
            t.Start(broadcastToken.Token);

            vjManager = VJoyControllerManager.GetManager();
            while (open)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                byte[] dgram = new byte[2];

                try { dgram = udp.Receive(ref ep); }
                catch (SocketException) { }
                System.Diagnostics.Debug.WriteLine("Datagram received from " + ep.ToString());

                if (dgram[0] == Protocol.CONNECT)
                {
                    Connect(ep);
                }
                else if (dgram[0] == Protocol.DISCONNECT)
                {
                    //clean dict and vJoy device
                    System.Diagnostics.Debug.WriteLine(ep.ToString() + " disconnected");
                    availableVJ[connections[ep] - 1] = connections[ep];
                    connections.Remove(ep);
                }
                else if (dgram[0] == Protocol.CONTROL)
                {
                    if (!connections.ContainsKey(ep)) Connect(ep);
                    ParseControl(vjManager, connections[ep], dgram[1..]);
                }

                System.Diagnostics.Debug.WriteLine(string.Join(",", connections.Keys));
            }
        }

        private static void Connect(IPEndPoint ep)
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
                System.Diagnostics.Debug.WriteLine((vjManager.IsVJoyEnabled ? "Acquired" : "Failed to acquire") + " vJoy device for " + ep.Address.ToString());
                connections.Add(ep, vjID);
                availableVJ[vjID - 1] = 0;
            }
            else System.Diagnostics.Debug.WriteLine("No vJoy devices remaining for " + ep.Address.ToString());
        }

        public static void Close() {
            //cleanup
            open = false;
            broadcastToken.Cancel();
            udp.Dispose();
            vjManager.Dispose();
            connections = new Dictionary<IPEndPoint, uint>();
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

        private static void Broadcast(object tk)
        {
            CancellationToken token = (CancellationToken)tk;
            while (!token.IsCancellationRequested)
            {
                Capture(out Bitmap f);
                byte[] frame = Protocol.Encode(ref f);

                if (frame.Length < 60000)
                {
                    foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                        {
                            if (!open) break;
                            byte[] dgram = Protocol.Datagram(Protocol.FRAME, Protocol.COMPLETE, frame);
                            System.Diagnostics.Debug.WriteLine("Sending " + dgram.Length.ToString() + " bytes to " + endpoint.ToString());
                            try { udp.Send(dgram, dgram.Length, endpoint); }
                            catch (SocketException) { }
                        }
                }
                else
                {
                    for (int i = 0; i < frame.Length; i += 60000)
                        foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                            {
                                if (!open) break;
                                byte[] dgram = Protocol.Datagram(Protocol.FRAME, (byte)(i/60000), (byte)(frame.Length/60000 + 1), frame[i..Math.Min(i + 60000, frame.Length)]);
                                System.Diagnostics.Debug.WriteLine("Sending " + dgram.Length.ToString() + " bytes to " + endpoint.ToString());
                                try { udp.Send(dgram, dgram.Length, endpoint); }
                                catch (SocketException) { }
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
