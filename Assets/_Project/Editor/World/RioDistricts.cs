using System.Collections.Generic;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Die Viertel als benannte Flaechen, von Hand gesetzt.
    ///
    /// Vorher entschied ein Rauschen pro Superblock, welcher Typ wo herrscht.
    /// Das Ergebnis war eine luckenlose Decke ueber beide Inseln, in der sich
    /// nichts voneinander abhob. Jetzt ist jedes Viertel eine Ellipse mit
    /// eigener Ausrichtung - und zwischen den Ellipsen bleibt Land frei.
    ///
    /// Die Ausrichtung ist der eigentliche Gewinn: In Rio folgen die Strassen
    /// der Kueste oder dem Tal, nicht dem Norden. Jedes Viertel bekommt
    /// deshalb sein eigenes gedrehtes Raster.
    ///
    /// Koordinaten wie ueberall in Pixeln der Whiteboard-Skizze.
    /// </summary>
    public static class RioDistricts
    {
        /// <summary>Favela = Gang-Viertel: kleines Raster, arme Haeuser, Huetten in jeder Luecke.</summary>
        public enum Kind { Centro, Orla, Bairro, Nobre, Industrie, Favela }

        public struct District
        {
            public string Name;
            public Kind Type;
            public Vector2 Center;      // Weltkoordinaten, auf dem Inselraster
            public float A, B;          // Halbachsen in Metern
            /// <summary>Drehung des Strassenrasters - fuer alle Viertel einer Insel gleich.</summary>
            public float Rotation;
            /// <summary>Drehung der Ellipse, also nur der Form des Viertels.</summary>
            public float FormDrehung;
            /// <summary>Lage der Mitte im gemeinsamen 12-m-Raster der Insel.</summary>
            public Vector2Int Gitter;
            public int Insel;           // 0 = Rio, 1 = Niteroi
        }

        // ------------------------------------------------------------------
        // Ein Raster je Insel
        //
        // Vorher hatte jedes Viertel sein eigenes, verschieden gedrehtes
        // Strassenraster. Solange die Viertel weit auseinanderlagen, liefen
        // Avenidas mit Boegen dazwischen. Seit sie dicht aneinanderstossen,
        // bleiben 0 bis 24 m Platz - zu wenig fuer einen Bogen mit 40 m
        // Radius, und von 24 Verbindungen kamen nur 6 zustande.
        //
        // Jetzt liegen alle Viertel einer Insel auf demselben 12-m-Raster mit
        // derselben Drehung. An der Naht zwischen zwei Vierteln treffen die
        // Strassenzellen dann genau aufeinander und werden direkt verbunden
        // (RioStrassenplan.NaehteVerbinden). Organisch wird es trotzdem: durch
        // die unregelmaessigen Linien je Viertel und die schiefen Haeuser.
        // ------------------------------------------------------------------

        static readonly float[] InselDrehung = { 8f, -8f };

        /// <summary>Alles westlich davon ist Rio, oestlich Niteroi (Weltmeter).</summary>
        const float InselGrenzeX = 300f;

        static District D(string name, Kind k, float px, float py, float a, float b, float form)
        {
            var c = RioWorldLayout.ToWorld(px, py);
            int insel = c.x < InselGrenzeX ? 0 : 1;
            float rot = InselDrehung[insel];

            // Mitte auf das gemeinsame Raster der Insel legen (Ursprung im Weltnullpunkt)
            float rad = rot * Mathf.Deg2Rad, cs = Mathf.Cos(rad), sn = Mathf.Sin(rad);
            var lokal = new Vector2(c.x * cs - c.y * sn, c.x * sn + c.y * cs);
            var g = new Vector2Int(Mathf.RoundToInt(lokal.x / 12f), Mathf.RoundToInt(lokal.y / 12f));
            var gl = new Vector2(g.x * 12f, g.y * 12f);
            c = new Vector2(gl.x * cs + gl.y * sn, -gl.x * sn + gl.y * cs);

            return new District
            {
                Name = name, Type = k, Center = c,
                A = a * RioWorldLayout.MetersPerPixel,
                B = b * RioWorldLayout.MetersPerPixel,
                Rotation = rot, FormDrehung = form, Gitter = g, Insel = insel
            };
        }

        /// <summary>
        /// Uebernimmt das Nachbarviertel an dieser Stelle die Naht? Dann legt
        /// dieses Viertel dort keine Ringstrasse an - sonst liefen an jeder
        /// Naht zwei Strassen nebeneinander. Die Ringstrasse gehoert immer
        /// dem Viertel mit dem kleineren Index.
        /// </summary>
        public static bool NachbarUebernimmt(int di, Vector2 nachbar, System.Func<Vector2, bool> istStadt)
        {
            int dj = IndexAt(nachbar);
            return dj >= 0 && dj < di && All[dj].Insel == All[di].Insel && istStadt(nachbar);
        }

        /// <summary>
        /// Nach der Skizze: die Viertel liegen dicht an dicht innerhalb der
        /// beiden gruenen Stadtumrisse (RioWorldLayout.InStadt) und ueberlappen
        /// sich, damit keine Felder dazwischen frei bleiben. Dazu die beiden
        /// Gang-Viertel: Jacarezinho in Rio zwischen Zona Oeste und Stadion,
        /// Morro do Estado im Osten von Niteroi.
        ///
        /// Index 0 muss Centro bleiben (Startpunkt der Figur).
        /// </summary>
        public static readonly District[] All =
        {
            // ---------------- Rio ----------------
            // Bewusst zu gross: die Ellipsen ueberlappen sich, damit dazwischen
            // kein Feld frei bleibt, und der Stadtumriss schneidet sie aussen ab.
            D("Centro",          Kind.Centro,    680, 470, 85, 70,  14f),   // 0
            D("Porto",           Kind.Industrie, 500, 440, 55, 50,   0f),   // 1
            D("Zona Norte",      Kind.Bairro,    545, 515, 80, 70,  22f),   // 2
            D("Tijuca",          Kind.Bairro,    615, 585, 85, 65, -12f),   // 3
            D("Botafogo",        Kind.Bairro,    725, 585, 70, 65,  26f),   // 4
            D("Copacabana",      Kind.Orla,      762, 662, 55, 50, -38f),   // 5
            D("Ipanema",         Kind.Orla,      705, 712, 50, 30, -14f),   // 6
            D("Lagoa",           Kind.Nobre,     640, 672, 55, 40,   4f),   // 7
            D("Zona Oeste",      Kind.Bairro,    445, 600, 60, 100, -10f),  // 8
            D("Jacarezinho",     Kind.Favela,    497, 565, 48, 85,  -8f),   // 9  Gang-Viertel

            // ---------------- Niteroi ----------------
            D("Centro Niteroi",  Kind.Centro,   1040, 460, 70, 58, -22f),   // 10
            D("Icarai",          Kind.Orla,     1008, 372, 58, 52,  16f),   // 11
            D("Niteroi Norte",   Kind.Bairro,   1100, 350, 72, 48,   0f),   // 12
            D("Fonseca",         Kind.Bairro,   1110, 470, 55, 65,   0f),   // 13
            D("Morro do Estado", Kind.Favela,   1205, 440, 72, 95,   0f),   // 14 Gang-Viertel
        };

        // ------------------------------------------------------------------
        // Strassenlinien
        //
        // Vorher lag jede Strasse im festen Abstand, alle Blocks gleich gross
        // - das sah aus wie Millimeterpapier. Jetzt hat jedes Viertel eigene,
        // unregelmaessige Abstaende, und einzelne Strassenstuecke zwischen zwei
        // Kreuzungen fallen weg. Daraus entstehen ungleiche, verwinkelte
        // Blocks. Strassenplan und Hausplaner fragen beide hier, damit sie
        // dasselbe Netz sehen.
        // ------------------------------------------------------------------

        class Linien
        {
            public int[] X, Z;                 // sortierte Zellindizes der Strassenlinien
            public HashSet<long> Weg;          // weggelassene Stuecke
        }

        static Linien[] _linien;

        static void Abstaende(Kind k, out int min, out int max, out float weg)
        {
            switch (k)
            {
                case Kind.Centro:    min = 4; max = 6;  weg = 0.10f; break;
                case Kind.Orla:      min = 4; max = 8;  weg = 0.15f; break;
                case Kind.Nobre:     min = 4; max = 7;  weg = 0.22f; break;
                case Kind.Industrie: min = 7; max = 10; weg = 0.10f; break;
                case Kind.Favela:    min = 3; max = 5;  weg = 0.35f; break;
                default:             min = 3; max = 6;  weg = 0.22f; break;   // Bairro
            }
        }

        static Linien LinienVon(int di)
        {
            if (_linien == null || _linien.Length != All.Length) _linien = new Linien[All.Length];
            if (_linien[di] != null) return _linien[di];

            var d = All[di];
            Abstaende(d.Type, out int min, out int max, out float quote);
            var rng = new System.Random(7919 * (di + 1) + 17);
            int nx = Halbraster(d) + 6, nz = nx;

            var l = new Linien { X = Folge(rng, nx, min, max), Z = Folge(rng, nz, min, max), Weg = new HashSet<long>() };
            for (int i = 0; i < l.X.Length; i++)
                for (int k = 0; k < l.Z.Length - 1; k++)
                    if (rng.NextDouble() < quote) l.Weg.Add(Key(0, l.X[i], k));
            for (int i = 0; i < l.Z.Length; i++)
                for (int k = 0; k < l.X.Length - 1; k++)
                    if (rng.NextDouble() < quote) l.Weg.Add(Key(1, l.Z[i], k));

            _linien[di] = l;
            return l;
        }

        static int[] Folge(System.Random rng, int n, int min, int max)
        {
            var liste = new List<int> { 0 };
            for (int i = 0; i > -n;) { i -= rng.Next(min, max + 1); liste.Insert(0, i); }
            for (int i = 0; i < n;) { i += rng.Next(min, max + 1); liste.Add(i); }
            return liste.ToArray();
        }

        static long Key(int achse, int linie, int stueck) =>
            ((long)achse << 48) | ((long)(linie + 32768) << 24) | (long)(stueck + 32768);

        /// <summary>Gibt es das Stueck der Linie zwischen Querlinie k und k+1?</summary>
        static bool Stueck(Linien l, int achse, int linie, int k, int anzahlQuer) =>
            k >= 0 && k < anzahlQuer - 1 && !l.Weg.Contains(Key(achse, linie, k));

        /// <summary>
        /// Ist die Zelle (lokale Indizes ix, iz im gedrehten Raster des Viertels)
        /// Strasse? Ohne Randring - den legen Strassen- und Hausplaner selbst.
        /// </summary>
        public static bool IstStrasse(int di, int ix, int iz)
        {
            var l = LinienVon(di);
            int kx = System.Array.BinarySearch(l.X, ix);
            int kz = System.Array.BinarySearch(l.Z, iz);

            if (kx >= 0 && kz >= 0)
            {
                // Kreuzung: Strasse, solange mindestens ein Stueck daran haengt
                return Stueck(l, 0, ix, kz - 1, l.Z.Length) || Stueck(l, 0, ix, kz, l.Z.Length)
                    || Stueck(l, 1, iz, kx - 1, l.X.Length) || Stueck(l, 1, iz, kx, l.X.Length);
            }
            if (kx >= 0) return Stueck(l, 0, ix, ~kz - 1, l.Z.Length);
            if (kz >= 0) return Stueck(l, 1, iz, ~kx - 1, l.X.Length);
            return false;
        }

        /// <summary>
        /// Streicht Strassenstuecke, die im Viertel mit weniger als min Zellen
        /// zusammenhaengen. Laeuft im Strassenplan und im Hausplaner auf
        /// demselben Raster - sonst wirft der Strassenplan die Stummel spaeter
        /// als Splitter weg, und der Hausplaner laesst dort trotzdem einen
        /// leeren Streifen frei.
        /// </summary>
        public static void StummelStreichen(bool[] strasse, int w, int h, int min)
        {
            var besucht = new bool[w * h];
            var stueck = new List<int>();
            var stapel = new Stack<int>();
            for (int start = 0; start < w * h; start++)
            {
                if (!strasse[start] || besucht[start]) continue;
                stueck.Clear();
                stapel.Push(start);
                besucht[start] = true;
                while (stapel.Count > 0)
                {
                    int c = stapel.Pop();
                    stueck.Add(c);
                    int cx = c % w, cz = c / w;
                    for (int r = 0; r < 4; r++)
                    {
                        int x = cx + (r == 0 ? 1 : r == 1 ? -1 : 0);
                        int z = cz + (r == 2 ? 1 : r == 3 ? -1 : 0);
                        if (x < 0 || z < 0 || x >= w || z >= h) continue;
                        int n = z * w + x;
                        if (!strasse[n] || besucht[n]) continue;
                        besucht[n] = true;
                        stapel.Push(n);
                    }
                }
                if (stueck.Count < min)
                    foreach (int c in stueck) strasse[c] = false;
            }
        }

        /// <summary>
        /// Halbe Kantenlaenge des Rechenrasters eines Viertels, in Zellen. Muss
        /// die ganze Ellipse fassen: der Rand ist verrauscht (bis zum 1,22-
        /// fachen der Halbachse), und die Form ist gegen das Raster gedreht.
        /// Vorher reichte das Raster nur bis A/12 + 2 Zellen - am Rand fielen
        /// Zellen weg, die laut IndexAt zum Viertel gehoerten, und dort legte
        /// keine Seite eine Strasse an.
        /// </summary>
        public static int Halbraster(in District d) =>
            Mathf.CeilToInt(Mathf.Max(d.A, d.B) * 1.25f / 12f) + 2;

        /// <summary>Liegt der Punkt in einem Gang-Viertel?</summary>
        public static bool InFavela(Vector2 p)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Type == Kind.Favela && Norm(All[i], p) <= 1f) return true;
            return false;
        }

        /// <summary>
        /// Welches Viertel deckt diesen Punkt ab? -1 wenn keines.
        /// Bei Ueberlappung gewinnt das kleinere - so bleibt ein kleines
        /// Spezialviertel im grossen Wohngebiet erhalten.
        /// </summary>
        public static int IndexAt(Vector2 p)
        {
            // Gang-Viertel gehen immer vor: sonst frisst ein grosses
            // Nachbarviertel ihren Rand.
            for (int i = 0; i < All.Length; i++)
                if (All[i].Type == Kind.Favela && Norm(All[i], p) <= 1f) return i;

            int best = -1;
            float bestArea = float.MaxValue;

            for (int i = 0; i < All.Length; i++)
            {
                if (Norm(All[i], p) > 1f) continue;
                float area = All[i].A * All[i].B;
                if (area < bestArea) { bestArea = area; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Abstand im Viertelmass: unter 1 liegt innerhalb.
        ///
        /// Der Rand bekommt Rauschen aufgeschlagen. Eine perfekte Ellipse
        /// sieht man einem Stadtplan sofort an - echte Viertelgrenzen folgen
        /// Haengen, Kuesten und Hauptstrassen und sind entsprechend krumm.
        /// </summary>
        public static float Norm(in District d, Vector2 p)
        {
            // Die Form hat ihre eigene Drehung, unabhaengig vom Strassenraster
            Vector2 o = p - d.Center;
            float frad = d.FormDrehung * Mathf.Deg2Rad;
            float fc = Mathf.Cos(frad), fs = Mathf.Sin(frad);
            Vector2 local = new Vector2(o.x * fc - o.y * fs, o.x * fs + o.y * fc);
            float u = local.x / d.A, v = local.y / d.B;
            float r = Mathf.Sqrt(u * u + v * v);
            if (r < 0.55f) return r;          // innen braucht es kein Rauschen

            float winkel = Mathf.Atan2(v, u);
            float n = Mathf.PerlinNoise(
                Mathf.Cos(winkel) * 1.7f + d.Center.x * 0.0013f + 11f,
                Mathf.Sin(winkel) * 1.7f + d.Center.y * 0.0013f + 23f);
            return r / (0.82f + n * 0.36f);
        }

        // ------------------------------------------------------------------
        // Das gedrehte Koordinatensystem eines Viertels
        //
        // Hier lag der Fehler, der das halbe Strassennetz zerrissen hat:
        // Diese beiden Funktionen benutzten die uebliche mathematische
        // 2D-Drehung, die gegen den Uhrzeigersinn dreht. Unitys Yaw dreht
        // von oben gesehen aber im Uhrzeigersinn - yaw 90 zeigt nach +X.
        //
        // Das Kachelraster lief damit in die eine Richtung gedreht, die
        // Kacheln selbst in die andere. Bei Rotation 0 fiel es nicht auf;
        // in einem um 26 Grad gedrehten Viertel lagen die Anschluesse
        // benachbarter Kacheln bis zu 10 m auseinander. Von 35.325
        // Anschluessen hatten 26.485 keinen Partner.
        //
        // Beide Funktionen drehen jetzt wie Unity. Wer sie anfasst, muss
        // sie weiter gegenseitig invers halten.
        // ------------------------------------------------------------------

        /// <summary>Punkt in das gedrehte Koordinatensystem des Viertels.</summary>
        public static Vector2 ToLocal(in District d, Vector2 p)
        {
            Vector2 o = p - d.Center;
            float rad = d.Rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(o.x * c - o.y * s, o.x * s + o.y * c);
        }

        /// <summary>Zurueck in Weltkoordinaten, mit Unitys Drehsinn.</summary>
        public static Vector2 ToWorld(in District d, Vector2 local)
        {
            float rad = d.Rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return d.Center + new Vector2(local.x * c + local.y * s, -local.x * s + local.y * c);
        }

        /// <summary>
        /// Welche Viertel verbunden sind. Ohne diese Liste stehen die
        /// Viertel als Inseln da - genau das zeigte der erste Plan.
        /// Indizes zeigen in <see cref="All"/>.
        /// </summary>
        public static readonly int[,] Verbindungen =
        {
            // Rio
            {0,1},{0,2},{0,3},{0,4},{1,2},{1,8},{2,3},{2,8},{2,9},{3,4},
            {3,7},{3,9},{4,5},{5,6},{6,7},{7,8},{8,9},
            // Niteroi
            {10,11},{10,12},{10,13},{11,12},{12,13},{13,14},{12,14},
        };

        /// <summary>Strassenraster je Viertelstyp: Periode in X und Z, in 12-m-Kacheln.</summary>
        public static void Raster(Kind k, out int px, out int pz)
        {
            switch (k)
            {
                case Kind.Centro:    px = 5; pz = 5; break;   // 48 x 48 m
                case Kind.Orla:      px = 6; pz = 8; break;   // 60 x 84 m, Riegel quer zum Wasser
                case Kind.Nobre:     px = 6; pz = 5; break;   // 60 x 48 m
                case Kind.Industrie: px = 10; pz = 8; break;  // 108 x 84 m
                case Kind.Favela:    px = 4; pz = 3; break;   // 36 x 24 m, enge Gassen
                default:             px = 5; pz = 4; break;   // Bairro 48 x 36 m
            }
        }
    }
}
