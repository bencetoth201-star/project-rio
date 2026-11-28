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

        public enum Viertel { Centro, Orla, Bairro, Nobre, Industrie, Favela }

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
            { 4, 3 },    // Favela:    36 x 24 m, eng
        };

        /// <summary>Luecke zwischen den Haeusern je Viertel.</summary>
        static readonly float[] Luecke = { 0.4f, 0.6f, 0.5f, 3.5f, 4f, 0.1f };

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
            // Gang-Viertel: nur die armen Haeuser, dicht an dicht
            new[] { Barraco, Puxadinho, FavelaReihe, Cortico, Barraco, Puxadinho, Boteco },
        };

        /// <summary>Eckgebaeude je Viertel, -1 wenn keines passt.</summary>
        static readonly int[] EckModell =
        {
            RioModelCatalog.EckPredio, RioModelCatalog.EckPredio,
            RioModelCatalog.EckSobrado, -1, -1, -1
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

        /// <summary>
        /// Wie weit die Pruefung hinter die eigene Kante zurueckweicht.
        /// Etwas mehr als eine halbe Rasterzelle, damit eine Zelle, deren
        /// Mittelpunkt genau auf der gemeinsamen Kante liegt, keine
        /// Kollision zwischen zwei aneinanderstossenden Kacheln meldet.
        /// </summary>
        const float Einzug = 1.1f;

        bool[] _occ;
        int _ow, _oh;

        public void InitOccupancy()
        {
            _ow = Mathf.CeilToInt((_t.MaxX - _t.MinX) / OccCell) + 2;
            _oh = Mathf.CeilToInt((_t.MaxZ - _t.MinZ) / OccCell) + 2;
            _occ = new bool[_ow * _oh];
        }

        /// <summary>
        /// Laeuft ueber genau die Zellen, deren Mittelpunkt im gedrehten
        /// Rechteck liegt.
        ///
        /// Vorher wurde die achsparallele Huelle genommen. Bei einem um 26 Grad
        /// gedrehten Viertel ist die Huelle einer 12-m-Kachel aber 16 m breit -
        /// jede Strasse sperrte damit 2 m in die Nachbarblocks hinein, und es
        /// passten kaum noch Haeuser hinein.
        /// </summary>
        bool ForEachCell(Vector2 center, Vector2 size, float yaw, bool markieren)
        {
            // Welt -> lokal mit Unitys Drehsinn (yaw im Uhrzeigersinn). Hier
            // stand vorher -yaw: das Rechteck lag dann gespiegelt gedreht, in
            // einem um 22 Grad gedrehten Viertel um 44 Grad daneben - Haeuser
            // konnten an den Ecken in die Strasse ragen.
            float rad = yaw * Mathf.Deg2Rad;
            float cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            float hx = size.x * 0.5f, hz = size.y * 0.5f;

            // Beim Pruefen ein Stueck nach innen gehen. Zwei Kacheln, die
            // aneinanderstossen, teilen sich die Zellen auf ihrer gemeinsamen
            // Kante; ohne den Einzug hielt jede die Kante der anderen fuer
            // belegt. Im Avenida-Router wurden dadurch 879 von 922 Kacheln zur
            // Kreuzung, weil der Strang sich selbst blockierte.
            if (!markieren) { hx -= Einzug; hz -= Einzug; }
            if (hx <= 0f || hz <= 0f) return true;

            // achsparallele Huelle, nur um den Suchbereich einzugrenzen
            float ex = Mathf.Abs(size.x * cs) * 0.5f + Mathf.Abs(size.y * sn) * 0.5f;
            float ez = Mathf.Abs(size.x * sn) * 0.5f + Mathf.Abs(size.y * cs) * 0.5f;

            int x0 = Mathf.FloorToInt((center.x - ex - _t.MinX) / OccCell);
            int x1 = Mathf.CeilToInt((center.x + ex - _t.MinX) / OccCell) - 1;
            int z0 = Mathf.FloorToInt((center.y - ez - _t.MinZ) / OccCell);
            int z1 = Mathf.CeilToInt((center.y + ez - _t.MinZ) / OccCell) - 1;

            if (!markieren && (x0 < 0 || z0 < 0 || x1 >= _ow || z1 >= _oh)) return false;
            x0 = Mathf.Max(0, x0); z0 = Mathf.Max(0, z0);
            x1 = Mathf.Min(_ow - 1, x1); z1 = Mathf.Min(_oh - 1, z1);

            for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
            {
                // Zellmittelpunkt in das System des Rechtecks drehen
                float wx = _t.MinX + (x + 0.5f) * OccCell - center.x;
                float wz = _t.MinZ + (z + 0.5f) * OccCell - center.y;
                float lx = wx * cs - wz * sn;
                float lz = wx * sn + wz * cs;
                if (Mathf.Abs(lx) > hx || Mathf.Abs(lz) > hz) continue;

                int i = z * _ow + x;
                if (markieren) _occ[i] = true;
                else if (_occ[i]) return false;
            }
            return true;
        }

        public bool Free(Vector2 center, Vector2 size, float yaw) =>
            ForEachCell(center, size, yaw, false);

        public void Occupy(Vector2 center, Vector2 size, float yaw) =>
            ForEachCell(center, size, yaw, true);

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

        /// <summary>
        /// Dieselbe Liste veraenderbar, fuer die Nachbearbeitung durch
        /// <see cref="RioStrassennetz"/>. Hoehen und Kachelmodelle lassen sich
        /// erst festlegen, wenn das ganze Netz steht - vorher weiss niemand,
        /// welche Nachbarn eine Kachel bekommt.
        /// </summary>
        internal List<RioHouseWorld.Placement> PlacementList => _placements;
        public int Count => _placements.Count;
        public int RoadTiles { get; private set; }
        public int Houses { get; private set; }
        public int Shacks { get; private set; }
        public int Blocks { get; private set; }
        public int Avenidas { get; private set; }
        public int Freiflaechen { get; private set; }

        /// <summary>Innenflaechen der Blocks (x0, z0, x1, z1) fuer die Hoefe.</summary>
        public readonly List<Vector4> BlockRects = new List<Vector4>();

        /// <summary>Wie viele Blocks je Viertelstyp entstanden sind.</summary>
        public readonly int[] KindCount = new int[6];

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
                if (RioWahrzeichen.Belegt(p, Cell)) continue;   // Flughafen, Stadion, Hafen ...

                // Stadt nur in den gruenen Umrissen der Skizze und in den
                // Gang-Vierteln - nicht mehr verstreut ueber beide Inseln
                if (!RioWorldLayout.InStadt(p) && !RioDistricts.InFavela(p)) continue;

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
        // Stadt: ein Viertel nach dem anderen
        //
        // Jedes Viertel hat sein eigenes, gedrehtes Raster. Innerhalb eines
        // Viertels passen alle Kacheln zusammen, weil alles im selben
        // gedrehten System liegt. Zwischen zwei Vierteln bleibt Land frei -
        // vorher lag ueber beide Inseln eine einzige luckenlose Decke.
        // ------------------------------------------------------------------

        /// <summary>Taugt das Gelaende an dieser Stelle fuer Stadt?</summary>
        bool TerrainOk(Vector2 w) => IstStadt(w);

        /// <summary>Darf an dieser Stelle ueberhaupt Stadt stehen?</summary>
        public bool IstStadt(Vector2 w)
        {
            int gx = Mathf.FloorToInt((w.x - _t.MinX) / Cell);
            int gz = Mathf.FloorToInt((w.y - _t.MinZ) / Cell);
            return IsCity(gx, gz);
        }

        /// <summary>
        /// Verbindet die Viertel mit Avenidas.
        ///
        /// Gelaufen wird gierig: in jedem Schritt die Richtung zum Ziel, und
        /// wenn dort Wasser oder Steilhang liegt, bis zu 60 Grad ausweichen.
        /// Das reicht, um Kuestenlinien zu umgehen, ohne einen vollen
        /// Wegfinder zu brauchen.
        /// </summary>
        public void PlaceAvenidas(System.Random rng)
        {
            int n = RioDistricts.Verbindungen.GetLength(0);
            for (int e = 0; e < n; e++)
            {
                var a = RioDistricts.All[RioDistricts.Verbindungen[e, 0]];
                var b = RioDistricts.All[RioDistricts.Verbindungen[e, 1]];
                Route(a.Center, b.Center);
            }
        }

        void Route(Vector2 from, Vector2 to)
        {
            const float step = 24f;      // Kantenlaenge der Avenida-Kachel
            Vector2 p = from;
            int guard = 0;

            while (Vector2.Distance(p, to) > step * 1.5f && guard++ < 900)
            {
                Vector2 soll = (to - p).normalized;
                Vector2 gewaehlt = Vector2.zero;
                bool gefunden = false;

                // Direkt, dann in wachsenden Winkeln ausweichen
                foreach (float abw in new[] { 0f, 20f, -20f, 40f, -40f, 60f, -60f })
                {
                    float rad = abw * Mathf.Deg2Rad;
                    var dir = new Vector2(
                        soll.x * Mathf.Cos(rad) - soll.y * Mathf.Sin(rad),
                        soll.x * Mathf.Sin(rad) + soll.y * Mathf.Cos(rad));

                    Vector2 next = p + dir * step;
                    if (!_t.IsLand(next)) continue;
                    if (_t.DistanceToCoast(next) < 18f) continue;
                    if (_t.SlopeAt(next) > 0.26f) continue;

                    gewaehlt = dir; gefunden = true; break;
                }

                if (!gefunden) break;      // Sackgasse, Rest der Strecke entfaellt

                p += gewaehlt * step;
                float yaw = Mathf.Atan2(gewaehlt.x, gewaehlt.y) * Mathf.Rad2Deg;

                // Avenidas laufen vor der Stadt und haben Vorrang. Wenn hier
                // doch etwas liegt, ist es eine andere Avenida - dann gibt es
                // eine Kreuzung statt einer Luecke.
                int model = Free(p, Lot(RioModelCatalog.AvenidaStraight), yaw)
                          ? RioModelCatalog.AvenidaStraight
                          : RioModelCatalog.AvenidaCross;
                var lot = Lot(model);
                Occupy(p, lot, yaw);

                _placements.Add(new RioHouseWorld.Placement
                {
                    position = new Vector3(p.x, _t.SampleHeight(p) + 0.05f, p.y),
                    yaw = yaw,
                    model = model,
                    colorSeed = Hash((int)p.x, (int)p.y)
                });
                Avenidas++;
            }
        }

        public void PlaceCity(System.Random rng)
        {
            for (int di = 0; di < RioDistricts.All.Length; di++)
                BuildDistrict(di, rng);
        }

        void BuildDistrict(int di, System.Random outerRng)
        {
            var d = RioDistricts.All[di];
            RioDistricts.Raster(d.Type, out int px, out int pz);

            int nx = Mathf.CeilToInt(d.A / Cell) + 2;
            int nz = Mathf.CeilToInt(d.B / Cell) + 2;
            int w = nx * 2 + 1, h = nz * 2 + 1;

            // 1. Wo ist ueberhaupt Platz?
            var valid = new bool[w * h];
            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                var local = new Vector2((ix - nx) * Cell, (iz - nz) * Cell);
                var world = RioDistricts.ToWorld(d, local);
                if (RioDistricts.Norm(d, world) > 1f) continue;
                if (!TerrainOk(world)) continue;
                if (RioDistricts.IndexAt(world) != di) continue;
                valid[iz * w + ix] = true;
            }

            // 2. Strassenzellen: unregelmaessige Linien (RioDistricts.IstStrasse),
            //    plus Ring am Rand
            var street = new bool[w * h];
            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                if (!valid[iz * w + ix]) continue;

                bool linie = RioDistricts.IstStrasse(di, ix - nx, iz - nz);

                // Randzelle: hat einen ungueltigen Nachbarn. Daraus wird die
                // Ringstrasse - ohne sie endet jede Querstrasse im Feld.
                bool rand = !Get(valid, w, h, ix + 1, iz) || !Get(valid, w, h, ix - 1, iz)
                         || !Get(valid, w, h, ix, iz + 1) || !Get(valid, w, h, ix, iz - 1);

                street[iz * w + ix] = linie || rand;
            }
            RioDistricts.StummelStreichen(street, w, h, RioStrassenplan.MinNetz);

            // Die Strassenkacheln setzt RioStrassenplan. Hier standen sie
            // frueher auch, mit einem Free()-Test davor - und jede Kachel, die
            // der Test ablehnte, riss ein Loch in eine Strasse. Der Graph
            // entscheidet das jetzt vorher, und zwar fuer das ganze Netz.

            // 3. Blocks = zusammenhaengende Flaechen ohne Strasse. Weil
            //    Strassenstuecke wegfallen, sind das keine Rechtecke mehr,
            //    sondern auch L- und T-foermige, verwinkelte Flaechen.
            var kind = (Viertel)(int)d.Type;
            var besucht = new bool[w * h];
            var zellen = new List<int>();
            var stapel = new Stack<int>();

            for (int start = 0; start < w * h; start++)
            {
                if (besucht[start] || !valid[start] || street[start]) continue;

                zellen.Clear();
                stapel.Push(start);
                besucht[start] = true;
                while (stapel.Count > 0)
                {
                    int c = stapel.Pop();
                    zellen.Add(c);
                    int cx = c % w, cz = c / w;
                    foreach (var (ox, oz) in Nachbarn)
                    {
                        int x = cx + ox, z = cz + oz;
                        if (x < 0 || z < 0 || x >= w || z >= h) continue;
                        int n = z * w + x;
                        if (besucht[n] || !valid[n] || street[n]) continue;
                        besucht[n] = true;
                        stapel.Push(n);
                    }
                }

                Blocks++;
                KindCount[(int)kind]++;
                var brng = new System.Random(Hash(start + di * 613, zellen.Count));
                BlockBebauen(d, kind, zellen, street, valid, w, h, nx, nz, brng);
            }
        }

        static readonly (int, int)[] Nachbarn = { (1, 0), (-1, 0), (0, 1), (0, -1) };

        static bool Get(bool[] a, int w, int h, int x, int z) =>
            x >= 0 && z >= 0 && x < w && z < h && a[z * w + x];

        // Je Viertelstyp: Schraeglage der Haeuser (Grad), groesster Abstand zur
        // Strasse (m), Versuche je Zelle fuer Hinterhaeuser
        static readonly float[] Schraeg = { 3f, 4f, 9f, 6f, 3f, 20f };
        static readonly float[] Rueckversatz = { 0.6f, 1.2f, 2.8f, 4f, 2f, 1.5f };
        static readonly float[] Hinterhaus = { 0.4f, 0.5f, 1.3f, 0.4f, 0.3f, 2.5f };

        /// <summary>
        /// Bebaut einen Block organisch statt in Reihe: An jeder Strassenkante
        /// wird ein Haus versucht - mit zufaelligem Versatz entlang der Kante,
        /// eigenem Abstand zur Strasse und leicht schraeg. Die Kanten kommen in
        /// zufaelliger Reihenfolge dran, deshalb entstehen ungleiche Luecken
        /// und keine sauberen Zeilen. Danach fuellen Hinterhaeuser die Mitte.
        /// Ueberschneidungen verhindert das Belegungsraster.
        /// </summary>
        void BlockBebauen(in RioDistricts.District d, Viertel kind, List<int> zellen,
                          bool[] street, bool[] valid, int w, int h, int nx, int nz,
                          System.Random rng)
        {
            var pal = Palette[(int)kind];
            int k = (int)kind;

            // Ab und zu ein Platz oder Park statt Haeusern
            if (kind != Viertel.Industrie && kind != Viertel.Favela && zellen.Count >= 12 && rng.NextDouble() < 0.07)
            {
                int mitte = zellen[zellen.Count / 2];
                var mw = RioDistricts.ToWorld(d, Lokal(mitte, w, nx, nz));
                int frei = rng.NextDouble() < 0.5 ? RioModelCatalog.Praca : RioModelCatalog.Parque;
                if (TryPlace(frei, mw, d.Rotation + rng.Next(4) * 90f, _t.SampleHeight(mw) - 0.1f, rng))
                { Freiflaechen++; return; }
            }

            // Strassenkanten sammeln: (Zelle, Richtung zur Strasse)
            var kanten = new List<(int zelle, Vector2 raus)>();
            foreach (int c in zellen)
            {
                int cx = c % w, cz = c / w;
                foreach (var (ox, oz) in Nachbarn)
                    if (Get(street, w, h, cx + ox, cz + oz)) kanten.Add((c, new Vector2(ox, oz)));
            }
            Mischen(kanten, rng);

            for (int runde = 0; runde < 2; runde++)
            foreach (var (c, raus) in kanten)
            {
                int model = Choose(pal, 40f, rng);
                if (model < 0) continue;
                var lot = Lot(model);

                Vector2 entlang = new Vector2(-raus.y, raus.x);
                Vector2 kante = Lokal(c, w, nx, nz) + raus * (Cell * 0.5f);
                Vector2 vorne = kante + entlang * (((float)rng.NextDouble() - 0.5f) * Cell);
                float abstand = (float)rng.NextDouble() * Rueckversatz[k];
                Vector2 posLokal = vorne - raus * (lot.y * 0.5f + abstand + 0.3f);

                float yaw = Mathf.Atan2(raus.x, raus.y) * Mathf.Rad2Deg + d.Rotation
                          + ((float)rng.NextDouble() * 2f - 1f) * Schraeg[k];

                Vector2 pos = RioDistricts.ToWorld(d, posLokal);
                Vector2 front = RioDistricts.ToWorld(d, vorne);
                if (_t.IsLand(pos) && TryPlace(model, pos, yaw, _t.SampleHeight(front) - 0.1f, rng))
                    Houses++;
            }

            // Hinterhaeuser: in die Mitte des Blocks, beliebig gedreht
            int versuche = Mathf.CeilToInt(zellen.Count * Hinterhaus[k]);
            for (int i = 0; i < versuche; i++)
            {
                int c = zellen[rng.Next(zellen.Count)];
                int model = Choose(pal, 14f, rng);
                if (model < 0) continue;

                Vector2 posLokal = Lokal(c, w, nx, nz) + new Vector2(
                    ((float)rng.NextDouble() - 0.5f) * Cell, ((float)rng.NextDouble() - 0.5f) * Cell);
                float yaw = d.Rotation + rng.Next(4) * 90f + ((float)rng.NextDouble() * 2f - 1f) * Schraeg[k] * 1.5f;

                Vector2 pos = RioDistricts.ToWorld(d, posLokal);
                if (_t.IsLand(pos) && TryPlace(model, pos, yaw, _t.SampleHeight(pos) - 0.1f, rng))
                    Houses++;
            }
        }

        static Vector2 Lokal(int zelle, int w, int nx, int nz) =>
            new Vector2((zelle % w - nx) * Cell, (zelle / w - nz) * Cell);

        static void Mischen<T>(List<T> liste, System.Random rng)
        {
            for (int i = liste.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (liste[i], liste[j]) = (liste[j], liste[i]);
            }
        }

        /// <summary>Zufaelliges Modell der Palette, das noch in die Restbreite passt.</summary>
        static int Choose(int[] pal, float maxWidth, System.Random rng)
        {
            int best = -1, treffer = 0;
            for (int i = 0; i < pal.Length; i++)
            {
                if (RioModelCatalog.Get(pal[i]).lot.x > maxWidth) continue;
                treffer++;
                if (rng.Next(treffer) == 0) best = pal[i];
            }
            return best;
        }

        // ------------------------------------------------------------------
        // Favelas
        // ------------------------------------------------------------------

        /// <summary>
        /// Huetten gibt es nur noch in den Gang-Vierteln (Zona Oeste, Morro do
        /// Estado), dort aber in jeder Luecke: zwischen den Haeusern der
        /// Blocks, in den Hinterhoefen und den Hang hinauf. Vorher standen sie
        /// an jedem Hang beider Inseln und damit verstreut ueber die ganze
        /// Karte.
        ///
        /// Kein Raster: dicht gestreut, mit Mindestabstand ueber das
        /// Belegungsraster, schief zueinander.
        /// </summary>
        public void PlaceFavelas(System.Random rng)
        {
            const float cell = 48f;

            for (float x = _t.MinX; x < _t.MaxX; x += cell)
            for (float z = _t.MinZ; z < _t.MaxZ; z += cell)
            {
                var center = new Vector2(x + cell * 0.5f, z + cell * 0.5f);
                if (!RioDistricts.InFavela(center)) continue;
                if (!_t.IsLand(center) || _t.IsInLake(center)) continue;

                float slope = _t.SlopeAt(center);
                if (slope > 0.85f) continue;                       // zu steil auch fuer Huetten

                float h = _t.SampleHeight(center);
                if (h < 2.5f || h > 210f) continue;
                if (NearPlace(center, 70f)) continue;
                if (RioWahrzeichen.Belegt(center, 0f)) continue;
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
                    if (!_t.IsLand(p2) || !RioDistricts.InFavela(p2)) continue;

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
                    if (!RioWorldLayout.InStadt(p)) continue;      // Villen nur in der Stadt
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
                if (RioWahrzeichen.Ersetzt(place.Label)) continue;   // Banco Central = Casa Império

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
                        if (!RioWorldLayout.InStadt(cand)) continue;   // keine Tankstelle allein im Feld
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
