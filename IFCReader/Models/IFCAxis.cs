using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace IFCReader.Models
{
    public class IFCAxis : IFCFigure
    {
        private Vector3 Origin { get; }
        private Vector3 XDirection { get; }
        private Vector3 ZDirection { get; }

        public IFCAxis(string originData, string xDirectionData, string zDirectionData)
        {
            Origin = DecodeCartesianPoint(originData);
            XDirection = DecodeDirection(xDirectionData);
            ZDirection = DecodeDirection(zDirectionData);
        }

        public override void Render()
        {
            GL.Begin(PrimitiveType.Lines);

            // Axe X (rouge)
            GL.Color3(1.0f, 0.0f, 0.0f);
            GL.Vertex3(Origin);
            GL.Vertex3(Origin + XDirection);

            // Axe Z (bleu)
            GL.Color3(0.0f, 0.0f, 1.0f);
            GL.Vertex3(Origin);
            GL.Vertex3(Origin + ZDirection);

            GL.End();
        }
    }
}
