using System.Collections.Generic;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Das Hoehenfeld der ganzen Insel. Rechnet aus der Kartenskizze eine
    /// Landschaft: Meeresgrund, Kuestenrampe, Berge, See, Strandbogen - und
    /// planiert hinterher die Strassenkorridore, damit keine Stufen entstehen.
    ///
    /// Einmal gebaut, danach nur noch abgefragt. Die Stadtteil- und
    /// Favela-Generatoren fragen hier Hoehe und Steigung ab.
    /// </summary>
    public class RioWorldTerrain
    {
        public readonly float MinX, MaxX, MinZ, MaxZ;
        public readonly float Cell;
        public readonly int CountX, CountZ;

        readonly float[] _height;
        readonly Vector2[] _rio;
        readonly Vector2[] _niteroi;

        // Strassenstuetzpunkte, in einem groben Raster abgelegt
        struct RoadSample { public Vector2 P; public float H; }
        readonly Dictionary<long, List<RoadSample>> _roadGrid = new Dictionary<long, List<RoadSample>>();
        const float RoadGridCell = 120f;
        const float RoadHalfWidth = 9f;
        const float RoadShoulder = 13f;

        public RioWorldTerrain(float minX, float maxX, float minZ, float maxZ, float cell)
        {
            MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ; Cell = cell;
            CountX = Mathf.RoundToInt((maxX - minX) / cell) + 1;
            CountZ = Mathf.RoundToInt((maxZ - minZ) / cell) + 1;

            _rio = RioWorldLayout.ToWorld(RioWorldLayout.RioOutlinePx);
            _niteroi = RioWorldLayout.ToWorld(RioWorldLayout.NiteroiOutlinePx);
            _height = new float[CountX * CountZ];

            BuildRoadSamples();

            for (int ix = 0; ix < CountX; ix++)
            for (int iz = 0; iz < CountZ; iz++)
                _height[ix * CountZ + iz] = Evaluate(new Vector2(minX + ix * cell, minZ + iz * cell));
        }

        // ------------------------------------------------------------------
        // Abfragen
        // ------------------------------------------------------------------

        public float HeightAtIndex(int ix, int iz)
        {
            ix = Mathf.Clamp(ix, 0, CountX - 1);
            iz = Mathf.Clamp(iz, 0, CountZ - 1);
            return _height[ix * CountZ + iz];
        }

        public Vector3 VertexAt(int ix, int iz) =>
            new Vector3(MinX + ix * Cell, HeightAtIndex(ix, iz), MinZ + iz * Cell);

        /// <summary>Hoehe an beliebiger Stelle, bilinear zwischen den Rasterpunkten.</summary>
        public float SampleHeight(Vector2 p)
        {
            float fx = Mathf.Clamp((p.x - MinX) / Cell, 0f, CountX - 1.001f);
            float fz = Mathf.Clamp((p.y - MinZ) / Cell, 0f, CountZ - 1.001f);
            int ix = (int)fx, iz = (int)fz;
            float tx = fx - ix, tz = fz - iz;

            float h00 = HeightAtIndex(ix, iz), h10 = HeightAtIndex(ix + 1, iz);
            float h01 = HeightAtIndex(ix, iz + 1), h11 = HeightAtIndex(ix + 1, iz + 1);
            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }

        /// <summary>Groesster Hoehenunterschied zu den Nachbarn, geteilt durch die Zellweite.</summary>
        public float SlopeAt(Vector2 p)
        {
            float h = SampleHeight(p);
            float d = Cell;
            float dx = Mathf.Abs(SampleHeight(p + new Vector2(d, 0f)) - h);
            float dz = Mathf.Abs(SampleHeight(p + new Vector2(0f, d)) - h);
            return Mathf.Max(dx, dz) / d;
        }

        public bool IsLand(Vector2 p) => SignedDistance(p) > 0f;

        public float DistanceToCoast(Vector2 p) => SignedDistance(p);

        public bool IsInLake(Vector2 p) =>
            Vector2.Distance(p, RioWorldLayout.LakeCenter) < RioWorldLayout.LakeRadius;

        public float RoadWeight(Vector2 p)
        {
            float best = float.MaxValue;
            ForEachNearbyRoadSample(p, s =>
            {
                float d = Vector2.Distance(p, s.P);
                if (d < best) best = d;
            });
            if (best >= RoadHalfWidth + RoadShoulder) return 0f;
            if (best <= RoadHalfWidth) return 1f;
            return 1f - Mathf.InverseLerp(RoadHalfWidth, RoadHalfWidth + RoadShoulder, best);
        }

        // ------------------------------------------------------------------
        // Hoehenfunktion
        // ------------------------------------------------------------------

        float Evaluate(Vector2 p)
        {
            float d = SignedDistance(p);

            if (d <= 0f)
            {
                // Meeresgrund: nach aussen immer tiefer
                return -Mathf.Lerp(0.4f, 46f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(-d / 950f)));
            }

            // Strandbogen: dort laeuft das Land flacher aus, der Sand wird breit
            float beach = 1f - Mathf.Clamp01(Vector2.Distance(p, RioWorldLayout.BeachCenter)
                                             / RioWorldLayout.BeachRadius);
            float ramp = RioWorldLayout.CoastRamp * (1f + 2.6f * beach * beach);

            float h = Mathf.Lerp(0f, RioWorldLayout.CityHeight,
                                 Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / ramp)));

            // Berge
            foreach (var m in RioWorldLayout.Mountains)
            {
                float t = 1f - Mathf.Clamp01(Vector2.Distance(p, m.Center) / m.Radius);
                if (t <= 0f) continue;
                h += m.Height * t * t * (3f - 2f * t);
            }

            // grobe Welligkeit, damit das Land nicht wie ein Brett wirkt
            float inland = Mathf.Clamp01(d / 300f);
            h += (Mathf.PerlinNoise(p.x * 0.0013f + 30f, p.y * 0.0013f + 70f) - 0.5f) * 16f * inland;
            h += (Mathf.PerlinNoise(p.x * 0.006f + 5f, p.y * 0.006f + 9f) - 0.5f) * 3.5f * inland;

            // See: Becken ausheben
            float lakeD = Vector2.Distance(p, RioWorldLayout.LakeCenter);
            if (lakeD < RioWorldLayout.LakeRadius)
            {
                float t = 1f - lakeD / RioWorldLayout.LakeRadius;
                float basin = RioWorldLayout.LakeLevel - 9f * Mathf.SmoothStep(0f, 1f, t);
                h = Mathf.Min(h, basin);
            }

            return Mathf.Max(h, 0.15f);
        }

        // ------------------------------------------------------------------
        // Strassen planieren
        // ------------------------------------------------------------------

        void BuildRoadSamples()
        {
            foreach (var road in RioWorldLayout.Roads)
            {
                var pts = RioWorldLayout.ToWorld(road);
                var samples = new List<RoadSample>();

                for (int i = 0; i < pts.Length - 1; i++)
                {
                    float len = Vector2.Distance(pts[i], pts[i + 1]);
                    int steps = Mathf.Max(2, Mathf.CeilToInt(len / 18f));
                    for (int s = 0; s < steps; s++)
                    {
                        Vector2 p = Vector2.Lerp(pts[i], pts[i + 1], s / (float)steps);
                        samples.Add(new RoadSample { P = p, H = Evaluate(p) });
                    }
                }
                samples.Add(new RoadSample { P = pts[pts.Length - 1], H = Evaluate(pts[pts.Length - 1]) });

                Smooth(samples);
                foreach (var s in samples) Register(s);
            }
        }

        static void Smooth(List<RoadSample> samples)
        {
            // Mehrfach glaetten: eine Strasse soll keine Treppe sein
            for (int pass = 0; pass < 14; pass++)
            {
                var copy = new float[samples.Count];
                for (int i = 0; i < samples.Count; i++)
                {
                    int a = Mathf.Max(0, i - 1), b = Mathf.Min(samples.Count - 1, i + 1);
                    copy[i] = (samples[a].H + samples[i].H + samples[b].H) / 3f;
                }
                for (int i = 0; i < samples.Count; i++)
                {
                    var s = samples[i];
                    s.H = Mathf.Max(copy[i], 0.8f);
                    samples[i] = s;
                }
            }
        }

        static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

        void Register(RoadSample s)
        {
            int cx = Mathf.FloorToInt(s.P.x / RoadGridCell);
            int cz = Mathf.FloorToInt(s.P.y / RoadGridCell);
            long k = Key(cx, cz);
            if (!_roadGrid.TryGetValue(k, out var list))
            {
                list = new List<RoadSample>();
                _roadGrid[k] = list;
            }
            list.Add(s);
        }

        void ForEachNearbyRoadSample(Vector2 p, System.Action<RoadSample> action)
        {
            int cx = Mathf.FloorToInt(p.x / RoadGridCell);
            int cz = Mathf.FloorToInt(p.y / RoadGridCell);
            for (int ox = -1; ox <= 1; ox++)
            for (int oz = -1; oz <= 1; oz++)
            {
                if (_roadGrid.TryGetValue(Key(cx + ox, cz + oz), out var list))
                    foreach (var s in list) action(s);
            }
        }

        // ------------------------------------------------------------------
        // Stempel
        //
        // Die Stadt legt hier ihre Hoehen ab, danach wird das Gelaende darauf
        // gezogen. Vorher lief es umgekehrt: das Gelaende stand fest, und die
        // Kacheln wurden darauf gelegt - auf einen Untergrund, der nichts von
        // ihnen wusste. Daher die schwebenden Haeuser und das Gras, das durch
        // die Fahrbahn stiess.
        // ------------------------------------------------------------------

        float[] _soll;
        float[] _sollGewicht;

        public void StempelBeginnen()
        {
            _soll = new float[_height.Length];
            _sollGewicht = new float[_height.Length];
        }

        /// <summary>
        /// Meldet eine Flaeche an, die auf Hoehe <paramref name="h"/> liegen
        /// soll. <paramref name="rand"/> ist der Streifen aussen herum, in dem
        /// das Gelaende weich in seine eigene Hoehe zurueckgeht.
        /// </summary>
        public void Stempeln(Vector2 mitte, Vector2 groesse, float yaw, float h, float rand)
        {
            if (_soll == null) return;

            // Unitys Drehsinn, wie im Belegungsraster (vorher -yaw, gespiegelt)
            float rad = yaw * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float hx = groesse.x * 0.5f, hz = groesse.y * 0.5f;

            float ex = Mathf.Abs(groesse.x * cs) * 0.5f + Mathf.Abs(groesse.y * sn) * 0.5f + rand;
            float ez = Mathf.Abs(groesse.x * sn) * 0.5f + Mathf.Abs(groesse.y * cs) * 0.5f + rand;

            int x0 = Mathf.Max(0, Mathf.FloorToInt((mitte.x - ex - MinX) / Cell));
            int x1 = Mathf.Min(CountX - 1, Mathf.CeilToInt((mitte.x + ex - MinX) / Cell));
            int z0 = Mathf.Max(0, Mathf.FloorToInt((mitte.y - ez - MinZ) / Cell));
            int z1 = Mathf.Min(CountZ - 1, Mathf.CeilToInt((mitte.y + ez - MinZ) / Cell));

            for (int ix = x0; ix <= x1; ix++)
            for (int iz = z0; iz <= z1; iz++)
            {
                float wx = MinX + ix * Cell - mitte.x;
                float wz = MinZ + iz * Cell - mitte.y;
                float lx = wx * cs - wz * sn;
                float lz = wx * sn + wz * cs;

                // Abstand nach aussen vom Rechteck, in seinem eigenen System
                float dx = Mathf.Max(0f, Mathf.Abs(lx) - hx);
                float dz = Mathf.Max(0f, Mathf.Abs(lz) - hz);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d > rand) continue;

                float w = rand <= 0f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, d / rand);
                if (w <= 0f) continue;

                int i = ix * CountZ + iz;
                _soll[i] += h * w;
                _sollGewicht[i] += w;
            }
        }

        /// <summary>
        /// Rechnet die Stempel in das Hoehenfeld ein und glaettet danach nur
        /// die Zellen, die kein Stempel beansprucht hat - so bleiben die
        /// Plateaus exakt, und das Gelaende daneben schliesst weich an.
        /// </summary>
        public void StempelAnwenden(int glaettung)
        {
            if (_soll == null) return;

            var gestempelt = new bool[_height.Length];
            for (int i = 0; i < _height.Length; i++)
            {
                float g = _sollGewicht[i];
                if (g <= 1e-4f) continue;
                float soll = _soll[i] / g;
                float t = Mathf.Clamp01(g);
                _height[i] = Mathf.Lerp(_height[i], soll, t);
                if (t > 0.98f) gestempelt[i] = true;
            }

            var copy = new float[_height.Length];
            for (int pass = 0; pass < glaettung; pass++)
            {
                System.Array.Copy(_height, copy, _height.Length);
                for (int ix = 1; ix < CountX - 1; ix++)
                for (int iz = 1; iz < CountZ - 1; iz++)
                {
                    int i = ix * CountZ + iz;
                    if (gestempelt[i]) continue;
                    if (copy[i] < 0.2f) continue;          // Wasser bleibt
                    float avg = (copy[(ix - 1) * CountZ + iz] + copy[(ix + 1) * CountZ + iz]
                               + copy[ix * CountZ + iz - 1] + copy[ix * CountZ + iz + 1]) * 0.25f;
                    _height[i] = Mathf.Lerp(copy[i], avg, 0.5f);
                }
            }

            _soll = null;
            _sollGewicht = null;
        }

        /// <summary>Nach dem Befuellen: Strassenkorridore in das Hoehenfeld einbuegeln.</summary>
        public void FlattenRoads()
        {
            for (int ix = 0; ix < CountX; ix++)
            for (int iz = 0; iz < CountZ; iz++)
            {
                var p = new Vector2(MinX + ix * Cell, MinZ + iz * Cell);
                float nearest = float.MaxValue, roadH = 0f;

                ForEachNearbyRoadSample(p, s =>
                {
                    float d = Vector2.Distance(p, s.P);
                    if (d < nearest) { nearest = d; roadH = s.H; }
                });

                if (nearest >= RoadHalfWidth + RoadShoulder) continue;

                float w = nearest <= RoadHalfWidth
                    ? 1f
                    : 1f - Mathf.InverseLerp(RoadHalfWidth, RoadHalfWidth + RoadShoulder, nearest);

                int i = ix * CountZ + iz;
                _height[i] = Mathf.Lerp(_height[i], roadH, Mathf.SmoothStep(0f, 1f, w));
            }
        }

        /// <summary>
        /// Glaettet das flache Land, laesst die Morros stehen.
        /// Eine Stadt steht auf einer Ebene, nicht auf Wellblech. Ohne das
        /// saessen die Strassenkacheln als Treppe im Gelaende, weil jede
        /// Kachel flach ist, der Untergrund aber nicht.
        /// </summary>
        public void SmoothLowland(int passes)
        {
            var copy = new float[_height.Length];

            for (int pass = 0; pass < passes; pass++)
            {
                System.Array.Copy(_height, copy, _height.Length);

                for (int ix = 1; ix < CountX - 1; ix++)
                for (int iz = 1; iz < CountZ - 1; iz++)
                {
                    int i = ix * CountZ + iz;
                    float h = copy[i];
                    if (h < 0.2f || h > 80f) continue;      // Wasser und Berge bleiben

                    float sum = copy[i] * 4f
                              + copy[(ix - 1) * CountZ + iz] + copy[(ix + 1) * CountZ + iz]
                              + copy[ix * CountZ + iz - 1] + copy[ix * CountZ + iz + 1];
                    float avg = sum / 8f;

                    // Je flacher, desto staerker glaetten. Am Hangfuss soll
                    // der Uebergang zum Berg erhalten bleiben.
                    float steep = Mathf.Abs(copy[(ix + 1) * CountZ + iz] - copy[(ix - 1) * CountZ + iz])
                                + Mathf.Abs(copy[ix * CountZ + iz + 1] - copy[ix * CountZ + iz - 1]);
                    float w = 1f - Mathf.Clamp01(steep / (Cell * 0.9f));

                    _height[i] = Mathf.Lerp(copy[i], avg, w);
                }
            }
        }

        // ------------------------------------------------------------------
        // Polygongeometrie
        // ------------------------------------------------------------------

        float SignedDistance(Vector2 p)
        {
            float dRio = PolyDistance(p, _rio);
            float dNit = PolyDistance(p, _niteroi);
            if (PointInPoly(p, _rio)) return dRio;
            if (PointInPoly(p, _niteroi)) return dNit;
            return -Mathf.Min(dRio, dNit);
        }

        static float PolyDistance(Vector2 p, Vector2[] poly)
        {
            float best = float.MaxValue;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                float d = SegmentDistance(p, poly[j], poly[i]);
                if (d < best) best = d;
            }
            return best;
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 1e-6f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + ab * t);
        }

        static bool PointInPoly(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
