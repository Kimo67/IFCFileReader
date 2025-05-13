using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace IFCReader.Models
{
    /// Représente un poteau.
    public class IFCColumn : IFCFigure, IDisposable
    {
        private readonly int vbo;
        private readonly int vao;
        private readonly int vertexCount;
        private readonly Vector3 color;

        public IFCColumn(IEnumerable<Vector3> baseLoop, Vector3 extrusion, Vector3 color)
        {
            this.color = color;

            // génère les 8 sommets du prisme (base + sommet)
            var bottom = new List<Vector3>(baseLoop);
            var top    = bottom.ConvertAll(p => p + extrusion);

            // génère un grillage (lines) : arêtes + contours base & haut
            var lines = new List<Vector3>();

            for (int i = 0; i < bottom.Count; i++)
            {
                int nxt = (i + 1) % bottom.Count;
                // arêtes verticales
                lines.Add(bottom[i]); lines.Add(top[i]);
                // base
                lines.Add(bottom[i]); lines.Add(bottom[nxt]);
                // haut
                lines.Add(top[i]);    lines.Add(top[nxt]);
            }

            vertexCount = lines.Count;

            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer,
                          lines.Count * Vector3.SizeInBytes,
                          lines.ToArray(),
                          BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);
            GL.EnableVertexAttribArray(0);
        }

        public override void Render()
        {
            GL.LineWidth(2f);
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }
}
