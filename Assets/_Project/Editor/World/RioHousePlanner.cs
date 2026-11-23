using System.Collections.Generic;
using Rio.World;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Baut die Stadt: Strassenraster aus Kacheln, Haeuser bündig am Gehweg,
    /// Favelas an den Haengen.
    ///
    /// Der entscheidende Unterschied zur ersten Fassung: Haeuser stehen nicht
    /// mehr auf einem Punktraster verstreut, sondern saeumen die Blockraender.
    /// Eine Stadt besteht aus Strassen und Blocks, nicht aus gleichmaessig
    /// verteilten Punkten - deshalb sah das vorher aus wie ein Schachbrett
    /// mit Luecken und nicht wie Rio.
    ///
    /// Zweiter Unterschied: Ein Block hat einen Charakter. Alle Haeuser darin
    /// kommen aus einer kleinen Auswahl benachbarter Raenge, nicht aus der
    /// ganzen Liste. Dadurch wirken Nachbarhaeuser verwandt statt zufaellig
    /// zusammengewuerfelt.
    /// </summary>
    public class RioHousePlanner
    {
        // Hoehen in Metern, aus haeuser.json (size[1]).
        static readonly float[] ModelHeight =
        {
            3.3f, 5.0f, 9.2f, 9.6f, 9.1f, 7.2f, 10.0f, 9.1f, 8.8f, 22.5f,
            8.1f, 7.2f, 9.6f, 31.5f, 8.6f, 6.1f, 10.6f, 13.7f, 16.0f
        };

        public const int FirstResidential = 0;
        public const int LastResidential = 14;
        public const int ModelTankstelle = 15;
        public const int VillaLeblon = 16;
        public const int VillaJoatinga = 17;
        public const int ModelBank = 18;

        // Strassenraster: 12-m-Kacheln, Block = 5 x 3 Kacheln = 60 x 36 m.
        public const float Cell = 12f;
        /// <summary>
        /// Superblock: 12 x 12 Kacheln = 144 x 144 m. Seine Raender sind
        /// immer Strasse - das sind die Hauptachsen der Stadt.
        ///
        /// Innen bekommt jeder Superblock sein eigenes Raster, je nach
        /// Viertelstyp. Weil alle Periodenlaengen Teiler von 12 sind, treffen
        /// die inneren Strassen immer auf den Rand. Deshalb koennen zwei
        /// Nachbarviertel voellig verschieden geschnitten sein, ohne dass eine
        /// Strasse im Nichts endet.
        /// </summary>
        const int Super = 12;

        public enum Viertel { Centro, Orla, Bairro, Nobre, Industrie }

        /// <summary>Periodenlaenge in X und Z je Viertel. Block = (p-1) Kacheln.</summary>
        static readonly int[,] Raster =
        {
            { 3, 3 },   // Centro:    24 x 24 m, eng und hoch
            { 6, 3 },   // Orla:      60 x 24 m, lange Zeilen zum Wasser
            { 4, 3 },   // Bairro:    36 x 24 m, der Normalfall
            { 6, 4 },   // Nobre:     60 x 36 m, grosszuegig
            { 6, 6 },   // Industrie: 60 x 60 m, wenige grosse Kisten
        };

        /// <summary>Luecke zwischen den Haeusern je Viertel.</summary>
        static readonly float[] Luecke = { 0.1f, 0.8f, 0.5f, 3.2f, 2.5f };

        /// <summary>Verschiebung des Rangs je Viertel, auf den Wohlstand obendrauf.</summary>
        static readonly int[] RangBonus = { 2, 2, 0, 3, -2 };

        readonly RioWorldTerrain _t;
        readonly List<RioHouseWorld.Placement> _placements = new List<RioHouseWorld.Placement>();
        readonly List<Vector2> _reserved = new List<Vector2>();
        readonly List<float> _reservedRadius = new List<float>();
        readonly List<Vector2> _villaSpots = new List<Vector2>();

        bool[] _city;
        Viertel[] _kind;          // je Superblock
        int _sw, _sh;             // Superblocks in X und Z
        int _gw, _gh;

        public IReadOnlyList<RioHouseWorld.Placement> Placements => _placements;
        public int Count => _placements.Count;
        public int RoadTiles { get; private set; }
        public int Houses { get; private set; }
        public int Shacks { get; private set; }
        public int Blocks { get; private set; }

        /// <summary>Innenflaechen der Blocks (x0, z0, x1, z1) fuer die Hoefe.</summary>
        public readonly List<Vector4> BlockRects = new List<Vector4>();

        /// <summary>Wie viele Blocks je Viertelstyp entstanden sind.</summary>
        public readonly int[] KindCount = new int[5];

        public RioHousePlanner(RioWorldTerrain terrain) { _t = terrain; }

        // ------------------------------------------------------------------
        // Wohlstand
        // ------------------------------------------------------------------

        public float Wealth(Vector2 p)
        {
            float w = 0.42f;

            float slope = _t.SlopeAt(p);
            w -= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.45f, slope)) * 0.5f;

            float beach = Vector2.Distance(p, RioWorldLayout.BeachCenter);
            w += (1f - Mathf.Clamp01(beach / 1100f)) * 0.42f;

            float lake = Vector2.Distance(p, RioWorldLayout.LakeCenter);
            w += (1f - Mathf.Clamp01(lake / 700f)) * 0.22f;

            w += Downtown(p) * 0.18f;
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
        // Stadtmaske
        // ------------------------------------------------------------------

        Vector2 CellCenter(int gx, int gz) =>
            new Vector2(_t.MinX + (gx + 0.5f) * Cell, _t.MinZ + (gz + 0.5f) * Cell);

        /// <summary>
        /// Einmal durchrechnen, wo ueberhaupt Stadt sein kann. Danach fragen
        /// Strassen und Haeuser nur noch diese Tabelle ab.
        /// </summary>
        public void BuildCityMask()
        {
            _gw = Mathf.FloorToInt((_t.MaxX - _t.MinX) / Cell);
            _gh = Mathf.FloorToInt((_t.MaxZ - _t.MinZ) / Cell);
            _city = new bool[_gw * _gh];

            for (int gx = 0; gx < _gw; gx++)
            for (int gz = 0; gz < _gh; gz++)
            {
                var p = CellCenter(gx, gz);
                if (!_t.IsLand(p)) continue;
                if (_t.IsInLake(p)) continue;
                if (_t.DistanceToCoast(p) < 22f) continue;
                if (_t.RoadWeight(p) > 0.02f) continue;     // Platz fuer die Hauptstrassen

                float h = _t.SampleHeight(p);
                if (h < 2.5f || h > 70f) continue;
                if (_t.SlopeAt(p) > 0.12f) continue;        // flach genug fuer ein Raster
                if (NearPlace(p, 45f)) continue;

                _city[gz * _gw + gx] = true;
            }
        }

        bool IsCity(int gx, int gz) =>
            gx >= 0 && gz >= 0 && gx < _gw && gz < _gh && _city[gz * _gw + gx];

        /// <summary>
        /// Welcher Viertelstyp herrscht hier? Lage schlaegt Wohlstand:
        /// am Hafen ist Industrie, egal wie teuer die Gegend sonst waere.
        /// </summary>
        Viertel KindAt(Vector2 p)
        {
            if (Industry(p) > 0.4f) return Viertel.Industrie;
            if (Downtown(p) > 0.52f) return Viertel.Centro;
            if (_t.DistanceToCoast(p) < 420f) return Viertel.Orla;
            return Wealth(p) > 0.5f ? Viertel.Nobre : Viertel.Bairro;
        }

        public void BuildDistrictMap()
        {
            _sw = _gw / Super + 1;
            _sh = _gh / Super + 1;
            _kind = new Viertel[_sw * _sh];

            for (int sx = 0; sx < _sw; sx++)
            for (int sz = 0; sz < _sh; sz++)
            {
                var c = new Vector2(_t.MinX + (sx * Super + Super * 0.5f) * Cell,
                                    _t.MinZ + (sz * Super + Super * 0.5f) * Cell);
                _kind[sz * _sw + sx] = KindAt(c);
            }
        }

        public Viertel KindOfCell(int gx, int gz)
        {
            int sx = Mathf.Clamp(gx / Super, 0, _sw - 1);
            int sz = Mathf.Clamp(gz / Super, 0, _sh - 1);
            return _kind[sz * _sw + sx];
        }

        bool IsStreetCell(int gx, int gz)
        {
            if (gx % Super == 0 || gz % Super == 0) return true;   // Hauptachse
            var k = KindOfCell(gx, gz);
            return gx % Raster[(int)k, 0] == 0 || gz % Raster[(int)k, 1] == 0;
        }

        bool IsRoadCell(int gx, int gz) => IsCity(gx, gz) && IsStreetCell(gx, gz);

        static bool NearPlace(Vector2 p, float radius)
        {
            foreach (var pl in RioWorldLayout.Places)
                if (Vector2.Distance(p, pl.Position) < radius) return true;
            return false;
        }

        // ------------------------------------------------------------------
        // Strassennetz
        // ------------------------------------------------------------------

        public void PlaceRoads()
        {
            for (int gx = 0; gx < _gw; gx++)
            for (int gz = 0; gz < _gh; gz++)
            {
                if (!IsRoadCell(gx, gz)) continue;

                int mask = 0;
                if (IsRoadCell(gx, gz + 1)) mask |= RioModelCatalog.N;
                if (IsRoadCell(gx + 1, gz)) mask |= RioModelCatalog.E;
                if (IsRoadCell(gx, gz - 1)) mask |= RioModelCatalog.S;
                if (IsRoadCell(gx - 1, gz)) mask |= RioModelCatalog.W;

                // Einzelne Zellen ohne Anschluss bringen nichts
                if (mask == 0) continue;

                // Eine Sackgasse als Gerade setzen, die gibt es auf dem
                // Raster oft am Stadtrand.
                if (mask == RioModelCatalog.N || mask == RioModelCatalog.S) mask = RioModelCatalog.N | RioModelCatalog.S;
                if (mask == RioModelCatalog.E || mask == RioModelCatalog.W) mask = RioModelCatalog.E | RioModelCatalog.W;

                if (!RioModelCatalog.FindTile(mask, out int model, out float yaw)) continue;

                var p = CellCenter(gx, gz);
                int seed = Hash(gx, gz);

                // Ab und zu Kopfsteinpflaster statt Asphalt, das bricht die
                // Gleichfoermigkeit ohne das Netz zu stoeren.
                if (model == RioModelCatalog.RoadStraight && (seed % 11) == 0)
                    model = RioModelCatalog.RoadCobble;

                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(p.x, _t.SampleHeight(p) + 0.05f, p.y),
                    yaw = yaw,
                    model = model,
                    colorSeed = seed
                });
                RoadTiles++;
            }
        }

        // ------------------------------------------------------------------
        // Haeuser an den Blockraendern
        // ------------------------------------------------------------------

        public void PlaceBlocks()
        {
            for (int sx = 0; sx < _sw; sx++)
            for (int sz = 0; sz < _sh; sz++)
            {
                var kind = _kind[sz * _sw + sx];
                int px = Raster[(int)kind, 0], pz = Raster[(int)kind, 1];
                int cw = px - 1, cd = pz - 1;

                for (int ix = 0; ix < Super / px; ix++)
                for (int iz = 0; iz < Super / pz; iz++)
                {
                    int cx0 = sx * Super + ix * px + 1;
                    int cz0 = sz * Super + iz * pz + 1;

                    bool ok = true;
                    for (int i = 0; i < cw && ok; i++)
                    for (int j = 0; j < cd && ok; j++)
                        if (!IsCity(cx0 + i, cz0 + j)) ok = false;
                    if (!ok) continue;

                    float x0 = _t.MinX + cx0 * Cell;
                    float z0 = _t.MinZ + cz0 * Cell;
                    float x1 = x0 + cw * Cell;
                    float z1 = z0 + cd * Cell;
                    var center = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);

                    Blocks++;
                    BlockRects.Add(new Vector4(x0, z0, x1, z1));
                    KindCount[(int)kind]++;
                    var rng = new System.Random(Hash(cx0, cz0));

                    // Charakter des Blocks: Wohlstand plus Zuschlag des Viertels.
                    float w = Wealth(center);
                    int baseRank = Mathf.Clamp(Mathf.RoundToInt(w * LastResidential) + RangBonus[(int)kind],
                                               0, LastResidential);
                    float gap = Luecke[(int)kind];

                    Frontage(new Vector2(x0, z0), new Vector2(1f, 0f), x1 - x0, new Vector2(0f, -1f), kind, baseRank, gap, rng);
                    Frontage(new Vector2(x1, z1), new Vector2(-1f, 0f), x1 - x0, new Vector2(0f, 1f), kind, baseRank, gap, rng);
                    Frontage(new Vector2(x0, z1), new Vector2(0f, -1f), z1 - z0, new Vector2(-1f, 0f), kind, baseRank, gap, rng);
                    Frontage(new Vector2(x1, z0), new Vector2(0f, 1f), z1 - z0, new Vector2(1f, 0f), kind, baseRank, gap, rng);
                }
            }
        }

        /// <summary>
        /// Setzt Haeuser an einer Blockkante entlang. start ist die Ecke,
        /// along die Laufrichtung, outward die Blickrichtung der Haeuser.
        /// </summary>
        void Frontage(Vector2 start, Vector2 along, float length, Vector2 outward,
                      Viertel kind, int baseRank, float gap, System.Random rng)
        {
            float yaw = Mathf.Atan2(outward.x, outward.y) * Mathf.Rad2Deg;
            float cursor = 2.5f;                 // Ecken frei lassen
            float usable = length - 5f;

            while (cursor < usable)
            {
                int model = PickFromBlock(kind, baseRank, rng);
                var lot = Lot(model);

                // Passt das Haus noch in den Rest der Front?
                if (cursor + lot.x > usable)
                {
                    model = SmallestFitting(usable - cursor);
                    if (model < 0) break;
                    lot = Lot(model);
                }

                Vector2 frontPoint = start + along * (cursor + lot.x * 0.5f);
                Vector2 pos = frontPoint + outward * (lot.y * 0.5f);

                if (_t.IsLand(pos) && !IsReserved(pos, lot.x * 0.4f))
                {
                    float ground = _t.SampleHeight(frontPoint);
                    _placements.Add(new RioHouseWorld.Placement
                    {
                        position = new Vector3(pos.x, ground - 0.1f, pos.y),
                        yaw = yaw,
                        model = model,
                        colorSeed = rng.Next(1, int.MaxValue)
                    });
                    Houses++;
                }

                cursor += lot.x + gap * (float)rng.NextDouble();
            }
        }

        /// <summary>
        /// Zieht aus den drei Raengen um den Blockcharakter herum.
        /// Im Centro wird oft zum Hochhaus gegriffen - ein Zentrum lebt von
        /// der Silhouette, nicht von zweistoeckigen Haeusern.
        /// </summary>
        int PickFromBlock(Viertel kind, int baseRank, System.Random rng)
        {
            if (kind == Viertel.Centro && rng.NextDouble() < 0.45)
                return rng.NextDouble() < 0.7 ? 9 : 13;     // Predio oder Cobertura

            int offset = rng.Next(3) - 1;
            if (rng.NextDouble() < 0.07) offset = rng.Next(5) - 2;   // seltener Ausreisser
            return Mathf.Clamp(baseRank + offset, FirstResidential, LastResidential);
        }

        static int SmallestFitting(float width)
        {
            int best = -1; float bestW = float.MaxValue;
            for (int m = FirstResidential; m <= LastResidential; m++)
            {
                float w = RioHausDaten.Haeuser[m].lotBreite;
                if (w <= width && w < bestW) { best = m; bestW = w; }
            }
            return best;
        }

        // ------------------------------------------------------------------
        // Favelas
        // ------------------------------------------------------------------

        /// <summary>
        /// An den Haengen gibt es kein Raster. Huetten werden dicht gestreut,
        /// mit Mindestabstand statt Gitter, und stehen schief zueinander.
        /// </summary>
        public void PlaceFavelas(System.Random rng)
        {
            const float cell = 60f;
            var taken = new List<Vector2>();

            for (float x = _t.MinX; x < _t.MaxX; x += cell)
            for (float z = _t.MinZ; z < _t.MaxZ; z += cell)
            {
                var center = new Vector2(x + cell * 0.5f, z + cell * 0.5f);
                if (!_t.IsLand(center) || _t.IsInLake(center)) continue;

                float slope = _t.SlopeAt(center);
                if (slope < 0.14f || slope > 0.8f) continue;

                float h = _t.SampleHeight(center);
                if (h < 6f || h > 200f) continue;
                if (NearPlace(center, 55f)) continue;
                if (_t.DistanceToCoast(center) < 25f) continue;

                taken.Clear();
                int tries = 46;
                for (int i = 0; i < tries; i++)
                {
                    var p = center + new Vector2(
                        ((float)rng.NextDouble() - 0.5f) * cell,
                        ((float)rng.NextDouble() - 0.5f) * cell);

                    bool clash = false;
                    foreach (var q in taken)
                        if (Vector2.Distance(p, q) < 7.5f) { clash = true; break; }
                    if (clash) continue;
                    if (!_t.IsLand(p) || IsReserved(p, 5f)) continue;

                    taken.Add(p);
                    int model = rng.Next(0, 5);         // nur die aermsten fuenf
                    float ground = _t.SampleHeight(p) - 1.2f;

                    _placements.Add(new RioHouseWorld.Placement
                    {
                        position = new Vector3(p.x, ground, p.y),
                        yaw = (float)rng.NextDouble() * 360f,
                        model = model,
                        colorSeed = rng.Next(1, int.MaxValue)
                    });
                    Shacks++;
                }
            }
        }

        // ------------------------------------------------------------------
        // Sperrflaechen und Einzelstuecke
        // ------------------------------------------------------------------

        bool IsReserved(Vector2 p, float radius)
        {
            for (int i = 0; i < _reserved.Count; i++)
                if (Vector2.Distance(p, _reserved[i]) < _reservedRadius[i] + radius) return true;
            return false;
        }

        public void Reserve(Vector2 p, float radius)
        {
            _reserved.Add(p);
            _reservedRadius.Add(radius);
        }

        public static Vector2 Lot(int model) =>
            new Vector2(RioHausDaten.Haeuser[model].lotBreite, RioHausDaten.Haeuser[model].lotTiefe);

        public static float Height(int model) => ModelHeight[model];

        public void PlaceVillas(System.Random rng)
        {
            int[] villas = { VillaJoatinga, VillaLeblon };
            const float minApart = 900f;

            foreach (int model in villas)
            {
                var lot = Lot(model);
                float radius = Mathf.Max(lot.x, lot.y) * 0.5f + 10f;

                Vector2 best = Vector2.zero;
                float bestScore = float.MinValue;

                for (float x = _t.MinX; x < _t.MaxX; x += 40f)
                for (float z = _t.MinZ; z < _t.MaxZ; z += 40f)
                {
                    var p = new Vector2(x, z);
                    if (!_t.IsLand(p) || _t.IsInLake(p)) continue;
                    if (_t.DistanceToCoast(p) < radius + 15f) continue;
                    if (_t.SlopeAt(p) > 0.10f) continue;
                    float h = _t.SampleHeight(p);
                    if (h < 4f || h > 90f) continue;
                    if (NearPlace(p, 150f) || IsReserved(p, radius)) continue;
                    if (TooCloseToVilla(p, minApart)) continue;

                    float score = Wealth(p) + (1f - Mathf.Clamp01(_t.DistanceToCoast(p) / 400f)) * 0.3f;
                    if (score > bestScore) { bestScore = score; best = p; }
                }

                if (bestScore == float.MinValue) continue;

                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(best.x, _t.SampleHeight(best) - 0.2f, best.y),
                    yaw = YawTowardsWater(best),
                    model = model,
                    colorSeed = rng.Next(1, int.MaxValue)
                });
                Reserve(best, radius + 20f);
                _villaSpots.Add(best);
            }
        }

        bool TooCloseToVilla(Vector2 p, float minApart)
        {
            foreach (var v in _villaSpots)
                if (Vector2.Distance(p, v) < minApart) return true;
            return false;
        }

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

        public void PlaceSpecials(System.Random rng)
        {
            foreach (var place in RioWorldLayout.Places)
            {
                if (place.Kind != RioWorldLayout.Poi.BankGross &&
                    place.Kind != RioWorldLayout.Poi.BankKlein) continue;

                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(place.Position.x, _t.SampleHeight(place.Position) - 0.2f, place.Position.y),
                    yaw = (Hash((int)place.Position.x, (int)place.Position.y) & 3) * 90f,
                    model = ModelBank,
                    colorSeed = rng.Next(1, int.MaxValue)
                });
                Reserve(place.Position, 60f);
            }

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

                        _placements.Add(new RioHouseWorld.Placement
                        {
                            position = new Vector3(cand.x, _t.SampleHeight(cand) - 0.2f, cand.y),
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

        public static int Hash(int x, int z)
        {
            unchecked
            {
                int h = x * 73856093 ^ z * 19349663 ^ 0x5f3a2b1;
                h ^= h >> 13; h *= 1274126177; h ^= h >> 16;
                return h & 0x7fffffff;
            }
        }

        public int[] CountPerModel()
        {
            var counts = new int[RioModelCatalog.Count];
            foreach (var p in _placements) counts[p.model]++;
            return counts;
        }
    }
}
