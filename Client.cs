using System;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace waninput2
{
    class Client
    {
        static void Main(string[] args)
        {
            using ClientWindow c = new ClientWindow(1280, 720, "Remote Play Client", 60f, new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])));
            c.Run();
        }
    }

    class ClientWindow : GameWindow
    {
        private int width, height;

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
            width = w; height = h;

            //setup sockets
            IPAddress ip = Dns.GetHostAddresses(Dns.GetHostName())[0];

            UdpClient udp = new UdpClient(ep.Port);

            byte[] dgram = Protocol.Datagram(Protocol.HANDSHAKE, 1);
            udp.Send(dgram, dgram.Length, ep);
        }

        private void DrawImage(Bitmap image)
        {
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

            base.OnUnload();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            shader.Use();

            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

            DrawImage(Protocol.Decode(Protocol.Encode(Server.Capture())));

            //openGL required
            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

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
    }
}
