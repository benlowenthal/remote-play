using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace waninput2
{
    class Shader
    {
        public int handle;
        private bool disposed = false;

        public Shader()
        {
            string vertexShaderSource;

            //using (StreamReader reader = new StreamReader(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + Path.DirectorySeparatorChar + "shader.vert", Encoding.UTF8))
            using (StreamReader reader = new StreamReader(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "shader.vert", Encoding.UTF8))
            {
                vertexShaderSource = reader.ReadToEnd();
            }

            string fragmentShaderSource;

            //using (StreamReader reader = new StreamReader(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + Path.DirectorySeparatorChar + "shader.frag", Encoding.UTF8))
            using (StreamReader reader = new StreamReader(Environment.CurrentDirectory + Path.DirectorySeparatorChar + "shader.frag", Encoding.UTF8))
            {
                fragmentShaderSource = reader.ReadToEnd();
            }

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            //create shader program
            handle = GL.CreateProgram();

            //copy shader data
            GL.AttachShader(handle, vertexShader);
            GL.AttachShader(handle, fragmentShader);

            GL.LinkProgram(handle);

            //cleanup
            GL.DetachShader(handle, vertexShader);
            GL.DetachShader(handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
        
        public void Use()
        {
            GL.UseProgram(handle);
        }

        protected virtual void Destroy()
        {
            if (!disposed)
            {
                GL.DeleteProgram(handle);
                disposed = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(handle);
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
    }
}
