using System.Collections.Generic;
using Rio.World;
using UnityEditor;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Verteilt die 19 Hausmodelle ueber die Insel.
    ///
    /// Ablauf:
    /// 1. Wohlstandsfeld aus Hanglage, Strand-, See- und Bankennaehe, plus
    ///    grobem Rauschen fuer zusammenhaengende Viertel.
    /// 2. Die zwei Mansions zuerst, als Einzelstuecke auf den besten Plaetzen.
    /// 3. Dann Grundstuecke rastern und je Grundstueck ein Modell ziehen -
    ///    gewichtet um den Wohlstandswert, nicht hart eingeteilt.
    /// 4. Aus denselben Platzierungen die groben Kloetze fuer die Ferne bauen.
    ///
    /// Reiche Gegenden bekommen ein weiteres Raster als arme. Dadurch passen
    /// die grossen Modelle ueberhaupt erst hin, und es sieht nebenbei richtig
    /// aus: viel Platz oben, dicht gedraengt unten.
    /// </summary>
    public class RioHousePlanner
    {
        // Hoehen in Metern, aus haeuser.json (size[1]). Index wie RioHausDaten.Haeuser.
        static readonly float[] ModelHeight =
        {
            3.3f, 5.0f, 9.2f, 9.6f, 9.1f, 7.2f, 10.0f, 9.1f, 8.8f, 22.5f,
            8.1f, 7.2f, 9.6f, 31.5f, 8.6f, 6.1f, 10.6f, 13.7f, 16.0f
        };

        public const int FirstResidential = 0;
        public const int LastResidential = 14;   // 01..15, die Wohnhaeuser
        public const int ModelTankstelle = 15;
        public const int VillaLeblon = 16;
        public const int VillaJoatinga = 17;
        public const int ModelBank = 18;

        readonly RioWorldTerrain _t;
        readonly List<RioHouseWorld.Placement> _placements = new List<RioHouseWorld.Placement>();
        readonly List<Vector2> _reserved = new List<Vector2>();
        readonly List<float> _reservedRadius = new List<float>();

        // Nur fuer den Abstand der beiden Mansions zueinander. Frueher lief das
        // ueber die normale Sperrliste - dann war rund um jede Mansion 900 m
        // weit nichts mehr, weil die Sperre auch fuer alle anderen Haeuser galt.
        readonly List<Vector2> _villaSpots = new List<Vector2>();

        public IReadOnlyList<RioHouseWorld.Placement> Placements => _placements;
        public int Count => _placements.Count;

        public RioHousePlanner(RioWorldTerrain terrain) { _t = terrain; }

        // ------------------------------------------------------------------
        // Wohlstand
        // ------------------------------------------------------------------

        /// <summary>0 = Favela, 1 = Mansion. Rein aus der Geografie plus Rauschen.</summary>
        public float Wealth(Vector2 p)
        {
            float w = 0.42f;

            // Hanglage zieht stark runter: am Hang wird gebaut, was geht.
            float slope = _t.SlopeAt(p);
            w -= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.14f, 0.5f, slope)) * 0.5f;

            // Strandnaehe ist das Teuerste, was die Insel hat.
            float beach = Vector2.Distance(p, RioWorldLayout.BeachCenter);
            w += (1f - Mathf.Clamp01(beach / 1100f)) * 0.42f;

            // Seeblick zieht ebenfalls an.
            float lake = Vector2.Distance(p, RioWorldLayout.LakeCenter);
            w += (1f - Mathf.Clamp01(lake / 700f)) * 0.22f;

            // Zentrum: teuer, aber Hochhaus statt Villa - daher nur mittel.
            w += Downtown(p) * 0.18f;

            // Hafen, Flughaefen und Kaserne druecken die Gegend.
            w -= Industry(p) * 0.3f;

            // Grosswelliges Rauschen: so entstehen zusammenhaengende Viertel
            // statt perfekter Ringe um den Strand.
            w += (Mathf.PerlinNoise(p.x * 0.0016f + 13f, p.y * 0.0016f + 29f) - 0.5f) * 0.42f;
            w += (Mathf.PerlinNoise(p.x * 0.0065f + 3f, p.y * 0.0065f + 7f) - 0.5f) * 0.14f;

            return Mathf.Clamp01(w);
        }

        static float Downtown(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var pl in RioWorldLayout.Places)
                if (pl.Kind == RioWorldLayout.Poi.BankGross)
                    best = Mathf.Min(best, Vector2.Distance(p, pl.Position));
            return 1f - Mathf.Clamp01(best / 1300f);
        }

        static float Industry(Vector2 p)
        {
            float best = float.MaxValue;
            foreach (var pl in RioWorldLayout.Places)
            {
                if (pl.Kind != RioWorldLayout.Poi.Hafen &&
                    pl.Kind != RioWorldLayout.Poi.FlughafenGross &&
                    pl.Kind != RioWorldLayout.Poi.FlughafenKlein &&
                    pl.Kind != RioWorldLayout.Poi.Militaerbasis) continue;
                best = Mathf.Min(best, Vector2.Distance(p, pl.Position));
            }
            return 1f - Mathf.Clamp01(best / 700f);
        }

        // ------------------------------------------------------------------
        // Platzieren
        // ------------------------------------------------------------------

        public bool Buildable(Vector2 p, float maxSlope, float minH, float maxH, float lotRadius)
        {
            if (!_t.IsLand(p)) return false;
            if (_t.DistanceToCoast(p) < lotRadius + 12f) return false;
            if (_t.IsInLake(p)) return false;
            if (_t.RoadWeight(p) > 0.02f) return false;
            float h = _t.SampleHeight(p);
            if (h < minH || h > maxH) return false;
            if (_t.SlopeAt(p) > maxSlope) return false;
            return !IsReserved(p, lotRadius);
        }

        bool IsReserved(Vector2 p, float radius)
        {
            for (int i = 0; i < _reserved.Count; i++)
                if (Vector2.Distance(p, _reserved[i]) < _reservedRadius[i] + radius) return true;

            foreach (var pl in RioWorldLayout.Places)
                if (Vector2.Distance(p, pl.Position) < 150f + radius) return true;

            return false;
        }

        public void Reserve(Vector2 p, float radius)
        {
            _reserved.Add(p);
            _reservedRadius.Add(radius);
        }

        void Add(int model, Vector2 p, float yaw, int seed)
        {
            float ground = _t.SampleHeight(p);
            _placements.Add(new RioHouseWorld.Placement
            {
                position = new Vector3(p.x, ground - 0.3f, p.y),
                yaw = yaw,
                model = model,
                colorSeed = seed
            });
            var lot = Lot(model);
            Reserve(p, Mathf.Max(lot.x, lot.y) * 0.5f);
        }

        public static Vector2 Lot(int model) =>
            new Vector2(RioHausDaten.Haeuser[model].lotBreite, RioHausDaten.Haeuser[model].lotTiefe);

        public static float Height(int model) => ModelHeight[model];

        // ------------------------------------------------------------------
        // Die zwei Mansions
        // ------------------------------------------------------------------

        /// <summary>
        /// Sucht die beiden besten Plaetze der Insel und setzt dort je einmal
        /// eine Mansion. Laeuft vor allem anderen, damit nichts im Weg steht.
        /// </summary>
        public void PlaceVillas(System.Random rng)
        {
            int[] villas = { VillaJoatinga, VillaLeblon };   // groesste zuerst
            const float minApart = 900f;

            foreach (int model in villas)
            {
                var lot = Lot(model);
                float radius = Mathf.Max(lot.x, lot.y) * 0.5f + 10f;

                Vector2 best = Vector2.zero;
                float bestScore = float.MinValue;

                // Grobes Raster ueber die ganze Welt, dann das Beste nehmen.
                for (float x = _t.MinX; x < _t.MaxX; x += 40f)
                for (float z = _t.MinZ; z < _t.MaxZ; z += 40f)
                {
                    var p = new Vector2(x, z);
                    if (!Buildable(p, 0.10f, 4f, 90f, radius)) continue;
                    if (TooCloseToVilla(p, minApart)) continue;

                    float score = Wealth(p);
                    // Blick aufs Wasser ist das halbe Argument fuer eine Mansion
                    float coast = _t.DistanceToCoast(p);
                    score += (1f - Mathf.Clamp01(coast / 400f)) * 0.3f;

                    if (score > bestScore) { bestScore = score; best = p; }
                }

                if (bestScore == float.MinValue) continue;

                float yaw = YawTowardsWater(best);
                Add(model, best, yaw, rng.Next(1, int.MaxValue));
                _villaSpots.Add(best);   // nur fuer den Abstand zur zweiten Mansion
            }
        }

        bool TooCloseToVilla(Vector2 p, float minApart)
        {
            foreach (var v in _villaSpots)
                if (Vector2.Distance(p, v) < minApart) return true;
            return false;
        }

        /// <summary>Dreht die Vorderseite (+Z) Richtung offenes Wasser.</summary>
        float YawTowardsWater(Vector2 p)
        {
            float bestYaw = 0f, bestDist = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (float d = 40f; d <= 500f; d += 40f)
                {
                    if (_t.IsLand(p + dir * d)) continue;
                    if (d < bestDist) { bestDist = d; bestYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg; }
                    break;
                }
            }
            return bestYaw;
        }

        // ------------------------------------------------------------------
        // Wohnviertel
        // ------------------------------------------------------------------

        public void PlaceDistricts(float blockSize, float streetMargin)
        {
            for (float bx = _t.MinX; bx < _t.MaxX; bx += blockSize)
            for (float bz = _t.MinZ; bz < _t.MaxZ; bz += blockSize)
            {
                var center = new Vector2(bx + blockSize * 0.5f, bz + blockSize * 0.5f);
                if (!_t.IsLand(center) || _t.IsInLake(center)) continue;

                float h = _t.SampleHeight(center);
                if (h < 3f || h > 170f) continue;

                float slope = _t.SlopeAt(center);
                if (slope > 0.85f) continue;

                float w = Wealth(center);

                // Reich wohnt weitlaeufig, arm dicht an dicht.
                float pitch = Mathf.Lerp(9f, 26f, w);
                if (slope > 0.2f) pitch = 9f;          // Favela am Hang

                float inner = blockSize - streetMargin * 2f;
                int n = Mathf.FloorToInt(inner / pitch);
                if (n < 1) continue;

                float yaw = BlockYaw(bx, bz);

                for (int ix = 0; ix < n; ix++)
                for (int iz = 0; iz < n; iz++)
                {
                    var p = new Vector2(
                        bx + streetMargin + pitch * (ix + 0.5f),
                        bz + streetMargin + pitch * (iz + 0.5f));

                    int seed = Hash(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y));
                    var rng = new System.Random(seed);

                    float local = Mathf.Clamp01(Wealth(p));
                    int model = PickModel(local, pitch, _t.SlopeAt(p), rng);
                    if (model < 0) continue;

                    var lot = Lot(model);
                    float radius = Mathf.Max(lot.x, lot.y) * 0.5f;
                    if (!Buildable(p, 0.9f, 2.5f, 180f, radius * 0.75f)) continue;

                    // Leichte Luecken, damit keine perfekte Reihe entsteht
                    if (rng.NextDouble() < 0.12) continue;

                    float jitter = (float)(rng.NextDouble() - 0.5) * 6f;
                    Add(model, p, yaw + jitter, rng.Next(1, int.MaxValue));
                }
            }
        }

        /// <summary>Alle Haeuser eines Blocks schauen in dieselbe Richtung.</summary>
        static float BlockYaw(float bx, float bz)
        {
            int q = Hash(Mathf.RoundToInt(bx), Mathf.RoundToInt(bz)) & 3;
            return q * 90f;
        }

        /// <summary>
        /// Zieht ein Modell um den Wohlstandswert herum statt hart einzuteilen.
        /// Dadurch steht in einer guten Gegend auch mal ein einfacheres Haus -
        /// wie in echt. Zu grosse Modelle fallen danach durchs Rastersieb.
        /// </summary>
        int PickModel(float wealth, float pitch, float slope, System.Random rng)
        {
            // Am Steilhang wird nur arm gebaut.
            float top = slope > 0.2f ? 4f : LastResidential;

            float center = wealth * LastResidential;
            float spread = 1.8f;

            // Zwei Zufallszahlen addiert ergeben eine Glocke - reicht hier.
            float g = (float)(rng.NextDouble() + rng.NextDouble() - 1f);
            int rank = Mathf.RoundToInt(center + g * spread);
            rank = Mathf.Clamp(rank, FirstResidential, Mathf.RoundToInt(top));

            // Vom gezogenen Rang abwaerts das erste Modell nehmen, das aufs
            // Grundstueck passt. Skaliert wird nie.
            for (int m = rank; m >= FirstResidential; m--)
            {
                var lot = Lot(m);
                if (lot.x <= pitch && lot.y <= pitch) return m;
            }
            return -1;
        }

        public static int Hash(int x, int z)
        {
            unchecked
            {
                int h = x * 73856093 ^ z * 19349663 ^ 0x5f3a2b1;
                h ^= h >> 13; h *= 1274126177; h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        // ------------------------------------------------------------------
        // Spezialgebaeude
        // ------------------------------------------------------------------

        /// <summary>Banken an die Bank-Orte der Legende, Tankstellen an die Strassen.</summary>
        public void PlaceSpecials(System.Random rng)
        {
            foreach (var place in RioWorldLayout.Places)
            {
                if (place.Kind != RioWorldLayout.Poi.BankGross &&
                    place.Kind != RioWorldLayout.Poi.BankKlein) continue;

                float yaw = BlockYaw(place.Position.x, place.Position.y);
                float ground = _t.SampleHeight(place.Position);
                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(place.Position.x, ground - 0.2f, place.Position.y),
                    yaw = yaw,
                    model = ModelBank,
                    colorSeed = rng.Next(1, int.MaxValue)
                });
                Reserve(place.Position, 60f);
            }

            // Tankstellen entlang der Hauptstrasse, mit Abstand
            var road = RioWorldLayout.ToWorld(RioWorldLayout.MainRoadPx);
            float since = 0f;
            for (int i = 0; i < road.Length - 1; i++)
            {
                float len = Vector2.Distance(road[i], road[i + 1]);
                for (float d = 0f; d < len; d += 25f)
                {
                    since += 25f;
                    if (since < 900f) continue;

                    Vector2 on = Vector2.Lerp(road[i], road[i + 1], d / len);
                    Vector2 dir = (road[i + 1] - road[i]).normalized;
                    Vector2 side = new Vector2(-dir.y, dir.x) * 26f;

                    foreach (var cand in new[] { on + side, on - side })
                    {
                        if (!_t.IsLand(cand) || _t.SlopeAt(cand) > 0.12f) continue;
                        if (IsReserved(cand, 20f)) continue;

                        float ground = _t.SampleHeight(cand);
                        _placements.Add(new RioHouseWorld.Placement
                        {
                            position = new Vector3(cand.x, ground - 0.2f, cand.y),
                            yaw = Mathf.Atan2(-side.x, -side.y) * Mathf.Rad2Deg,
                            model = ModelTankstelle,
                            colorSeed = rng.Next(1, int.MaxValue)
                        });
                        Reserve(cand, 40f);
                        since = 0f;
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Statistik
        // ------------------------------------------------------------------

        public int[] CountPerModel()
        {
            var counts = new int[RioHausDaten.Haeuser.Length];
            foreach (var p in _placements) counts[p.model]++;
            return counts;
        }

        public long TotalTriangles()
        {
            long sum = 0;
            foreach (var p in _placements) sum += RioHausDaten.Haeuser[p.model].dreiecke;
            return sum;
        }
    }
}
