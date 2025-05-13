# IFCReader – Visualiseur 3D léger pour fichiers IFC

Ce projet permet d’afficher la géométrie de base
(Murs, Dalles, Poteaux, Axes) d’un fichier **IFC**.
Il fonctionne avec :

* Profils simples (rectangles, polylignes)  
* Profils complexes décrits par **`IFCCOMPOSITECURVE`**  

---

## Pré-requis

| Outil / lib | Version minimale |
|-------------|------------------|
| .NET SDK    | **8.0**          |
| OpenTK      | 4.x (référence NuGet déjà dans le projet) |
| GPU         | Compatible OpenGL 3.3 |

---

## Compiler 

```bash
dotnet build      # restauration + compilation
```

## Lancer le viewer

```bash
# 1) modèle par défaut (data/Projet.ifc)
dotnet run

# 2) n’importe quel IFC
dotnet run <chemin-vers-fichier.ifc>
```

## Contrôles dans la fenêtre

| Action   | Contrôle                     |
| -------- | ---------------------------- |
| Rotation | Clic gauche + glisser souris |
| Zoom     | (Flèche ↑ / Flèche ↓)        |

## Organisation du code

```bash
├── Program.cs           # Parse -> Scene -> Viewer
├── Services/            # FileService, IFCFileParser
├── Utils/               # IfcGeom (placements, profils…)
├── Models/              # IFCWall, IFCColumn, IFCSegment…
└── Scene/Extractor/     # WallExtractor, ColumnExtractor, SlabExtractor

```
- Les extracteurs transforment les entités IFC en IFCFigure.
- Le Viewer3D initialise OpenGL et dessine les figures.

