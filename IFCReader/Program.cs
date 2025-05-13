using System;
using System.Collections.Generic;
using System.IO;
using IFCFileReader.Services;   // IFCFileParser
using IFCReader.Scene;          // SceneBuilder, Scene
using OpenTK.Mathematics;       // Vector3


class Program
{
    // Valeur par défaut si aucun argument n'est passé
    private const string DefaultIfcFile = "data/Projet.ifc";

    static void Main(string[] args)
    {
        // Choix du fichier : argument CLI ou valeur par défaut
        var ifcFile = args.Length > 0 ? args[0] : DefaultIfcFile;

        if (!File.Exists(ifcFile))
        {
            Console.WriteLine($"⛔ Fichier introuvable : {ifcFile}");
            return;
        }

        // 1. Parse IFC (dictionnaire ID → params)
        var db = new IFCFileParser().Parse(ifcFile);

        // 2. Construit la scène (murs, colonnes, dalles)
        var scene = new SceneBuilder()
                .AddExtractor(new WallExtractor(
                    rectColor : new Vector3(1,1,0),
                    arbColor  : new Vector3(0,1,1)))
                .AddExtractor(new ColumnExtractor(
                    rectColor : new Vector3(1,1,0),
                    circColor : new Vector3(1,0,1)))
                .AddExtractor(new SlabExtractor(new Vector3(0.8f,0.5f,0.2f)))
                .Build(db);

        // Rien de récupérer -> on avertit et on quitte
        if (scene.Figures.Count == 0)
        {
            Console.WriteLine("Aucun élément à afficher.");
            return;
        }

        scene.LogSummary();

        // 3. Lancement du viewer OpenGL
        var viewer = new Viewer3D();
        viewer.AddFigures(scene.Figures);
        viewer.Run();
    }
}
