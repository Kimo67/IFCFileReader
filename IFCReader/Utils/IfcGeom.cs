using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Mathematics;

namespace IFCReader.Utils
{
    public static class IfcGeom
    {
        private const string IdRx = @"#(\d+)";
        private static readonly Regex IdRegex = new(IdRx);

        public static IEnumerable<string> IdMatches(string s) =>
            IdRegex.Matches(s).Select(m => m.Groups[1].Value);

        /*--------------------------------------------------------------*/
        /*  MATRICE MONDE  (translation + rotation cumulées)            */
        /*--------------------------------------------------------------*/
        public static bool WorldTransform(string placementId,
                                          Dictionary<string, Dictionary<string,string>> db,
                                          out Matrix4 transform)
        {
            transform = Matrix4.Identity;
            string plcId = placementId;
            var visited  = new HashSet<string>();

            while (plcId != "$" && visited.Add(plcId))
            {
                if (!db["IFCLOCALPLACEMENT"].TryGetValue(plcId, out var plc)) return false;
                var parts = plc.Split(',').Select(t => t.Trim()).ToArray();
                if (parts.Length < 2) return false;

                string relTo   = parts[0];
                string axisId  = parts[1].TrimStart('#');

                if (!db["IFCAXIS2PLACEMENT3D"].TryGetValue(axisId, out var ax)) return false;
                var toks = ax.Split(',').Select(t => t.Trim()).ToArray();
                if (toks.Length < 1) return false;

                /*--- location --------------------------------------------*/
                string locId = IdRegex.Match(toks[0]).Groups[1].Value;
                var loc = ParseVector(db["IFCCARTESIANPOINT"][locId]);

                /*--- orientation (Axis = Z, RefDirection = X) ------------*/
                Vector3 z = toks.Length > 1 && toks[1] != "$"
                          ? ParseVector(db["IFCDIRECTION"][toks[1].TrimStart('#')]).Normalized()
                          : Vector3.UnitZ;
                Vector3 x = toks.Length > 2 && toks[2] != "$"
                          ? ParseVector(db["IFCDIRECTION"][toks[2].TrimStart('#')]).Normalized()
                          : Vector3.UnitX;
                Vector3 y = Vector3.Cross(z, x).Normalized();
                x = Vector3.Cross(y, z);        // assure orthogonalité

                /*--- compose matrice rotation+translation ----------------*/
                var rot4 = new Matrix4(
                    x.X, x.Y, x.Z, 0,
                    y.X, y.Y, y.Z, 0,
                    z.X, z.Y, z.Z, 0,
                    0,   0,   0,   1);

                Matrix4 m = rot4 * Matrix4.CreateTranslation(loc);
                transform = m * transform;      // parent à gauche

                plcId = relTo.TrimStart('#');
            }
            return true;
        }

        /*  Origine absolue seule (sans rotation) — colonne ne fonctionne qu'avec */
        public static bool AbsoluteOrigin(string placementId,
                                          Dictionary<string, Dictionary<string,string>> db,
                                          out Vector3 origin)
        {
            if (!WorldTransform(placementId, db, out Matrix4 w))
            {
                origin = Vector3.Zero; return false;
            }
            origin = new Vector3(w.M41, w.M42, w.M43);
            return true;
        }

        /*--------------------------------------------------------------*/
        /*  PROFIL 2D (rect ou polyline)  –  repère LOCAL               */
        /*--------------------------------------------------------------*/
        public static List<Vector3>? ProfileVertices(string pid,
                                                     Dictionary<string, Dictionary<string,string>> db,
                                                     Vector3 originLocal,
                                                     out bool isRect)
        {
            isRect = false;

            if (db.TryGetValue("IFCRECTANGLEPROFILEDEF", out var rDict) &&
                rDict.TryGetValue(pid, out var rp))
            {
                isRect = true;
                var p = rp.Split(',').Select(s => s.Trim()).ToArray();
                float x = float.Parse(p[^2], CultureInfo.InvariantCulture);
                float y = float.Parse(p[^1], CultureInfo.InvariantCulture);
                float hx = x / 2f, hy = y / 2f;

                return new()
                {
                    originLocal + new Vector3(-hx, -hy, 0),
                    originLocal + new Vector3( hx, -hy, 0),
                    originLocal + new Vector3( hx,  hy, 0),
                    originLocal + new Vector3(-hx,  hy, 0)
                };
            }

            if (db.TryGetValue("IFCARBITRARYCLOSEDPROFILEDEF", out var aDict) &&
                aDict.TryGetValue(pid, out var ap))
            {
                string curveId = IdMatches(ap).Last();
                if (db.TryGetValue("IFCPOLYLINE", out var plDict) &&
                    plDict.TryGetValue(curveId, out var pl))
                {
                    var pts = IdMatches(pl)
                              .Select(id => originLocal + ParseVector(db["IFCCARTESIANPOINT"][id]));
                    return pts.ToList();
                }
            }
            return null;
        }

        /*------------------ aides diverses ----------------------------*/
        public static string? FindExtrudedSolid(string pds,
                                                Dictionary<string, Dictionary<string,string>> db)
        {
            foreach (var repId in IdMatches(pds))
            {
                if (!db.TryGetValue("IFCSHAPEREPRESENTATION", out var repDict) ||
                    !repDict.TryGetValue(repId, out var rep)) continue;

                if (!rep.Contains("SweptSolid", StringComparison.OrdinalIgnoreCase)) continue;

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
                !db.TryGetValue("IFCDIRECTION", out var d) ||
                !d.TryGetValue(token.TrimStart('#'), out var v)) return Vector3.UnitZ;
            return ParseVector(v).Normalized();
        }

        public static Vector3 ParseVector(string str)
        {
            var v = str.Trim('(', ')').Split(',')
                       .Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
            return new Vector3(v[0], v[1], v.Length > 2 ? v[2] : 0);
        }
    }
}