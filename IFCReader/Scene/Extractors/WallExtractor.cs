using System;
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
        private int _nRect, _nArb, _nAxisFallback;

        public WallExtractor(Vector3 rectColor, Vector3 arbColor)
        {
            _rectColor = rectColor;
            _arbColor  = arbColor;
        }

        public void Extract(Dictionary<string, Dictionary<string,string>> db,
                             List<IFCFigure> figs)
        {
            _nRect = _nArb = _nAxisFallback = 0;

            string key = db.ContainsKey("IFCWALLSTANDARDCASE") ? "IFCWALLSTANDARDCASE"
                       : db.ContainsKey("IFCWALL")             ? "IFCWALL" : null;
            if (key == null) return;

            foreach (var wall in db[key].Values)
            {
                var ids = IfcGeom.IdMatches(wall).ToArray();             
                if (ids.Length < 3) continue;

                string plcId = ids[1];
                string pdsId = ids[2];

                if (!IfcGeom.WorldTransform(plcId, db, out var mWorld)) continue;

                if (!db.TryGetValue("IFCPRODUCTDEFINITIONSHAPE", out var pdsDict) ||
                    !pdsDict.TryGetValue(pdsId, out var pds)) continue;

                string? solidId = IfcGeom.FindExtrudedSolid(pds, db);
                if (solidId == null)
                {
                    foreach (var repId in IfcGeom.IdMatches(pds))
                    {
                        if (db.TryGetValue("IFCSHAPEREPRESENTATION", out var repDict) &&
                            repDict.TryGetValue(repId, out var repParams))
                        {
                            solidId = IfcGeom.IdMatches(repParams)
                                           .FirstOrDefault(id => db["IFCEXTRUDEDAREASOLID"].ContainsKey(id));
                            if (solidId != null) break;
                        }
                    }
                }
                if (solidId == null) continue;

                var solid = db["IFCEXTRUDEDAREASOLID"][solidId]
                            .Trim('(', ')').Split(',').Select(t => t.Trim()).ToArray();
                if (solid.Length < 3) continue;

                string profileId = solid[0].TrimStart('#');
                Vector3 dirLocal = IfcGeom.DirectionVector(solid[^2], db); // avant-dernier
                if (!float.TryParse(solid[^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float depth))
                    continue;

                Vector3 extrusion = Vector3.TransformNormal(dirLocal * depth, mWorld);
                Vector3 originW   = Vector3.TransformPosition(Vector3.Zero, mWorld);

                var vertsLocal = IfcGeom.ProfileVertices(profileId, db, Vector3.Zero, out bool isRect);

                if (vertsLocal != null)
                {
                    var vertsWorld = vertsLocal.Select(v => Vector3.TransformPosition(v, mWorld)).ToList();
                    figs.Add(new IFCWall(vertsWorld, extrusion, isRect ? _rectColor : _arbColor));
                    if (isRect) _nRect++; else _nArb++;
                }
                else
                {
                    // fallback : dessine juste l’axe
                    figs.Add(new IFCSegment(originW, originW + extrusion));
                    _nAxisFallback++;
                }
            }
        }

        public string SummaryLine =>
            $"Walls → Rect:{_nRect}  Arbitrary:{_nArb}  AxisFallback:{_nAxisFallback}";
    }
}
