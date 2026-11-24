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
        public enum Kind { Centro, Orla, Bairro, Nobre, Industrie }

        public struct District
        {
            public string Name;
            public Kind Type;
            public Vector2 Center;      // Weltkoordinaten
            public float A, B;          // Halbachsen in Metern
            public float Rotation;      // Grad, Richtung der Hauptstrassen
        }

        static District D(string name, Kind k, float px, float py, float a, float b, float rot)
        {
            var c = RioWorldLayout.ToWorld(px, py);
            return new District
            {
                Name = name, Type = k, Center = c,
                A = a * RioWorldLayout.MetersPerPixel,
                B = b * RioWorldLayout.MetersPerPixel,
                Rotation = rot
            };
        }

        public static readonly District[] All =
        {
            // ---------------- Rio, Westinsel ----------------
            D("Centro",        Kind.Centro,    735, 520, 52, 46,  14f),
            D("Flamengo",      Kind.Orla,      772, 586, 30, 34,  10f),
            D("Botafogo",      Kind.Bairro,    702, 596, 40, 36,  26f),
            D("Copacabana",    Kind.Orla,      806, 676, 44, 30, -38f),
            D("Ipanema",       Kind.Orla,      742, 726, 46, 26, -14f),
            D("Lagoa",         Kind.Nobre,     588, 686, 48, 40,   4f),
            D("Tijuca",        Kind.Bairro,    628, 556, 52, 40, -12f),
            D("Zona Norte",    Kind.Bairro,    520, 486, 56, 50,  22f),
            D("Porto",         Kind.Industrie, 520, 442, 40, 26,   0f),
            D("Zona Oeste",    Kind.Bairro,    446, 626, 46, 50, -22f),
            D("Barra",         Kind.Nobre,     470, 760, 50, 32,  12f),

            // ---------------- Niteroi, Ostinsel ----------------
            D("Centro Niteroi", Kind.Centro,   1066, 452, 44, 40, -22f),
            D("Icarai",         Kind.Orla,     1146, 356, 42, 32,  16f),
            D("Niteroi Norte",  Kind.Bairro,   1180, 276, 46, 28,   0f),
            D("Niteroi Leste",  Kind.Bairro,   1232, 478, 40, 42,  26f),
            D("Niteroi Sul",    Kind.Bairro,   1142, 702, 50, 36, -16f),
            D("Militaer",       Kind.Industrie,1236, 586, 32, 28,   0f),
        };

        /// <summary>
        /// Welches Viertel deckt diesen Punkt ab? -1 wenn keines.
        /// Bei Ueberlappung gewinnt das kleinere - so bleibt ein kleines
        /// Spezialviertel im grossen Wohngebiet erhalten.
        /// </summary>
        public static int IndexAt(Vector2 p)
        {
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

        /// <summary>Abstand im Viertelmass: unter 1 liegt innerhalb der Ellipse.</summary>
        public static float Norm(in District d, Vector2 p)
        {
            Vector2 local = ToLocal(d, p);
            float u = local.x / d.A, v = local.y / d.B;
            return Mathf.Sqrt(u * u + v * v);
        }

        /// <summary>Punkt in das gedrehte Koordinatensystem des Viertels.</summary>
        public static Vector2 ToLocal(in District d, Vector2 p)
        {
            Vector2 o = p - d.Center;
            float rad = -d.Rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(o.x * c - o.y * s, o.x * s + o.y * c);
        }

        /// <summary>Zurueck in Weltkoordinaten.</summary>
        public static Vector2 ToWorld(in District d, Vector2 local)
        {
            float rad = d.Rotation * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return d.Center + new Vector2(local.x * c - local.y * s, local.x * s + local.y * c);
        }

        /// <summary>
        /// Welche Viertel verbunden sind. Ohne diese Liste stehen die
        /// Viertel als Inseln da - genau das zeigte der erste Plan.
        /// Indizes zeigen in <see cref="All"/>.
        /// </summary>
        public static readonly int[,] Verbindungen =
        {
            // Rio
            {0,1},{1,2},{2,3},{3,4},{4,5},{5,6},{6,0},{6,7},{7,8},{8,0},
            {7,9},{9,10},{10,5},{2,6},{0,2},{5,9},
            // Niteroi
            {11,12},{12,13},{11,14},{14,16},{11,15},{15,16},{13,14},
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
                default:             px = 5; pz = 4; break;   // Bairro 48 x 36 m
            }
        }
    }
}
