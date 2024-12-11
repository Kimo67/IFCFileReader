using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System.Collections.Generic;
using IFCReader.Models;

public class Viewer3D
{
    private readonly List<IFCFigure> figures = new List<IFCFigure>();

    public void AddFigure(IFCFigure figure)
    {
        figures.Add(figure);
    }

    public void Run()
    {
        var settings = new NativeWindowSettings()
        {
            Size = new Vector2i(800, 600),
            Title = "IFC Viewer 3D"
        };

        using (var window = new GameWindow(GameWindowSettings.Default, settings))
        {
            window.RenderFrame += (args) =>
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                foreach (var figure in figures)
                {
                    figure.Render();
                }

                window.SwapBuffers();
            };

            window.Run();
        }
    }
}
