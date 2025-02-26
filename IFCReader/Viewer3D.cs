using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using IFCReader.Models;

public class Viewer3D
{
    private readonly List<IFCFigure> figures = new List<IFCFigure>();
    private int shaderProgram;

    // Variables de caméra pour la navigation
    private Vector3 cameraPosition = new Vector3(3f, 3f, 3f);
    private Vector3 cameraTarget = Vector3.Zero;
    private float cameraSpeed = 2.5f; // vitesse de déplacement

    public void AddFigure(IFCFigure figure)
    {
        figures.Add(figure);
    }

    public void Run()
    {
        var settings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(800, 600),
            Title = "IFC Viewer 3D",
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core
        };

        using (var window = new GameWindow(GameWindowSettings.Default, settings))
        {
            // Optionnel : positionnement de la fenêtre
            window.Location = new Vector2i(100, 100);

            window.Load += () =>
            {
                shaderProgram = CreateShaderProgram();
                GL.ClearColor(0f, 0f, 0f, 1f);
                GL.Enable(EnableCap.DepthTest);

                // Initialiser les buffers de chaque figure (ici pour IFCAxis)
                foreach (var figure in figures)
                {
                    if (figure is IFCAxis axis)
                    {
                        axis.InitializeBuffers();
                    }
                }
            };

            window.UpdateFrame += (FrameEventArgs args) =>
            {
                var input = window.KeyboardState;
                float move = cameraSpeed * (float)args.Time;
                
                // Utiliser les flèches directionnelles pour se déplacer horizontalement
                if (input.IsKeyDown(Keys.Up))
                    cameraPosition.Z -= move;
                if (input.IsKeyDown(Keys.Down))
                    cameraPosition.Z += move;
                if (input.IsKeyDown(Keys.Left))
                    cameraPosition.X -= move;
                if (input.IsKeyDown(Keys.Right))
                    cameraPosition.X += move;
                // Space pour monter et LeftShift pour descendre
                if (input.IsKeyDown(Keys.Space))
                    cameraPosition.Y += move;
                if (input.IsKeyDown(Keys.LeftShift))
                    cameraPosition.Y -= move;
            };

            window.Resize += (ResizeEventArgs e) =>
            {
                GL.Viewport(0, 0, e.Width, e.Height);
            };

            window.RenderFrame += (FrameEventArgs args) =>
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                GL.UseProgram(shaderProgram);

                // Définir les matrices de transformation
                Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(45f),
                    window.ClientSize.X / (float)window.ClientSize.Y,
                    0.1f,
                    100f
                );
                Matrix4 view = Matrix4.LookAt(cameraPosition, cameraTarget, Vector3.UnitY);
                Matrix4 model = Matrix4.Identity;

                int projLocation = GL.GetUniformLocation(shaderProgram, "uProjection");
                int viewLocation = GL.GetUniformLocation(shaderProgram, "uView");
                int modelLocation = GL.GetUniformLocation(shaderProgram, "uModel");

                GL.UniformMatrix4(projLocation, false, ref projection);
                GL.UniformMatrix4(viewLocation, false, ref view);
                GL.UniformMatrix4(modelLocation, false, ref model);

                foreach (var figure in figures)
                {
                    figure.Render();
                }

                window.SwapBuffers();
            };

            window.Run();
        }
    }

    private int CreateShaderProgram()
    {
        string vertexShaderSource = @"
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec3 aColor;
            out vec3 vColor;
            uniform mat4 uProjection;
            uniform mat4 uView;
            uniform mat4 uModel;
            void main()
            {
                gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
                vColor = aColor;
            }";

        string fragmentShaderSource = @"
            #version 330 core
            in vec3 vColor;
            out vec4 fragColor;
            void main()
            {
                fragColor = vec4(vColor, 1.0);
            }";

        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexShaderSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompilation(vertexShader);

        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentShaderSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompilation(fragmentShader);

        int program = GL.CreateProgram();
        GL.AttachShader(program, vertexShader);
        GL.AttachShader(program, fragmentShader);
        GL.LinkProgram(program);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        return program;
    }

    private void CheckShaderCompilation(int shader)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception("Erreur de compilation du shader: " + infoLog);
        }
    }
}
