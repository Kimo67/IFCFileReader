using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IFCReader.Models;
using IFCReader.Utils;
using OpenTK.Mathematics;

namespace IFCReader.Scene
{
    public class WallExtractor : IProductExtractor
    {
        private readonly Vector3 _rectColor;
        private readonly Vector3 _arbColor;
        private int _nRect, _nArb;

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
                var ids = IfcGeom.IdMatches(wall).ToArray();
                if (ids.Length < 3) continue;

                string plcId = ids[1];
                string pdsId = ids[2];

                
                if (!IfcGeom.WorldTransform(plcId, db, out var world)) continue;
                if (!db["IFCPRODUCTDEFINITIONSHAPE"].TryGetValue(pdsId, out var pds)) continue;

                var solidId = IfcGeom.FindExtrudedSolid(pds, db);
                if (solidId == null || !db["IFCEXTRUDEDAREASOLID"].TryGetValue(solidId, out var solid)) continue;

                var tok = solid.Trim('(', ')').Split(',').Select(s => s.Trim()).ToArray();
                if (tok.Length < 4) continue;

                string profileId = tok[0].TrimStart('#');
                Vector3 dirLocal = IfcGeom.DirectionVector(tok[2], db);
                if (!float.TryParse(tok[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float depth)) continue;

                Vector3 dirWorld = Vector3.TransformNormal(dirLocal, world).Normalized();
                var extrusion = dirWorld * depth;

                var vertsLocal = IfcGeom.ProfileVertices(profileId, db, Vector3.Zero, out bool isRect);
                if (vertsLocal == null) continue;

                var vertsWorld = vertsLocal.Select(v => Vector3.TransformPosition(v, world))
                                           .ToList();

                figs.Add(new IFCWall(vertsWorld, extrusion, isRect ? _rectColor : _arbColor));
                if (isRect) _nRect++; else _nArb++;
            }
        }

        public string SummaryLine => $"Walls → Rect:{_nRect}  Arbitrary:{_nArb}";
    }
}