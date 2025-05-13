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
        /*--------------------------------------------------------------*/
        /*  OUTILS GÉNÉRAUX                                             */
        /*--------------------------------------------------------------*/
        private const string IdRx = @"#(\d+)";
        private static readonly Regex IdRegex = new(IdRx);

        public static IEnumerable<string> IdMatches(string s) =>
            IdRegex.Matches(s).Select(m => m.Groups[1].Value);

        public static Vector3 ParseVector(string str)
        {
            var v = str.Trim('(', ')').Split(',')
                       .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                       .ToArray();
            return new Vector3(v[0], v[1], v.Length > 2 ? v[2] : 0);
        }

        public static Vector3 DirectionVector(string token,
                                              Dictionary<string, Dictionary<string,string>> db)
        {
            if (token == "$" ||
                !db.TryGetValue("IFCDIRECTION", out var d) ||
                !d.TryGetValue(token.TrimStart('#'), out var v))
                return Vector3.UnitZ;

            return ParseVector(v).Normalized();
        }

        /*--------------------------------------------------------------*/
        /*  MATRICE PLACEMENT -> MONDE                                   */
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

                /*--- localisation ---------------------------------------*/
                string locId = IdRegex.Match(toks[0]).Groups[1].Value;
                var loc = ParseVector(db["IFCCARTESIANPOINT"][locId]);

                /*--- orientation ----------------------------------------*/
                Vector3 z = toks.Length > 1 && toks[1] != "$"
                          ? ParseVector(db["IFCDIRECTION"][toks[1].TrimStart('#')]).Normalized()
                          : Vector3.UnitZ;
                Vector3 x = toks.Length > 2 && toks[2] != "$"
                          ? ParseVector(db["IFCDIRECTION"][toks[2].TrimStart('#')]).Normalized()
                          : Vector3.UnitX;
                Vector3 y = Vector3.Cross(z, x).Normalized();
                x = Vector3.Cross(y, z);        // orthogonalise

                var rot4 = new Matrix4(
                    x.X, x.Y, x.Z, 0,
                    y.X, y.Y, y.Z, 0,
                    z.X, z.Y, z.Z, 0,
                    0,   0,   0,   1);

                var m = rot4 * Matrix4.CreateTranslation(loc);
                transform = m * transform;

                plcId = relTo.TrimStart('#');
            }
            return true;
        }

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
        /*  PROFIL 2D (RECT, POLYLINE, COMPOSITE)                       */
        /*--------------------------------------------------------------*/
        public static List<Vector3>? ProfileVertices(string pid,
                                                     Dictionary<string, Dictionary<string,string>> db,
                                                     Vector3 originLocal,
                                                     out bool isRect)
        {
            isRect = false;

            /*--- IFCRECTANGLEPROFILEDEF --------------------------------*/
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

            /*--- IFCARBITRARYCLOSEDPROFILEDEF -> IFCPOLYLINE -----------*/
            if (db.TryGetValue("IFCARBITRARYCLOSEDPROFILEDEF", out var aDict) &&
                aDict.TryGetValue(pid, out var ap))
            {
                string curveId = IdMatches(ap).Last();

                // 1) Polyline
                if (db.TryGetValue("IFCPOLYLINE", out var plDict) &&
                    plDict.TryGetValue(curveId, out var pl))
                {
                    var pts = IdMatches(pl)
                            .Select(id => originLocal + ParseVector(db["IFCCARTESIANPOINT"][id]));
                    return pts.ToList();
                }

                // 2) CompositeCurve (nouveau)
                if (db.TryGetValue("IFCCOMPOSITECURVE", out var _)
                    && db["IFCCOMPOSITECURVE"].ContainsKey(curveId))
                {
                    // on relance ProfileVertices avec le nouveau pid
                    return ProfileVertices(curveId, db, originLocal, out isRect);
                }


            }


            /*--- IFCCOMPOSITECURVE -------------------------------------*/
            if (db.TryGetValue("IFCCOMPOSITECURVE", out var ccDict) &&
                ccDict.TryGetValue(pid, out var ccParams))
            {
                var verts = new List<Vector3>();
                var segIds = IdMatches(ccParams);            // segments de la courbe composite

                foreach (var segId in segIds)
                {
                    var segLine = db["IFCCOMPOSITECURVESEGMENT"][segId];
                    string trimmedId = IdMatches(segLine).Last();
                    var trimmed = db["IFCTRIMMEDCURVE"][trimmedId];
                    string basisId = IdMatches(trimmed).First();   // ligne ou cercle

                    /*--- ligne ----------------------------------------*/
                    if (db["IFCLINE"].ContainsKey(basisId))
                    {
                        var ln = db["IFCLINE"][basisId];
                        var pts = IdMatches(ln).ToArray();   // p0 , dir
                        var p0  = ParseVector(db["IFCCARTESIANPOINT"][pts[0]]);
                        var dir = DirectionVector("#" + pts[1], db).Normalized();

                        // trims : deux tokens après basisId
                        string[] trims = trimmed.Split(',')
                                                .Skip(1)      // saute basisId
                                                .Take(2)
                                                .Select(t => t.Trim('(',')','$'))
                                                .ToArray();

                        float t0 = ParseTrim(trims[0], db);
                        float t1 = ParseTrim(trims[1], db);

                        verts.Add(originLocal + p0 + dir * t0);
                        verts.Add(originLocal + p0 + dir * t1);
                    }
                    /*--- cercle / arc --------------------------------*/
                    else if (db["IFCCIRCLE"].ContainsKey(basisId))
                    {
                        var circ = db["IFCCIRCLE"][basisId]
                                   .Split(',').Select(t => t.Trim()).ToArray();
                        var center = ParseVector(
                            db["IFCCARTESIANPOINT"][circ[0].TrimStart('#')]);
                        float radius = float.Parse(circ[2], CultureInfo.InvariantCulture);

                        // trims = deux angles radian
                        var toks = trimmed.Split(',')
                                          .Skip(1).Take(2)
                                          .Select(t => t.Trim('(',')'))
                                          .ToArray();
                        float ang0 = float.Parse(toks[0], CultureInfo.InvariantCulture);
                        float ang1 = float.Parse(toks[1], CultureInfo.InvariantCulture);
                        int   n    = 16;
                        for (int i = 0; i <= n; i++)
                        {
                            float a = ang0 + (ang1 - ang0) * i / n;
                            verts.Add(originLocal + center +
                                      new Vector3(MathF.Cos(a) * radius,
                                                  MathF.Sin(a) * radius, 0));
                        }
                    }
                }
                isRect = false;
                return verts;
            }

            return null;   // profil non pris en charge
        }

        /*----- utilitaire pour lire un paramètre de trim ---------------*/
        private static float ParseTrim(string token,
                                       Dictionary<string, Dictionary<string,string>> db)
        {
            // 1) nombre direct
            if (float.TryParse(token, NumberStyles.Float,
                               CultureInfo.InvariantCulture, out float val))
                return val;

            // 2) référence #id → IFCPARAMETERVALUE (ou similaire)
            token = token.TrimStart('#');
            if (db.TryGetValue("IFCPARAMETERVALUE", out var pv) &&
                pv.TryGetValue(token, out var raw) &&
                float.TryParse(raw.Trim('(', ')'),
                               NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                return val;

            // défaut
            return 0f;
        }

        /*--------------------------------------------------------------*/
        /*  Trouver l'IfcExtrudedAreaSolid lié à une PDS                */
        /*--------------------------------------------------------------*/
        public static string? FindExtrudedSolid(string pdsParams,
                                                Dictionary<string, Dictionary<string,string>> db)
        {
            // 1) lien direct
            foreach (var id in IdMatches(pdsParams))
                if (db.TryGetValue("IFCEXTRUDEDAREASOLID", out var solids) &&
                    solids.ContainsKey(id))
                    return id;

            // 2) via ShapeRepresentation
            if (db.TryGetValue("IFCSHAPEREPRESENTATION", out var repDict))
            {
                foreach (var repId in IdMatches(pdsParams))
                {
                    if (!repDict.TryGetValue(repId, out var repParams)) continue;
                    if (!repParams.Contains("SweptSolid", StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var cid in IdMatches(repParams))
                        if (db["IFCEXTRUDEDAREASOLID"].ContainsKey(cid))
                            return cid;
                }
            }
            return null;
        }
    }
}
