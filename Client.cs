using System;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace waninput2
{
    class Client
    {
        public static void Main(string[] args)
        {
            using ClientWindow c = new ClientWindow(1280, 720, "Remote Play Client", 60f, new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])));
            c.Run();
        }
    }

    class ClientWindow : GameWindow
    {
        private int texture;

        private readonly float[] vertices = {
             1f,  1f, 0f, 1f, 0f,     // top right
             1f, -1f, 0f, 1f, 1f,     // bottom right
            -1f, -1f, 0f, 0f, 1f,     // bottom left
            -1f,  1f, 0f, 0f, 0f      // top left
        };

        private readonly uint[] indices = {
            0, 1, 3,    // first triangle
            1, 2, 3     // second triangle
        };

        private int vao;
        private int vertexBuffer;
        private int elementBuffer;

        private Shader shader;

        private UdpClient udp;
        private IPEndPoint endp;
        private Bitmap frame;

        private bool unloaded = false;

        public ClientWindow(int w, int h, string title, float freq, IPEndPoint ep) : base(
            new GameWindowSettings() {
                RenderFrequency = freq,
                UpdateFrequency = freq
            },
            new NativeWindowSettings()
            {
                Size = new OpenTK.Mathematics.Vector2i(w, h),
                Title = "Remote Play Client - " + ep.ToString()
            })
        {
            //setup sockets
            udp = new UdpClient();
            endp = ep;

            byte[] dgram = Protocol.Datagram(Protocol.CONNECT, Array.Empty<byte>());
            udp.Send(dgram, dgram.Length, ep);
            System.Diagnostics.Debug.WriteLine("Sent {0} to {1}", Protocol.CONNECT.ToString(), ep.ToString());

            frame = new Bitmap(w, h);

            Thread frameListen = new Thread(new ThreadStart(FrameListen));
            frameListen.Start();

            Thread controls = new Thread(new ThreadStart(SendControls));
            controls.Start();

            Unload += delegate () { unloaded = true; };
        }

        private void DrawImage(Bitmap image)
        {
            int width = Size.X;
            int height = Size.Y;

            image = Protocol.Rescale(image, width, height);

            BitmapData bmp = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb8, width, height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, bmp.Scan0);
            image.UnlockBits(bmp);
            image.Dispose();

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        }

        protected override void OnLoad()
        {
            shader = new Shader();
            GLFW.Init();

            //setup vertex array object
            vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            //setup render buffers
            vertexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StreamDraw);

            elementBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StreamDraw);

            //make vertex atrributes
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = GL.GetAttribLocation(shader.handle, "aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));

            //create texture to write bitmap to
            GL.GenTextures(1, out texture);
            GL.BindTexture(TextureTarget.Texture2D, texture);

            base.OnLoad();
        }

        protected override void OnUnload()
        {
            GL.DeleteVertexArray(vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.DeleteBuffer(vertexBuffer);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.DeleteBuffer(elementBuffer);

            shader.Dispose();
            frame.Dispose();

            byte[] dg = Protocol.Datagram(Protocol.DISCONNECT, Array.Empty<byte>());
            udp.Send(dg, dg.Length);
            System.Diagnostics.Debug.WriteLine("Sent disconnect token");
            udp.Close();

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shader.Use();

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            DrawImage(frame);

            //openGL required
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape)) Dispose();

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            if (e.Height * 16 < e.Width * 9)
            {
                int viewWidth = e.Height * 16 / 9;
                GL.Viewport((e.Width - viewWidth) / 2, 0, viewWidth, e.Height);
            }
            else
            {
                int viewHeight = e.Width * 9 / 16;
                GL.Viewport(0, (e.Height - viewHeight) / 2, e.Width, viewHeight);
            }
            base.OnResize(e);
        }

        private void FrameListen()
        {
            //listens for frames sent from server
            udp.Connect(endp);
            System.Diagnostics.Debug.WriteLine("Bound to " + endp.ToString());
            byte[][] frameBuffer = Array.Empty<byte[]>();

            while (!unloaded)
            {
                try
                {
                    byte[] dgram = udp.Receive(ref endp);
                    System.Diagnostics.Debug.WriteLine("Received " + dgram.Length.ToString() + " bytes from " + endp.ToString());
                    if (dgram[0] == Protocol.FRAME)
                    {
                        if (dgram[1] == Protocol.COMPLETE)
                            frame = Protocol.Decode(dgram[2..]);
                        else
                        {
                            if (frameBuffer.Length != dgram[2])
                                frameBuffer = new byte[dgram[2]][];
                            frameBuffer[dgram[1]] = dgram[3..];

                            bool full = true;
                            foreach (byte[] b in frameBuffer)
                                if (b == null)
                                    full = false;

                            //reconstruct frame from buffer
                            if (full)
                            {
                                List<byte> frameConstruct = new List<byte>(frameBuffer[0]);
                                for (int i = 1; i < frameBuffer.Length; i++)
                                    frameConstruct.AddRange(frameBuffer[i]);

                                frameBuffer = Array.Empty<byte[]>();
                                frame = Protocol.Decode(frameConstruct.ToArray());
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    System.Diagnostics.Debug.WriteLine("Receive failed at " + DateTime.Now.ToString());
                }
            }
        }

        private void SendControls()
        {
            //controller setup
            int jid = -1;
            for (int x = 0; x < 4; x++)
                if (GLFW.JoystickIsGamepad(x))
                    jid = x;

            if (jid == -1)
            {
                System.Diagnostics.Debug.WriteLine("No controller found");
                return;
            }

            Span<float> axes = new Span<float>(new float[6]);
            JoystickHats hats = new JoystickHats();
            JoystickInputAction[] buts = new JoystickInputAction[10];

            Span<float> newAxes;
            JoystickHats newHats;
            JoystickInputAction[] newButs;

            while (!unloaded)
            {
                GLFW.PollEvents();

                newAxes = GLFW.GetJoystickAxes(jid);
                newHats = GLFW.GetJoystickHats(jid)[0];
                newButs = GLFW.GetJoystickButtons(jid)[..10];

                if (!axes.SequenceEqual(newAxes))
                {
                    List<byte> data = new List<byte>();
                    foreach (float f in newAxes)
                    {
                        //convert to vJoy values
                        short val = (short)((f * 16500) + 16500);
                        data.AddRange(BitConverter.GetBytes(val));
                    }
                    System.Diagnostics.Debug.WriteLine("Sending " + string.Join(" ", newAxes.ToArray()));
                    byte[] dgram = Protocol.Datagram(Protocol.CONTROL, Protocol.AXIS, data.ToArray());
                    udp.Send(dgram, dgram.Length);
    
                    newAxes.CopyTo(axes);
                }

                if (hats != newHats)
                {
                    int val = -1;
                    List<int> pressed = new List<int>();

                    //vJoy continuous POV values
                    if (newHats.HasFlag(JoystickHats.Up)) pressed.Add(0);
                    if (newHats.HasFlag(JoystickHats.Left)) pressed.Add(8975);
                    if (newHats.HasFlag(JoystickHats.Down)) pressed.Add(17950);
                    if (newHats.HasFlag(JoystickHats.Right)) pressed.Add(26925);

                    if (pressed.Count > 0)
                    {
                        foreach (int x in pressed) val += x;
                        val = (val + 1) / pressed.Count;
                    }

                    byte[] data = BitConverter.GetBytes((short)val);
                    System.Diagnostics.Debug.WriteLine("Sending " + string.Join(" ", data));
                    byte[] dgram = Protocol.Datagram(Protocol.CONTROL, Protocol.HAT, data);
                    udp.Send(dgram, dgram.Length);

                    hats = newHats;
                }

                if (!buts.Equals(newButs))
                {
                    List<byte> data = new List<byte>();

                    foreach (JoystickInputAction a in newButs)
                    {
                        if (a == JoystickInputAction.Press) data.Add(BitConverter.GetBytes(true)[0]);
                        else data.Add(BitConverter.GetBytes(false)[0]);
                    }

                    System.Diagnostics.Debug.WriteLine("Sending " + string.Join(" ", data));
                    byte[] dgram = Protocol.Datagram(Protocol.CONTROL, Protocol.BUTTON, data.ToArray());
                    udp.Send(dgram, dgram.Length);

                    Array.Copy(newButs, buts, 10);
                }

                Thread.CurrentThread.Join(1000 / 30);
            }

            GLFW.Terminate();
        }
    }
}
