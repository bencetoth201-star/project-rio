using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Ein gemeinsamer Index ueber Gebaeude und Strassenkacheln. Der Streamer
    /// kennt nur Nummern - hier steht, was hinter einer Nummer steckt.
    ///
    /// 0..27  Gebaeude (Reihenfolge wie RioHausDaten)
    /// 28..60 Strassenkacheln
    ///
    /// Zu den Himmelsrichtungen: glTFast spiegelt beim Import die X-Achse.
    /// Ein Anschluss, der in der glTF-Datei nach Osten zeigt, liegt in Unity
    /// deshalb im Westen. Die Masken unten stehen bereits in Unity-Richtungen,
    /// damit beim Zusammensetzen nichts mehr umgedacht werden muss.
    ///
    /// Erzeugt aus haeuser.json und strassen.json - nicht von Hand aendern.
    /// </summary>
    public static class RioModelCatalog
    {
        public const int HouseCount = 28;
        public const int RoadBase = 28;

        // Richtungen als Bitmaske. N = +Z, E = +X, S = -Z, W = -X.
        public const int N = 1, E = 2, S = 4, W = 8;

        // --- Gebaeude, die der Generator namentlich braucht ---
        public const int Tankstelle = 15;
        public const int VillaLeblon = 16;
        public const int VillaJoatinga = 17;
        public const int Bank = 18;
        public const int TorreCentro = 19;
        public const int Edificio70 = 20;
        public const int Igreja = 21;
        public const int Lagerhalle = 22;
        public const int EckPadaria = 23;
        public const int EckSobrado = 24;
        public const int EckPredio = 25;
        public const int Praca = 26;
        public const int Parque = 27;

        // --- Strassenkacheln auf dem 12-m-Raster ---
        public const int RoadStraight = RoadBase + 0;
        public const int RoadCurve = RoadBase + 1;
        public const int RoadT = RoadBase + 2;
        public const int RoadCross = RoadBase + 3;
        public const int RoadCobble = RoadBase + 7;
        public const int AvenidaStraight = RoadBase + 8;
        public const int AvenidaCross = RoadBase + 9;
        public const int AvenidaRuaJoin = RoadBase + 10;
        public const int Copacabana = RoadBase + 13;
        public const int Beco = RoadBase + 14;
        public const int Escadaria = RoadBase + 15;
        public const int Hangstrasse = RoadBase + 17;
        public const int Tunnel = RoadBase + 19;
        public const int RuaBogen15 = RoadBase + 20;
        public const int RuaBogen30 = RoadBase + 21;
        public const int AvenidaBogen15 = RoadBase + 22;
        public const int AvenidaBogen30 = RoadBase + 23;
        public const int CopacabanaBogen15 = RoadBase + 24;
        public const int AvenidaT = RoadBase + 25;
        public const int AvenidaRuaUebergang = RoadBase + 26;
        public const int Stuetzmauer4 = RoadBase + 27;
        public const int Stuetzmauer8 = RoadBase + 28;
        public const int MauerEckeAussen = RoadBase + 29;
        public const int MauerEckeInnen = RoadBase + 30;
        public const int Boeschung = RoadBase + 31;
        public const int MauerTreppe = RoadBase + 32;

        static readonly float[] HouseHeight = { 3.3f, 5.0f, 9.2f, 9.6f, 9.1f, 7.2f, 10.0f, 9.1f, 8.8f, 22.5f, 8.1f, 7.2f, 9.6f, 31.5f, 8.6f, 6.1f, 10.6f, 13.7f, 16.0f, 139.2f, 118.2f, 22.1f, 12.2f, 8.2f, 8.6f, 22.0f, 6.5f, 7.6f };

        static readonly string[] RoadSlugs = { "01_rua_gerade", "02_rua_kurve", "03_rua_t", "04_rua_kreuzung", "05_rua_sackgasse", "06_rua_steigung", "07_rua_bushaltestelle", "08_rua_kopfstein", "09_avenida_gerade", "10_avenida_kreuzung", "11_avenida_einmuendung", "12_avenida_kurve", "13_kreisverkehr", "14_copacabana", "15_beco", "16_escadaria", "17_beco_gabelung", "18_hangstrasse", "19_viadukt", "20_tunnel", "21_rua_bogen_15", "22_rua_bogen_30", "23_avenida_bogen_15", "24_avenida_bogen_30", "25_copacabana_bogen_15", "26_avenida_t", "27_uebergang_avenida_rua", "28_stuetzmauer_4m", "29_stuetzmauer_8m", "30_mauer_ecke_aussen", "31_mauer_ecke_innen", "32_boeschung", "33_mauer_treppe" };

        /// <summary>Kachel X, Kachel Z, Hoehe.</summary>
        static readonly float[,] RoadSize =
        {
            {12f,12f,7.1f},
            {12f,12f,7.1f},
            {12f,12f,7.1f},
            {12f,12f,4.3f},
            {18f,20f,7.1f},
            {12f,24f,9.6f},
            {12f,24f,7.3f},
            {12f,12f,4.8f},
            {24f,24f,8.6f},
            {24f,24f,4.3f},
            {24f,24f,8.6f},
            {36f,36f,8.6f},
            {36f,36f,7.6f},
            {36f,24f,9.1f},
            {4f,12f,0.2f},
            {4f,12f,5.0f},
            {12f,12f,0.2f},
            {12f,24f,8.9f},
            {12f,24f,12.0f},
            {12f,24f,14.0f},
            {13.2f,11.9f,7.1f},
            {16.6f,23.0f,7.1f},
            {25.6f,18.6f,7.6f},
            {30.4f,36.0f,7.6f},
            {39.3f,34.2f,8.6f},
            {24f,24f,4.3f},
            {24f,24f,7.6f},
            {12f,4f,5.1f},
            {12f,4f,9.1f},
            {4f,4f,5.1f},
            {4f,4f,4.9f},
            {12f,10f,8.3f},
            {12f,8f,5.1f}
        };

        static readonly int[] RoadLinks = { 5, 9, 13, 15, 1, 5, 5, 5, 5, 15, 13, 9, 15, 5, 5, 5, 13, 5, 5, 5, 1, 1, 1, 1, 1, 13, 5, 0, 0, 0, 0, 0, 0 };

        public struct Entry
        {
            public string path;
            public Vector2 lot;
            public float height;
            public bool isRoad;
            public int links;
        }

        public static int Count => HouseCount + RoadSlugs.Length;

        public static Entry Get(int index)
        {
            if (index < HouseCount)
            {
                var h = RioHausDaten.Haeuser[index];
                return new Entry
                {
                    path = $"Assets/RioHaeuser/Modelle/{h.slug}.glb",
                    lot = new Vector2(h.lotBreite, h.lotTiefe),
                    height = HouseHeight[index],
                    isRoad = false,
                    links = 0
                };
            }

            int r = index - RoadBase;
            return new Entry
            {
                path = $"Assets/RioStrassen/Modelle/{RoadSlugs[r]}.glb",
                lot = new Vector2(RoadSize[r, 0], RoadSize[r, 1]),
                height = RoadSize[r, 2],
                isRoad = true,
                links = RoadLinks[r]
            };
        }

        /// <summary>Maske um n Vierteldrehungen im Uhrzeigersinn drehen: N wird E.</summary>
        public static int Rotate(int mask, int quarters)
        {
            quarters &= 3;
            for (int i = 0; i < quarters; i++)
                mask = ((mask << 1) | (mask >> 3)) & 0xF;
            return mask;
        }

        /// <summary>Sucht Rasterkachel und Drehung fuer eine Anschlussmaske.</summary>
        public static bool FindTile(int required, out int model, out float yaw)
        {
            int[] candidates = { RoadCross, RoadT, RoadCurve, RoadStraight };
            foreach (int m in candidates)
            {
                int baseMask = Get(m).links;
                for (int q = 0; q < 4; q++)
                    if (Rotate(baseMask, q) == required) { model = m; yaw = q * 90f; return true; }
            }
            model = RoadStraight; yaw = 0f;
            return false;
        }
    }
}
