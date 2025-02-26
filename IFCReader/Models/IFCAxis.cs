using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace IFCReader.Models
{
    public class IFCAxis : IFCFigure
    {
        private Vector3 Origin { get; }
        private Vector3 XDirection { get; }
        private Vector3 ZDirection { get; }

        private int vao;
        private int vbo;
        private int vertexCount;

        public IFCAxis(string originData, string xDirectionData, string zDirectionData)
        {
            Origin = DecodeCartesianPoint(originData);
            XDirection = DecodeDirection(xDirectionData);
            ZDirection = DecodeDirection(zDirectionData);
        }

        public void InitializeBuffers()
        {
            float[] vertices = new float[]
            {
                // Axe X (rouge)
                Origin.X, Origin.Y, Origin.Z,      1.0f, 0.0f, 0.0f,
                Origin.X + XDirection.X, Origin.Y + XDirection.Y, Origin.Z + XDirection.Z, 1.0f, 0.0f, 0.0f,

                // Axe Z (bleu)
                Origin.X, Origin.Y, Origin.Z,      0.0f, 0.0f, 1.0f,
                Origin.X + ZDirection.X, Origin.Y + ZDirection.Y, Origin.Z + ZDirection.Z, 0.0f, 0.0f, 1.0f
            };
            vertexCount = 4;

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
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
            GL.BindVertexArray(0);
        }
    }
}
