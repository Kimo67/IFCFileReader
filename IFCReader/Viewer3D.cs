using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using IFCReader.Models;

public class Viewer3D
{
    private readonly List<IFCFigure> figures = new();
    private int shaderProgram;

    /*----------------- paramètres caméra -----------------*/
    private float   yaw   = -45f;  // degrés
    private float   pitch = -30f;  // degrés
    private float   radius = 6f;   // distance au centre
    private Vector3 target  = Vector3.Zero;

    private const float MouseSensitivity = 0.3f;  // °/pixel
    private const float ZoomSpeed        = 10f;   // unités/s

    /*--------------------  API publique ------------------*/
    public void AddFigure(IFCFigure f)             => figures.Add(f);
    public void AddFigures(IEnumerable<IFCFigure> f) => figures.AddRange(f);

    /*--------------------  Run fenêtre -------------------*/
    public void Run()
    {
        var settings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(800, 600),
            Title      = "IFC Viewer 3D",
            APIVersion = new Version(3, 3),
            Profile    = ContextProfile.Core
        };

        using var win = new GameWindow(GameWindowSettings.Default, settings);

        win.Load += () =>
        {
            shaderProgram = CreateShaderProgram();
            GL.ClearColor(0f, 0f, 0f, 1f);
            GL.Enable(EnableCap.DepthTest);

            foreach (var fig in figures)
            {
                switch (fig)
                {
                    case IFCAxis    ax:   ax.InitializeBuffers();   break;
                    case IFCSegment sg:   sg.InitializeBuffers();   break;
                    case IFCWall    wl:   wl.InitializeBuffers();   break;
                }
            }
        };

        win.UpdateFrame += args =>
        {
            var kb = win.KeyboardState;
            float dt = (float)args.Time;

            /*--------- zoom via flèches ou molette ---------*/
            if (kb.IsKeyDown(Keys.Up))   radius -= ZoomSpeed * dt;
            if (kb.IsKeyDown(Keys.Down)) radius += ZoomSpeed * dt;
            radius = Math.Clamp(radius, 1f, 100f);

            /*--------- rotation via souris -----------------*/
            var ms    = win.MouseState;
            var delta = ms.Delta;                 // pixels depuis la frame précédente
            if (ms.IsButtonDown(MouseButton.Left))
            {
                yaw   -= delta.X * MouseSensitivity;
                pitch -= delta.Y * MouseSensitivity;
                pitch = Math.Clamp(pitch, -89f, 89f);  // évite le retournement
            }
        };

        win.RenderFrame += args =>
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(shaderProgram);

            Vector3 camPos = SphericalToCartesian(radius, yaw, pitch) + target;

            Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                win.ClientSize.X / (float)win.ClientSize.Y,
                0.1f, 500f);
            Matrix4 view  = Matrix4.LookAt(camPos, target, Vector3.UnitY);
            Matrix4 model = Matrix4.Identity;

            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uProjection"), false, ref proj);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uView"),       false, ref view);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "uModel"),      false, ref model);

            foreach (var f in figures) f.Render();
            win.SwapBuffers();
        };

        win.Resize += e => GL.Viewport(0, 0, e.Width, e.Height);
        win.Run();
    }

    /*===================   utilitaires   ===================*/
    private static Vector3 SphericalToCartesian(float r, float yawDeg, float pitchDeg)
    {
        float yawRad   = MathHelper.DegreesToRadians(yawDeg);
        float pitchRad = MathHelper.DegreesToRadians(pitchDeg);

        float x = r * MathF.Cos(pitchRad) * MathF.Cos(yawRad);
        float y = r * MathF.Sin(pitchRad);
        float z = r * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
        return new Vector3(x, y, z);
    }

    private int CreateShaderProgram()
    {
        const string vs = @"#version 330 core
            layout(location=0) in vec3 aPos;
            layout(location=1) in vec3 aColor;
            uniform mat4 uProjection,uView,uModel;
            out vec3 vColor;
            void main(){gl_Position=uProjection*uView*uModel*vec4(aPos,1);vColor=aColor;}";
        const string fs = @"#version 330 core
            in vec3 vColor;out vec4 fragColor;
            void main(){fragColor=vec4(vColor,1);}";

        int vert = CompileShader(ShaderType.VertexShader,   vs);
        int frag = CompileShader(ShaderType.FragmentShader, fs);

        int prog = GL.CreateProgram();
        GL.AttachShader(prog, vert); GL.AttachShader(prog, frag);
        GL.LinkProgram(prog);
        GL.DeleteShader(vert); GL.DeleteShader(frag);
        return prog;
    }

    private static int CompileShader(ShaderType type, string src)
    {
        int id = GL.CreateShader(type);
        GL.ShaderSource(id, src);
        GL.CompileShader(id);
        GL.GetShader(id, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0) throw new Exception(GL.GetShaderInfoLog(id));
        return id;
    }
}
