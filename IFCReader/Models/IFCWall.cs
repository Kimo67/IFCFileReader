using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace IFCReader.Models
{
    public class IFCWall : IFCFigure
    {
        private readonly List<Vector3> bottom;
        private readonly Vector3 extrusion;
        private readonly Vector3 color;

        private int vao, vbo;

        public IFCWall(List<Vector3> profilePoints, Vector3 extrusionVector, Vector3 rgb)
        {
            bottom    = profilePoints;
            extrusion = extrusionVector;
            color     = rgb;
        }

        public void InitializeBuffers()
        {
            var lineData = new List<float>();
            int n = bottom.Count;

            for (int i = 0; i < n; i++)
            {
                var a     = bottom[i];
                var b     = bottom[(i + 1) % n];
                var aTop  = a + extrusion;
                var bTop  = b + extrusion;

                AddLine(a,    b);
                AddLine(aTop, bTop);
                AddLine(a,    aTop);
            }

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          lineData.Count * sizeof(float),
                          lineData.ToArray(),
                          BufferUsageHint.StaticDraw);

            // positions
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // couleurs
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);

            // local function pour pousser une arête + couleur
            void AddLine(Vector3 p, Vector3 q)
            {
                lineData.AddRange(new float[]
                {
                    p.X, p.Y, p.Z,  color.X, color.Y, color.Z,
                    q.X, q.Y, q.Z,  color.X, color.Y, color.Z
                });
            }
        }

        public override void Render()
        {
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, bottom.Count * 3 * 2);
            GL.BindVertexArray(0);
        }
    }
}
