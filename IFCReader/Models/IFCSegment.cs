using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IFCReader.Models
{
    public class IFCSegment : IFCFigure
    {
        public Vector3 StartPoint { get; }
        public Vector3 EndPoint { get; }

        private int vao;
        private int vbo;

        public IFCSegment(Vector3 start, Vector3 end)
        {
            StartPoint = start;
            EndPoint = end;
        }

        public void InitializeBuffers()
        {
            float[] vertices = new float[]
            {
                StartPoint.X, StartPoint.Y, StartPoint.Z, 0.0f, 1.0f, 0.0f, // Vert
                EndPoint.X, EndPoint.Y, EndPoint.Z, 0.0f, 1.0f, 0.0f       // Vert
            };

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public override void Render()
        {
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, 2);
            GL.BindVertexArray(0);
        }
    }
}
