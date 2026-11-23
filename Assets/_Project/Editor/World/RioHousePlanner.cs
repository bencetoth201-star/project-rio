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
        // Gebaeude und Masse kommen aus RioModelCatalog, erzeugt aus den
        // JSON-Dateien der Pakete. Hier stehen nur noch die Rollen.
        public const int FirstResidential = 0;
        public const int LastResidential = 14;   // 01..15, die Wohnhaus-Leiter
        public const int ModelTankstelle = RioModelCatalog.Tankstelle;
        public const int VillaLeblon = RioModelCatalog.VillaLeblon;
        public const int VillaJoatinga = RioModelCatalog.VillaJoatinga;
        public const int ModelBank = RioModelCatalog.Bank;

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

        /// <summary>
        /// Periodenlaenge in X und Z je Viertel. Block = (p-1) Kacheln.
        /// Die Werte muessen Teiler von 12 sein, sonst enden Strassen an der
        /// Viertelgrenze im Nichts.
        /// </summary>
        static readonly int[,] Raster =
        {
            { 6, 6 },    // Centro:    60 x 60 m, Platz fuer die Tuerme
            { 12, 6 },   // Orla:     132 x 60 m, lange Zeilen
            { 4, 3 },    // Bairro:    36 x 24 m, der Normalfall
            { 6, 4 },    // Nobre:     60 x 36 m, grosszuegig
            { 12, 6 },   // Industrie:132 x 60 m, grosse Hallen
        };

        /// <summary>Luecke zwischen den Haeusern je Viertel.</summary>
        static readonly float[] Luecke = { 0.4f, 0.6f, 0.5f, 3.5f, 4f };

        // Modellindizes, der Lesbarkeit halber benannt
        const int Barraco = 0, Palafita = 1, Puxadinho = 2, FavelaReihe = 3, Cortico = 4;
        const int CasaRio = 5, Boteco = 6, Sobrado = 7, Mercadinho = 8, Predio = 9;
        const int ZonaOeste = 10, VillaZonaSul = 11, CasaBarra = 12, Cobertura = 13, CasaJoa = 14;

        /// <summary>
        /// Was in welchem Viertel gebaut werden darf. Frueher lief alles ueber
        /// eine gemeinsame Rangleiter - dadurch stand eine Favela-Huette auch
        /// mal neben einer Villa. Jetzt hat jedes Viertel seine eigene Liste,
        /// und Huetten gibt es nur noch am Hang.
        /// </summary>
        static readonly int[][] Palette =
        {
            new[] { RioModelCatalog.TorreCentro, RioModelCatalog.Edificio70, Predio, Cobertura },
            new[] { Predio, Cobertura, RioModelCatalog.Edificio70, ZonaOeste, Predio },
            new[] { CasaRio, Boteco, Sobrado, Mercadinho, Predio, CasaRio, Sobrado },
            new[] { VillaZonaSul, CasaBarra, CasaJoa, Cobertura, ZonaOeste },
            new[] { RioModelCatalog.Lagerhalle, Mercadinho, Cortico, Predio },
        };

        /// <summary>Eckgebaeude je Viertel, -1 wenn keines passt.</summary>
        static readonly int[] EckModell =
        {
            RioModelCatalog.EckPredio, RioModelCatalog.EckPredio,
            RioModelCatalog.EckSobrado, -1, -1
        };

        static readonly int[] FavelaPalette = { Barraco, Palafita, Puxadinho, FavelaReihe, Cortico };

        // ------------------------------------------------------------------
        // Belegungsraster
        //
        // Jede gesetzte Kachel, jedes Haus und jedes Wahrzeichen markiert hier
        // seine Grundflaeche. Vorher gab es nur eine Liste mit Kreisradien, und
        // die Haeuser an den Blockraendern haben sich ueberhaupt nicht
        // eingetragen - deshalb standen Haeuser ineinander und auf der Strasse.
        // ------------------------------------------------------------------

        const float OccCell = 2f;
        bool[] _occ;
        int _ow, _oh;

        public void InitOccupancy()
        {
            _ow = Mathf.CeilToInt((_t.MaxX - _t.MinX) / OccCell) + 2;
            _oh = Mathf.CeilToInt((_t.MaxZ - _t.MinZ) / OccCell) + 2;
            _occ = new bool[_ow * _oh];
        }

        /// <summary>Achsparallele Huelle des gedrehten Rechtecks, in Rasterzellen.</summary>
        void FootprintCells(Vector2 center, Vector2 size, float yaw,
                            out int x0, out int z0, out int x1, out int z1)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Abs(Mathf.Cos(rad)), si = Mathf.Abs(Mathf.Sin(rad));
            float ex = (size.x * c + size.y * si) * 0.5f;
            float ez = (size.x * si + size.y * c) * 0.5f;

            // Halboffenes Intervall: die obere Kante gehoert zur naechsten
            // Zelle. Mit Abrunden auf beiden Seiten sperrte jede Flaeche rundum
            // eine Zelle zu viel - bei 24 m tiefen Blocks blieb dann fast nichts
            // mehr uebrig, und es wurden statt 80.000 nur 160 Haeuser gesetzt.
            x0 = Mathf.FloorToInt((center.x - ex - _t.MinX) / OccCell);
            x1 = Mathf.CeilToInt((center.x + ex - _t.MinX) / OccCell) - 1;
            z0 = Mathf.FloorToInt((center.y - ez - _t.MinZ) / OccCell);
            z1 = Mathf.CeilToInt((center.y + ez - _t.MinZ) / OccCell) - 1;
            if (x1 < x0) x1 = x0;
            if (z1 < z0) z1 = z0;
        }

        public bool Free(Vector2 center, Vector2 size, float yaw)
        {
            FootprintCells(center, size, yaw, out int x0, out int z0, out int x1, out int z1);
            if (x0 < 0 || z0 < 0 || x1 >= _ow || z1 >= _oh) return false;
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                if (_occ[z * _ow + x]) return false;
            return true;
        }

        public void Occupy(Vector2 center, Vector2 size, float yaw)
        {
            FootprintCells(center, size, yaw, out int x0, out int z0, out int x1, out int z1);
            x0 = Mathf.Max(0, x0); z0 = Mathf.Max(0, z0);
            x1 = Mathf.Min(_ow - 1, x1); z1 = Mathf.Min(_oh - 1, z1);
            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                _occ[z * _ow + x] = true;
        }

        /// <summary>Setzt ein Modell, wenn der Platz frei ist. Liefert Erfolg.</summary>
        bool TryPlace(int model, Vector2 pos, float yaw, float groundAt, System.Random rng)
        {
            var lot = Lot(model);
            if (!Free(pos, lot, yaw)) return false;
            Occupy(pos, lot, yaw);
            _placements.Add(new RioHouseWorld.Placement
            {
                position = new Vector3(pos.x, groundAt, pos.y),
                yaw = yaw,
                model = model,
                colorSeed = rng.Next(1, int.MaxValue)
            });
            return true;
        }

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
        public int Freiflaechen { get; private set; }

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

        // Zugriffe fuer den Planexport
        public int GridW => _gw;
        public int GridH => _gh;
        public int SuperW => _sw;
        public int SuperH => _sh;
        public bool CityAt(int gx, int gz) => IsCity(gx, gz);
        public Viertel KindOfSuper(int sx, int sz) =>
            _kind[Mathf.Clamp(sz, 0, _sh - 1) * _sw + Mathf.Clamp(sx, 0, _sw - 1)];

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

                var lot = Lot(model);
                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(p.x, _t.SampleHeight(p) + 0.05f, p.y),
                    yaw = yaw,
                    model = model,
                    colorSeed = seed
                });
                // Die Kachel sperrt ihre Flaeche. Danach kann kein Haus mehr
                // auf der Strasse landen, egal was sonst schiefgeht.
                Occupy(p, lot, yaw);
                RoadTiles++;
            }
        }

        // ------------------------------------------------------------------
        // Haeuser an den Blockraendern
        // ------------------------------------------------------------------

        public void PlaceBlocks(System.Random rng)
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
                    KindCount[(int)kind]++;
                    var brng = new System.Random(Hash(cx0, cz0));

                    // Jeder zwoelfte Block bleibt offen: Platz oder Park.
                    if (kind != Viertel.Industrie && brng.NextDouble() < 0.085 &&
                        (x1 - x0) >= 38f && (z1 - z0) >= 38f)
                    {
                        int frei = brng.NextDouble() < 0.5
                                 ? RioModelCatalog.Praca : RioModelCatalog.Parque;
                        if (TryPlace(frei, center, 0f, _t.SampleHeight(center) - 0.1f, brng))
                        {
                            Freiflaechen++;
                            continue;
                        }
                    }

                    PlaceCorners(kind, x0, z0, x1, z1, brng);

                    float gap = Luecke[(int)kind];
                    Frontage(new Vector2(x0, z0), new Vector2(1f, 0f), x1 - x0, new Vector2(0f, -1f), kind, gap, brng);
                    Frontage(new Vector2(x1, z1), new Vector2(-1f, 0f), x1 - x0, new Vector2(0f, 1f), kind, gap, brng);
                    Frontage(new Vector2(x0, z1), new Vector2(0f, -1f), z1 - z0, new Vector2(-1f, 0f), kind, gap, brng);
                    Frontage(new Vector2(x1, z0), new Vector2(0f, 1f), z1 - z0, new Vector2(1f, 0f), kind, gap, brng);
                }
            }
        }

        /// <summary>
        /// Eckgrundstuecke zuerst. Eckgebaeude sind dafuer gebaut, nach zwei
        /// Seiten zu zeigen - stellt man dort ein normales Haus hin, schaut
        /// immer eine fensterlose Rueckwand auf die Strasse.
        /// </summary>
        void PlaceCorners(Viertel kind, float x0, float z0, float x1, float z1, System.Random rng)
        {
            int model = EckModell[(int)kind];
            if (model < 0) return;

            var lot = Lot(model);
            float hx = lot.x * 0.5f, hz = lot.y * 0.5f;

            var corners = new[]
            {
                (new Vector2(x0 + hx, z0 + hz), 180f),
                (new Vector2(x1 - hx, z0 + hz), 180f),
                (new Vector2(x0 + hx, z1 - hz),   0f),
                (new Vector2(x1 - hx, z1 - hz),   0f),
            };

            foreach (var (pos, yaw) in corners)
            {
                if (!_t.IsLand(pos)) continue;
                if (TryPlace(model, pos, yaw, _t.SampleHeight(pos) - 0.1f, rng)) Houses++;
            }
        }

        /// <summary>
        /// Setzt Haeuser an einer Blockkante entlang. start ist die Ecke,
        /// along die Laufrichtung, outward die Blickrichtung der Haeuser.
        ///
        /// Wichtig: Der Pivot der Modelle sitzt in der Mitte des Grundstuecks,
        /// die Vorderseite liegt an der +Z-Kante. Das Haus muss also vom
        /// Strassenrand aus NACH INNEN gerueckt werden. Vorher stand hier ein
        /// Plus statt eines Minus - dadurch ragte jedes Haus mit seiner halben
        /// Tiefe in die Fahrbahn.
        /// </summary>
        void Frontage(Vector2 start, Vector2 along, float length, Vector2 outward,
                      Viertel kind, float gap, System.Random rng)
        {
            float yaw = Mathf.Atan2(outward.x, outward.y) * Mathf.Rad2Deg;
            var pal = Palette[(int)kind];
            float cursor = 1.5f;
            float usable = length - 1.5f;

            while (cursor < usable - 5f)
            {
                float rest = usable - cursor;
                int model = Choose(pal, rest, rng);
                if (model < 0) break;

                var lot = Lot(model);
                Vector2 frontPoint = start + along * (cursor + lot.x * 0.5f);
                Vector2 pos = frontPoint - outward * (lot.y * 0.5f);

                bool gesetzt = false;
                if (_t.IsLand(pos))
                    gesetzt = TryPlace(model, pos, yaw, _t.SampleHeight(frontPoint) - 0.1f, rng);

                if (gesetzt)
                {
                    Houses++;
                    cursor += lot.x + gap * (float)rng.NextDouble();
                }
                else
                {
                    // Platz belegt oder kein Land: ein Stueck weiterruecken
                    cursor += 4f;
                }
            }
        }

        /// <summary>Zufaelliges Modell der Palette, das noch in die Restbreite passt.</summary>
        static int Choose(int[] pal, float maxWidth, System.Random rng)
        {
            int best = -1;
            int treffer = 0;
            for (int i = 0; i < pal.Length; i++)
            {
                if (RioModelCatalog.Get(pal[i]).lot.x > maxWidth) continue;
                treffer++;
                if (rng.Next(treffer) == 0) best = pal[i];   // gleichverteilt ziehen
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
            const float cell = 48f;

            for (float x = _t.MinX; x < _t.MaxX; x += cell)
            for (float z = _t.MinZ; z < _t.MaxZ; z += cell)
            {
                var center = new Vector2(x + cell * 0.5f, z + cell * 0.5f);
                if (!_t.IsLand(center) || _t.IsInLake(center)) continue;

                float slope = _t.SlopeAt(center);
                if (slope < 0.16f || slope > 0.85f) continue;      // nur echte Haenge

                float h = _t.SampleHeight(center);
                if (h < 8f || h > 210f) continue;
                if (NearPlace(center, 70f)) continue;
                if (_t.DistanceToCoast(center) < 25f) continue;

                // Nicht direkt neben die Villen. In echt grenzen Favelas zwar
                // an reiche Viertel, aber eine Huette im Vorgarten der Mansion
                // ist trotzdem Unsinn.
                if (TooCloseToVilla(center, 260f)) continue;

                // Dicht packen: der Mindestabstand ergibt sich aus den
                // Grundflaechen im Belegungsraster, nicht aus einem Radius.
                for (int i = 0; i < 70; i++)
                {
                    var p2 = center + new Vector2(
                        ((float)rng.NextDouble() - 0.5f) * cell,
                        ((float)rng.NextDouble() - 0.5f) * cell);
                    if (!_t.IsLand(p2)) continue;

                    int model = FavelaPalette[rng.Next(FavelaPalette.Length)];
                    float yaw = (float)rng.NextDouble() * 360f;
                    if (TryPlace(model, p2, yaw, _t.SampleHeight(p2) - 1.1f, rng)) Shacks++;
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

        public static Vector2 Lot(int model) => RioModelCatalog.Get(model).lot;

        public static float Height(int model) => RioModelCatalog.Get(model).height;

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
                    if (!Free(p, lot + new Vector2(16f, 16f), 0f)) continue;

                    float score = Wealth(p) + (1f - Mathf.Clamp01(_t.DistanceToCoast(p) / 400f)) * 0.3f;
                    if (score > bestScore) { bestScore = score; best = p; }
                }

                if (bestScore == float.MinValue) continue;

                float vYaw = YawTowardsWater(best);
                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(best.x, _t.SampleHeight(best) - 0.2f, best.y),
                    yaw = vYaw,
                    model = model,
                    colorSeed = rng.Next(1, int.MaxValue)
                });
                Occupy(best, lot + new Vector2(16f, 16f), vYaw);   // Grundstueck mit Garten
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

                float bYaw = (Hash((int)place.Position.x, (int)place.Position.y) & 3) * 90f;
                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(place.Position.x, _t.SampleHeight(place.Position) - 0.2f, place.Position.y),
                    yaw = bYaw,
                    model = ModelBank,
                    colorSeed = rng.Next(1, int.MaxValue)
                });
                Occupy(place.Position, Lot(ModelBank) + new Vector2(12f, 12f), bYaw);
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

                        float tYaw = Mathf.Atan2(-side.x, -side.y) * Mathf.Rad2Deg;
                        if (!Free(cand, Lot(ModelTankstelle), tYaw)) continue;
                        _placements.Add(new RioHouseWorld.Placement
                        {
                            position = new Vector3(cand.x, _t.SampleHeight(cand) - 0.2f, cand.y),
                            yaw = tYaw,
                            model = ModelTankstelle,
                            colorSeed = rng.Next(1, int.MaxValue)
                        });
                        Occupy(cand, Lot(ModelTankstelle), tYaw);
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
