using System;
using System.IO;

class IFCReader
{
    static void Main(string[] args)
    {
        // Chemin du fichier dans le dossier "data"
        string filePath = Path.Combine("data", "Projet.ifc");

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"Le fichier '{filePath}' est introuvable.");
            return;
        }

        try
        {
            // Lire la première ligne du fichier
            using (StreamReader reader = new StreamReader(filePath))
            {
                string? firstLine = reader.ReadLine();

                if (firstLine == null)
                {
                    Console.WriteLine("Le fichier est vide ou n'a pas pu être lu correctement.");
                    return;
                }
                
                Console.WriteLine("Première ligne du fichier IFC :");
                Console.WriteLine(firstLine);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la lecture du fichier : {ex.Message}");
        }
    }
}
