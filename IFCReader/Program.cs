using System;
using System.Linq;
using IFCReader.Models;
using IFCFileReader.Services;

class Program
{
    static void Main(string[] args)
    {
        string filePath = "data/Projet.ifc";

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Le fichier '{filePath}' est introuvable.");
            return;
        }

        // Charger et parser le fichier IFC
        var parser = new IFCFileParser();
        var parsedData = parser.Parse(filePath);

        // Récupérer les données pour l'axe #2 pour le test
        var axisData = parsedData["IFCAXIS2PLACEMENT3D"].FirstOrDefault(x => x.StartsWith("#2"));
        if (axisData == null)
        {
            Console.WriteLine("L'axe #2 est introuvable.");
            return;
        }

        // Extraire les ID
        var axisComponents = axisData.Split(new[] { "(", ")", "#" }, StringSplitOptions.RemoveEmptyEntries);
        string originId = axisComponents[1].Trim(',');
        string zDirectionId = axisComponents[2].Trim(',');
        string xDirectionId = axisComponents[3].Trim(',');
        
        Console.WriteLine($"Origin ID : {originId}");
        Console.WriteLine($"Z Direction ID : {zDirectionId}");
        Console.WriteLine($"X Direction ID : {xDirectionId}");

        // Récupérer les données associées
        var originData = parsedData["IFCCARTESIANPOINT"].FirstOrDefault(x => x.StartsWith($"#{originId}"));
        var zDirectionData = parsedData["IFCDIRECTION"].FirstOrDefault(x => x.StartsWith($"#{zDirectionId}"));
        var xDirectionData = parsedData["IFCDIRECTION"].FirstOrDefault(x => x.StartsWith($"#{xDirectionId}"));

        Console.WriteLine($"Données de l'origine : {originData}");
        Console.WriteLine($"Données de la direction Z : {zDirectionData}");
        Console.WriteLine($"Données de la direction X : {xDirectionData}");


        if (originData == null || zDirectionData == null || xDirectionData == null)
        {
            Console.WriteLine("Impossible de récupérer les données associées à l'axe #2.");
            return;
        }

        // Créer une instance d'IFCAxis
        var axis = new IFCAxis(originData, xDirectionData, zDirectionData);

        // Lancer le viewer OpenTK
        Viewer3D viewer = new Viewer3D();
        viewer.AddFigure(axis);
        viewer.Run();
    }
}
