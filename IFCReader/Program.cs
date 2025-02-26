using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
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

        // Vérifier que le fichier contient des axes
        if (!parsedData.ContainsKey("IFCAXIS2PLACEMENT3D"))
        {
            Console.WriteLine("Aucun axe (IFCAXIS2PLACEMENT3D) n'a été trouvé dans le fichier.");
            return;
        }

        var axesData = parsedData["IFCAXIS2PLACEMENT3D"];
        List<IFCAxis> axes = new List<IFCAxis>();

        // Parcourir tous les axes
        foreach (var axisData in axesData)
        {
            // Exemple de chaîne attendue : "#2 ((#3),(#4),(#5))"
            // On découpe la chaîne pour extraire les identifiants
            var components = axisData.Split(new[] { "(", ")", "#" }, StringSplitOptions.RemoveEmptyEntries);
            // On attend au moins 4 éléments : [id de l'axe, id origine, id direction Z, id direction X]
            if (components.Length < 4)
            {
                Console.WriteLine("Format inattendu pour l'axe: " + axisData);
                continue;
            }
            string originId = components[1].Trim(',');
            string zDirectionId = components[2].Trim(',');
            string xDirectionId = components[3].Trim(',');

            // Récupérer les données associées
            var originData = parsedData["IFCCARTESIANPOINT"].FirstOrDefault(x => x.StartsWith($"#{originId}"));
            var zDirectionData = parsedData["IFCDIRECTION"].FirstOrDefault(x => x.StartsWith($"#{zDirectionId}"));
            var xDirectionData = parsedData["IFCDIRECTION"].FirstOrDefault(x => x.StartsWith($"#{xDirectionId}"));

            if (originData == null || zDirectionData == null || xDirectionData == null)
            {
                Console.WriteLine($"Données manquantes pour l'axe avec origine #{originId}");
                continue;
            }

            // Créer l'axe et l'ajouter à la liste
            var axis = new IFCAxis(originData, xDirectionData, zDirectionData);
            axes.Add(axis);
            Console.WriteLine($"Axe ajouté : Origine #{originId}, Z #{zDirectionId}, X #{xDirectionId}");
        }

        if (axes.Count == 0)
        {
            Console.WriteLine("Aucun axe valide n'a été trouvé.");
            return;
        }

        // Lancer le viewer OpenTK et ajouter tous les axes
        Viewer3D viewer = new Viewer3D();
        foreach (var axis in axes)
        {
            viewer.AddFigure(axis);
        }
        viewer.Run();
    }
}
