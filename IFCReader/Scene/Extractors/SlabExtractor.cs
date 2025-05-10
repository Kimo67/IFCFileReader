// src/Scene/Extractors/SlabExtractor.cs
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IFCReader.Models;
using IFCReader.Utils;
using OpenTK.Mathematics;

namespace IFCReader.Scene
{
    public class SlabExtractor : IProductExtractor
    {
        private readonly Vector3 _color;
        private int _count;

        public SlabExtractor(Vector3 color) => _color = color;

        public void Extract(Dictionary<string, Dictionary<string,string>> db,
                            List<IFCFigure> figs)
        {
            if (!db.TryGetValue("IFCSLAB", out var slabs)) return;

            foreach (var slab in slabs.Values)
            {
                var ids = IfcGeom.IdMatches(slab).ToArray();
                if (ids.Length < 3) continue;

                string plcId = ids[1];
                string pdsId = ids[2];

                if (!IfcGeom.AbsoluteOrigin(plcId, db, out var origin)) continue;
                if (!db["IFCPRODUCTDEFINITIONSHAPE"].TryGetValue(pdsId, out var pds)) continue;

                var solidId = IfcGeom.FindExtrudedSolid(pds, db);
                if (solidId == null || !db["IFCEXTRUDEDAREASOLID"].TryGetValue(solidId, out var solid)) continue;

                var tok = solid.Trim('(', ')').Split(',').Select(t => t.Trim()).ToArray();
                if (tok.Length < 4) continue;

                string profileId = tok[0].TrimStart('#');
                if (!float.TryParse(tok[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float depth))
                    continue;

                // extrusion vers +Z ou -Z
                Vector3 extrusion = IfcGeom.DirectionVector(tok[2], db) * depth;

                var verts = IfcGeom.ProfileVertices(profileId, db, origin, out _);
                if (verts == null || verts.Count < 3) continue;

                figs.Add(new IFCWall(verts, extrusion, _color)); // même classe que les murs
                _count++;
            }
        }

        public string SummaryLine => $"Slabs  → {_count}";
    }
}