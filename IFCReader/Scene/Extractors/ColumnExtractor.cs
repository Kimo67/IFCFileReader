using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using IFCReader.Models;       // IFCColumn
using OpenTK.Mathematics;
using IFCReader.Utils;        // IfcGeom

namespace IFCReader.Scene
{
    /// Extrait les IFCCOLUMN (poteaux) avec profil rectangle ou cercle.
    public class ColumnExtractor : IProductExtractor
    {
        private readonly Vector3 _rectColor;
        private readonly Vector3 _circColor;
        private int _nRect, _nCirc;

        public ColumnExtractor(Vector3 rectColor, Vector3 circColor)
        {
            _rectColor = rectColor;
            _circColor = circColor;
        }

        public void Extract(Dictionary<string, Dictionary<string,string>> db,
                             List<IFCFigure> figs)
        {
            if (!db.TryGetValue("IFCCOLUMN", out var cols)) return;

            foreach (var col in cols.Values)
            {
                var ids = IfcGeom.IdMatches(col).ToArray();
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
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float depth)) continue;

                Vector3 dir = IfcGeom.DirectionVector(dirToken, db);
                var extrusion = dir * depth;

                // rectangle ou circle ?
                bool isRect;
                var verts = IfcGeom.ProfileVertices(profileId, db, origin, out isRect);
                if (isRect && verts != null)
                {
                    figs.Add(new IFCColumn(verts, extrusion, _rectColor));
                    _nRect++; continue;
                }

                // circle
                if (db.TryGetValue("IFCCIRCLEPROFILEDEF", out var circDict) &&
                    circDict.TryGetValue(profileId, out var circ))
                {
                    float radius = float.Parse(circ.Split(',').Last(), CultureInfo.InvariantCulture);
                    var circleVerts = GenerateCircle(origin, radius, 16);
                    figs.Add(new IFCColumn(circleVerts, extrusion, _circColor));
                    _nCirc++;
                }
            }
        }

        public string SummaryLine => $"Columns → Rect:{_nRect}  Circle:{_nCirc}";

        private static List<Vector3> GenerateCircle(Vector3 center, float r, int segments)
        {
            var list = new List<Vector3>(segments);
            for (int i = 0; i < segments; i++)
            {
                float a = i * MathF.Tau / segments;
                list.Add(new Vector3(center.X + r * MathF.Cos(a), center.Y + r * MathF.Sin(a), center.Z));
            }
            return list;
        }
    }
}