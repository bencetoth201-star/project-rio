using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Ein gemeinsamer Index ueber Haeuser und Strassenkacheln. Der Streamer
    /// kennt nur Nummern - hier steht, was hinter einer Nummer steckt.
    ///
    /// 0..18  Haeuser (Reihenfolge wie RioHausDaten)
    /// 19..38 Strassenkacheln 01..20
    ///
    /// Zu den Himmelsrichtungen: glTFast spiegelt beim Import die X-Achse.
    /// Ein Anschluss, der in der glTF-Datei nach Osten zeigt, liegt in Unity
    /// deshalb im Westen. Die Masken unten stehen bereits in Unity-Richtungen,
    /// damit beim Zusammensetzen nichts mehr umgedacht werden muss.
    /// </summary>
    public static class RioModelCatalog
    {
        public const int HouseCount = 19;
        public const int RoadBase = 19;

        // Richtungen als Bitmaske. N = +Z, E = +X, S = -Z, W = -X.
        public const int N = 1, E = 2, S = 4, W = 8;

        public struct Entry
        {
            public string path;
            public Vector2 lot;     // Grundflaeche X x Z
            public float height;
            public bool isRoad;
            public int links;       // nur Strassen: Anschlussmaske bei yaw 0
            public Entry(string path, float lx, float lz, float h, bool road, int links)
            {
                this.path = path; lot = new Vector2(lx, lz); height = h;
                isRoad = road; this.links = links;
            }
        }

        // Strassenkacheln auf dem 12-m-Raster. Nur diese vier braucht das
        // Netz; die Sonderteile (Avenida, Kreisverkehr, Tunnel) bleiben fuer
        // spaeter liegen.
        public const int RoadStraight = RoadBase + 0;   // 01_rua_gerade
        public const int RoadCurve = RoadBase + 1;      // 02_rua_kurve
        public const int RoadT = RoadBase + 2;          // 03_rua_t
        public const int RoadCross = RoadBase + 3;      // 04_rua_kreuzung
        public const int RoadCobble = RoadBase + 7;     // 08_rua_kopfstein

        static readonly string[] RoadSlugs =
        {
            "01_rua_gerade", "02_rua_kurve", "03_rua_t", "04_rua_kreuzung",
            "05_rua_sackgasse", "06_rua_steigung", "07_rua_bushaltestelle",
            "08_rua_kopfstein", "09_avenida_gerade", "10_avenida_kreuzung",
            "11_avenida_einmuendung", "12_avenida_kurve", "13_kreisverkehr",
            "14_copacabana", "15_beco", "16_escadaria", "17_beco_gabelung",
            "18_hangstrasse", "19_viadukt", "20_tunnel"
        };

        // Kachelgroesse X x Z und Hoehe, aus strassen.json
        static readonly float[,] RoadSize =
        {
            {12,12,7.1f},{12,12.7f,7.1f},{12,12,7.1f},{12,12,4.3f},{16.9f,20,7.1f},
            {12,24,9.6f},{12.4f,24,7.3f},{12,12,4.8f},{24,24,8.6f},{24,24,4.3f},
            {24,24,8.6f},{30,30,8.6f},{36,36,7.6f},{36,24,9.1f},{3,12,0.2f},
            {3,12,5f},{7.5f,12,0.2f},{12,24,8.9f},{12,24,12f},{12,24,14f}
        };

        // Anschlussmasken in Unity-Richtungen, bei yaw 0.
        static readonly int[] RoadLinks =
        {
            N | S,        // 01 gerade
            N | W,        // 02 kurve
            N | S | W,    // 03 T
            N | E | S | W,// 04 kreuzung
            N,            // 05 wendehammer
            N | S, N | S, N | S,
            N | S, N | E | S | W, N | S | W, N | W, N | E | S | W, N | S,
            N | S, N | S, N | S | W, N | S, N | S, N | S
        };

        public static int Count => HouseCount + RoadSlugs.Length;

        public static Entry Get(int index)
        {
            if (index < HouseCount)
            {
                var h = RioHausDaten.Haeuser[index];
                return new Entry($"Assets/RioHaeuser/Modelle/{h.slug}.glb",
                                 h.lotBreite, h.lotTiefe, RioHousePlanner.Height(index), false, 0);
            }

            int r = index - RoadBase;
            return new Entry($"Assets/RioStrassen/Modelle/{RoadSlugs[r]}.glb",
                             RoadSize[r, 0], RoadSize[r, 1], RoadSize[r, 2], true, RoadLinks[r]);
        }

        /// <summary>Maske um n Vierteldrehungen im Uhrzeigersinn drehen: N wird E.</summary>
        public static int Rotate(int mask, int quarters)
        {
            quarters &= 3;
            for (int i = 0; i < quarters; i++)
                mask = ((mask << 1) | (mask >> 3)) & 0xF;
            return mask;
        }

        /// <summary>
        /// Sucht Kachel und Drehung fuer eine geforderte Anschlussmaske.
        /// Liefert false, wenn keine der Rasterkacheln passt.
        /// </summary>
        public static bool FindTile(int required, out int model, out float yaw)
        {
            int[] candidates = { RoadCross, RoadT, RoadCurve, RoadStraight };

            foreach (int m in candidates)
            {
                int baseMask = Get(m).links;
                for (int q = 0; q < 4; q++)
                {
                    if (Rotate(baseMask, q) == required)
                    {
                        model = m; yaw = q * 90f; return true;
                    }
                }
            }

            model = RoadStraight; yaw = 0f;
            return false;
        }
    }
}
