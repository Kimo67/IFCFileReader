using System;
using System.Collections.Generic;
using System.IO;
using IFCFileReader.Services;          // IFCFileParser
using IFCReader.Scene;                 // SceneBuilder, Scene
using OpenTK.Mathematics;              // Vector3

class Program
{
    private const string IFC_FILE = "data/Projet.ifc";

    static void Main()
    {
        if (!File.Exists(IFC_FILE))
        {
            Console.WriteLine($"⛔ Fichier introuvable : {IFC_FILE}");
            return;
        }

        // 1. Parse IFC (dictionnaire ID → params)
        var db = new IFCFileParser().Parse(IFC_FILE);

        // 2. Construit la scène (pour l'instant : seulement les murs)
        var scene = new SceneBuilder()
                .AddExtractor(new WallExtractor(
                    rectColor : new Vector3(1,1,0),
                    arbColor  : new Vector3(0,1,1)))
                .AddExtractor(new ColumnExtractor(
                    rectColor : new Vector3(1,1,0),
                    circColor : new Vector3(1,0,1)))
                .AddExtractor(new SlabExtractor(new Vector3(0.8f,0.5f,0.2f)))
                .Build(db);


        if (scene.Figures.Count == 0)
        {
            Console.WriteLine("Aucun élément à afficher.");
            return;
        }

        scene.LogSummary();

        // 3. Affichage OpenGL
        var viewer = new Viewer3D();
        viewer.AddFigures(scene.Figures);
        viewer.Run();
    }
}