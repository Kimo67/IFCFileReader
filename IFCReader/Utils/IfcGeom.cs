using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Mathematics;

namespace IFCReader.Utils
{
    /// Fonctions utilitaires réutilisables pour lire la géométrie IFC.
    public static class IfcGeom
    {
        private const string IdRx = @"#(\d+)";
        private static readonly Regex IdRegex = new(IdRx);

        public static IEnumerable<string> IdMatches(string s) =>
            IdRegex.Matches(s).Select(m => m.Groups[1].Value);

        public static bool TryGetOrigin(string placementId,
                                        Dictionary<string, Dictionary<string,string>> db,
                                        out Vector3 origin)
        {
            origin = Vector3.Zero;
            if (!db["IFCLOCALPLACEMENT"].TryGetValue(placementId, out var plc)) return false;
            string axisId = plc.Split(',')[1].Trim().TrimStart('#');

            if (!db["IFCAXIS2PLACEMENT3D"].TryGetValue(axisId, out var axis)) return false;
            string cartId = IdRegex.Match(axis).Groups[1].Value;

            var p = db["IFCCARTESIANPOINT"][cartId]
                     .Trim('(', ')').Split(',')
                     .Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            origin = new Vector3(p[0], p[1], p.Length > 2 ? p[2] : 0);
            return true;
        }

        public static string? FindExtrudedSolid(string pds,
                                                Dictionary<string, Dictionary<string,string>> db)
        {
            foreach (var repId in IdMatches(pds))
            {
                if (!db.TryGetValue("IFCSHAPEREPRESENTATION", out var repDict) ||
                    !repDict.TryGetValue(repId, out var rep)) continue;

                if (!rep.Contains("SweptSolid", System.StringComparison.OrdinalIgnoreCase)) continue;

                var last = IdMatches(rep).LastOrDefault();
                if (last != null && db["IFCEXTRUDEDAREASOLID"].ContainsKey(last))
                    return last;
            }
            return null;
        }

        public static Vector3 DirectionVector(string token,
                                              Dictionary<string, Dictionary<string,string>> db)
        {
            if (token == "$" ||
                !db.TryGetValue("IFCDIRECTION", out var dirDict) ||
                !dirDict.TryGetValue(token.TrimStart('#'), out var dirParams))
                return Vector3.UnitZ;

            var v = dirParams.Trim('(', ')').Split(',')
                     .Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(v[0], v[1], v.Length > 2 ? v[2] : 0).Normalized();
        }

        public static List<Vector3>? ProfileVertices(string profileId,
                                                     Dictionary<string, Dictionary<string,string>> db,
                                                     Vector3 origin,
                                                     out bool isRect)
        {
            isRect = false;

            // Rectangle
            if (db.TryGetValue("IFCRECTANGLEPROFILEDEF", out var rDict) &&
                rDict.TryGetValue(profileId, out var r))
            {
                isRect = true;
                var p = r.Split(',').Select(s => s.Trim()).ToArray();
                float x = float.Parse(p[^2], CultureInfo.InvariantCulture);
                float y = float.Parse(p[^1], CultureInfo.InvariantCulture);
                float hx = x / 2f, hy = y / 2f;
                return new()
                {
                    new(origin.X - hx, origin.Y - hy, origin.Z),
                    new(origin.X + hx, origin.Y - hy, origin.Z),
                    new(origin.X + hx, origin.Y + hy, origin.Z),
                    new(origin.X - hx, origin.Y + hy, origin.Z)
                };
            }

            // ArbitraryClosed (polyline uniquement)
            if (db.TryGetValue("IFCARBITRARYCLOSEDPROFILEDEF", out var aDict) &&
                aDict.TryGetValue(profileId, out var a))
            {
                string curveId = IdMatches(a).Last();
                if (db.TryGetValue("IFCPOLYLINE", out var plDict) &&
                    plDict.TryGetValue(curveId, out var pl))
                {
                    var pts = IdMatches(pl).Select(id =>
                               origin + ParseVector(db["IFCCARTESIANPOINT"][id]));
                    return pts.ToList();
                }
            }
            return null;
        }

        private static Vector3 ParseVector(string s)
        {
            var v = s.Trim('(', ')').Split(',')
                     .Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(v[0], v[1], v.Length > 2 ? v[2] : 0);
        }
    }
}