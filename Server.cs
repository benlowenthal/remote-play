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
using NAudio.Wave;
using System.IO;

namespace waninput2
{
    class Server
    {
        private static int capWidth = 1280;
        private static int capHeight = 720;

        private static UdpClient udp;
        private static Dictionary<IPEndPoint, uint> connections = new Dictionary<IPEndPoint, uint>();
        private static VJoyControllerManager vjManager;

        private static CancellationTokenSource cancelToken = new CancellationTokenSource();

        private static bool open = false;
        private static uint[] availableVJ = new uint[] { 1, 2, 3, 4 };

        private static RichTextBox log;

        public static void Main(string[] _)
        {
            ServerForm f = new ServerForm();
            f.ShowDialog();
        }

        public static void Run(int port, int w, int h, RichTextBox logBox)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);
            udp = new UdpClient(ip);

            open = true;
            capWidth = w;
            capHeight = h;

            log = logBox;

            //spawn threads
            Thread t = new Thread(new ParameterizedThreadStart(Broadcast));
            t.Start(cancelToken.Token);

            Thread a = new Thread(new ParameterizedThreadStart(SendAudio));
            a.Start(cancelToken.Token);

            vjManager = VJoyControllerManager.GetManager();
            while (open)
            {
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                byte[] dgram = new byte[2];

                try { dgram = udp.Receive(ref ep); }
                catch (SocketException) { }

                if (dgram[0] == Protocol.CONNECT)
                {
                    Connect(ep);
                }
                else if (dgram[0] == Protocol.DISCONNECT)
                {
                    //clean dict and vJoy device
                    ServerLog(ep.ToString() + " disconnected");
                    availableVJ[connections[ep] - 1] = connections[ep];
                    connections.Remove(ep);
                }
                else if (dgram[0] == Protocol.CONTROL)
                {
                    if (!connections.ContainsKey(ep)) Connect(ep);
                    ParseControl(vjManager, connections[ep], dgram[1..]);
                }
            }
        }

        private static void Connect(IPEndPoint ep)
        {
            ServerLog("Connected to " + ep.ToString());

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
                ServerLog((vjManager.IsVJoyEnabled ? "Acquired" : "Failed to acquire") + " vJoy device for " + ep.Address.ToString());
                connections.Add(ep, vjID);
                availableVJ[vjID - 1] = 0;
            }
            else ServerLog("No vJoy devices remaining for " + ep.Address.ToString());
        }

        public static void Close()
        {
            //cleanup
            open = false;
            cancelToken.Cancel();
            udp.Dispose();
            vjManager.Dispose();
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
                    byte[] dgram = Protocol.Datagram(Protocol.FRAME, Protocol.COMPLETE, frame);
                    foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                        {
                            if (!open) break;
                            try { udp.Send(dgram, dgram.Length, endpoint); }
                            catch (SocketException) { }
                        }
                }
                else
                {
                    for (int i = 0; i < frame.Length; i += 60000)
                    {
                        byte[] dgram = Protocol.Datagram(Protocol.FRAME, (byte)(i / 60000), (byte)(frame.Length / 60000 + 1), frame[i..Math.Min(i + 60000, frame.Length)]);
                        foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                            {
                                if (!open) break;
                                try { udp.Send(dgram, dgram.Length, endpoint); }
                                catch (SocketException) { }
                            }
                    }
                }

                Thread.CurrentThread.Join(1000 / 30); //hertz
            }
        }

        private static void ParseControl(VJoyControllerManager manager, uint id, byte[] data)
        {
            IVJoyController vj = manager.AcquireController(id);

            if (data[0] == Protocol.AXIS && vj != null)
            {
                vj.SetAxisX(BitConverter.ToUInt16(data.AsSpan()[1..3]));
                vj.SetAxisY(BitConverter.ToUInt16(data.AsSpan()[3..5]));
                vj.SetAxisZ(BitConverter.ToUInt16(data.AsSpan()[5..7]));
                vj.SetAxisRx(BitConverter.ToUInt16(data.AsSpan()[7..9]));
                vj.SetAxisRy(BitConverter.ToUInt16(data.AsSpan()[9..11]));
                vj.SetAxisRz(BitConverter.ToUInt16(data.AsSpan()[11..]));
            }
            else if (data[0] == Protocol.BUTTON && vj != null)
            {
                for (int i = 1; i < 11; i++)
                {
                    if (BitConverter.ToBoolean(new byte[] { data[i] })) vj.PressButton((byte)i);
                    else vj.ReleaseButton((byte)i);
                }
            }
            else if (data[0] == Protocol.HAT && vj != null)
                vj.SetContPov(BitConverter.ToUInt16(data.AsSpan()[1..]), 1);

            if (vj != null) manager.RelinquishController(vj);
        }

        private static void SendAudio(object tk)
        {
            MemoryStream buffer = new MemoryStream();

            WasapiLoopbackCapture audioCapture = new WasapiLoopbackCapture();
            audioCapture.WaveFormat = new WaveFormat(22050, 8, 2);
            audioCapture.StartRecording();
            audioCapture.DataAvailable += (_, a) => { buffer.Write(a.Buffer, 0, a.BytesRecorded); };

            CancellationToken token = (CancellationToken)tk;
            while (!token.IsCancellationRequested)
            {
                byte[] dgram = Protocol.Datagram(Protocol.AUDIO, buffer.ToArray());
                if (dgram.Length > 1)
                {
                    foreach ((IPEndPoint endpoint, _) in connections) if (endpoint != null)
                        {
                            if (!open) break;
                            try { udp.Send(dgram, dgram.Length, endpoint); }
                            catch (SocketException) { }
                        }

                    //empty buffer after read
                    buffer = new MemoryStream();
                }

                Thread.CurrentThread.Join(1000 / 30); //hertz
            }

            buffer.Dispose();
            audioCapture.StopRecording();
        }

        private static void ServerLog(string msg)
        {
            if (log != null)
            {
                if (log.Text.Length > 0)
                    log.AppendText("\n");
                log.AppendText(msg);
            }
        }
    }
}
