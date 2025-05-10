using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using IFCReader.Models;       // IFCWall
using OpenTK.Mathematics;
using IFCReader.Utils;        // IfcGeom

namespace IFCReader.Scene
{
    //Extrait tous les IFCWallStandardCase.
    public class WallExtractor : IProductExtractor
    {
        private readonly Vector3 _rectColor;
        private readonly Vector3 _arbColor;
        private int _nRect;
        private int _nArb;

        public WallExtractor(Vector3 rectColor, Vector3 arbColor)
        {
            _rectColor = rectColor;
            _arbColor  = arbColor;
        }

        public void Extract(Dictionary<string, Dictionary<string,string>> db,
                             List<IFCFigure> figs)
        {
            if (!db.TryGetValue("IFCWALLSTANDARDCASE", out var walls)) return;

            foreach (var wall in walls.Values)
            {
                // ids : #entité, #OwnerHistory, #Placement, #Representation, ...
                var ids = IfcGeom.IdMatches(wall).ToArray();
                if (ids.Length < 3) continue;

                string placementId = ids[1];
                string pdsId       = ids[2];

                if (!IfcGeom.AbsoluteOrigin(placementId, db, out var origin)) continue;
                if (!db["IFCPRODUCTDEFINITIONSHAPE"].TryGetValue(pdsId, out var pds)) continue;

                var solidId = IfcGeom.FindExtrudedSolid(pds, db);
                if (solidId == null || !db["IFCEXTRUDEDAREASOLID"].TryGetValue(solidId, out var solid)) continue;

                var parts = solid.Trim('(', ')').Split(',').Select(s => s.Trim()).ToArray();
                if (parts.Length < 4) continue;

                string profileId = parts[0].TrimStart('#');
                string dirToken  = parts[2];
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float depth))
                    continue;

                Vector3 dir = IfcGeom.DirectionVector(dirToken, db);
                var extrusion = dir * depth;

                var verts = IfcGeom.ProfileVertices(profileId, db, origin, out bool isRect);
                if (verts == null || verts.Count < 3) continue;

                figs.Add(new IFCWall(verts, extrusion, isRect ? _rectColor : _arbColor));
                if (isRect) _nRect++; else _nArb++;
            }
        }

        public string SummaryLine =>
            $"Walls → Rect:{_nRect}  Arbitrary:{_nArb}";
    }
}