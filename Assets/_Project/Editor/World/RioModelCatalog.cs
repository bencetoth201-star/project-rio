using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Ein gemeinsamer Index ueber Gebaeude und Strassenkacheln. Der
    /// Streamer kennt nur Nummern - hier steht, was hinter einer Nummer
    /// steckt.
    ///
    /// 0..27   Gebaeude (Reihenfolge wie RioHausDaten)
    /// 28..82  Strassen-, Bruecken- und Gelaendeteile
    ///
    /// Alle Richtungen und Anker stehen in Unity-Koordinaten. glTFast
    /// spiegelt beim Import die X-Achse; das ist hier eingerechnet und
    /// darf nicht noch einmal umgedacht werden.
    ///
    /// Erzeugt von Tools/katalog_erzeugen.py - nicht von Hand aendern.
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

        // --- Strassen-, Bruecken- und Gelaendeteile ---
        public const int RoadStraight        = RoadBase +  0;   // 01_rua_gerade
        public const int RoadCurve           = RoadBase +  1;   // 02_rua_kurve
        public const int RoadT               = RoadBase +  2;   // 03_rua_t
        public const int RoadCross           = RoadBase +  3;   // 04_rua_kreuzung
        public const int RoadSackgasse       = RoadBase +  4;   // 05_rua_sackgasse
        public const int RuaSteigung         = RoadBase +  5;   // 06_rua_steigung
        public const int RuaBushaltestelle   = RoadBase +  6;   // 07_rua_bushaltestelle
        public const int RoadCobble          = RoadBase +  7;   // 08_rua_kopfstein
        public const int AvenidaStraight     = RoadBase +  8;   // 09_avenida_gerade
        public const int AvenidaCross        = RoadBase +  9;   // 10_avenida_kreuzung
        public const int AvenidaRuaJoin      = RoadBase + 10;   // 11_avenida_einmuendung
        public const int AvenidaKurve        = RoadBase + 11;   // 12_avenida_kurve
        public const int Kreisverkehr        = RoadBase + 12;   // 13_kreisverkehr
        public const int Copacabana          = RoadBase + 13;   // 14_copacabana
        public const int Beco                = RoadBase + 14;   // 15_beco
        public const int Escadaria           = RoadBase + 15;   // 16_escadaria
        public const int BecoGabelung        = RoadBase + 16;   // 17_beco_gabelung
        public const int Hangstrasse         = RoadBase + 17;   // 18_hangstrasse
        public const int Viadukt             = RoadBase + 18;   // 19_viadukt
        public const int Tunnel              = RoadBase + 19;   // 20_tunnel
        public const int RuaBogen15          = RoadBase + 20;   // 21_rua_bogen_15
        public const int RuaBogen30          = RoadBase + 21;   // 22_rua_bogen_30
        public const int AvenidaBogen15      = RoadBase + 22;   // 23_avenida_bogen_15
        public const int AvenidaBogen30      = RoadBase + 23;   // 24_avenida_bogen_30
        public const int CopacabanaBogen15   = RoadBase + 24;   // 25_copacabana_bogen_15
        public const int AvenidaT            = RoadBase + 25;   // 26_avenida_t
        public const int AvenidaRuaUebergang = RoadBase + 26;   // 27_uebergang_avenida_rua
        public const int Stuetzmauer4        = RoadBase + 27;   // 28_stuetzmauer_4m
        public const int Stuetzmauer8        = RoadBase + 28;   // 29_stuetzmauer_8m
        public const int MauerEckeAussen     = RoadBase + 29;   // 30_mauer_ecke_aussen
        public const int MauerEckeInnen      = RoadBase + 30;   // 31_mauer_ecke_innen
        public const int Boeschung           = RoadBase + 31;   // 32_boeschung
        public const int MauerTreppe         = RoadBase + 32;   // 33_mauer_treppe
        public const int TunnelMitte         = RoadBase + 33;   // 34_tunnel_mitte
        public const int SerpentineLinks     = RoadBase + 34;   // 35_rua_serpentine_links
        public const int SerpentineRechts    = RoadBase + 35;   // 36_rua_serpentine_rechts
        public const int RuaSteigungSanft    = RoadBase + 36;   // 37_rua_steigung_sanft
        public const int BrueckeSegment      = RoadBase + 37;   // 38_bruecke_segment
        public const int BrueckeSteigung     = RoadBase + 38;   // 39_bruecke_steigung
        public const int Brueckenkopf        = RoadBase + 39;   // 40_brueckenkopf
        public const int PfeilerFundament    = RoadBase + 40;   // 41_pfeiler_fundament
        public const int PfeilerSchaft2      = RoadBase + 41;   // 42_pfeiler_schaft_2m
        public const int PfeilerSchaft8      = RoadBase + 42;   // 43_pfeiler_schaft_8m
        public const int PfeilerKopf         = RoadBase + 43;   // 44_pfeiler_kopf
        public const int SchnellGerade       = RoadBase + 44;   // 45_schnellstrasse_gerade
        public const int SchnellBogen15      = RoadBase + 45;   // 46_schnellstrasse_bogen_15
        public const int SchnellBogen30      = RoadBase + 46;   // 47_schnellstrasse_bogen_30
        public const int SchnellAbfahrt      = RoadBase + 47;   // 48_schnellstrasse_abfahrt
        public const int SchnellAuffahrt     = RoadBase + 48;   // 49_schnellstrasse_auffahrt
        public const int SchnellViadukt      = RoadBase + 49;   // 50_schnellstrasse_viadukt
        public const int Stuetzmauer2        = RoadBase + 50;   // 51_stuetzmauer_2m
        public const int MauerAbschluss4R    = RoadBase + 51;   // 52_mauerabschluss_4m_rechts
        public const int MauerAbschluss4L    = RoadBase + 52;   // 53_mauerabschluss_4m_links
        public const int MauerAbschluss2R    = RoadBase + 53;   // 54_mauerabschluss_2m_rechts
        public const int MauerAbschluss2L    = RoadBase + 54;   // 55_mauerabschluss_2m_links

        static readonly float[] HouseHeight = { 3.3f, 5f, 9.2f, 9.6f, 9.1f, 7.2f, 10f, 9.1f, 8.8f, 22.5f, 8.1f, 7.2f, 9.6f, 31.5f, 8.6f, 6.1f, 10.6f, 13.7f, 16f, 139.2f, 118.2f, 22.1f, 12.2f, 8.2f, 8.6f, 22f, 6.5f, 7.6f };

        static readonly string[] RoadSlugs =
        {
            "01_rua_gerade", "02_rua_kurve", "03_rua_t", "04_rua_kreuzung",
            "05_rua_sackgasse", "06_rua_steigung", "07_rua_bushaltestelle", "08_rua_kopfstein",
            "09_avenida_gerade", "10_avenida_kreuzung", "11_avenida_einmuendung", "12_avenida_kurve",
            "13_kreisverkehr", "14_copacabana", "15_beco", "16_escadaria",
            "17_beco_gabelung", "18_hangstrasse", "19_viadukt", "20_tunnel",
            "21_rua_bogen_15", "22_rua_bogen_30", "23_avenida_bogen_15", "24_avenida_bogen_30",
            "25_copacabana_bogen_15", "26_avenida_t", "27_uebergang_avenida_rua", "28_stuetzmauer_4m",
            "29_stuetzmauer_8m", "30_mauer_ecke_aussen", "31_mauer_ecke_innen", "32_boeschung",
            "33_mauer_treppe", "34_tunnel_mitte", "35_rua_serpentine_links", "36_rua_serpentine_rechts",
            "37_rua_steigung_sanft", "38_bruecke_segment", "39_bruecke_steigung", "40_brueckenkopf",
            "41_pfeiler_fundament", "42_pfeiler_schaft_2m", "43_pfeiler_schaft_8m", "44_pfeiler_kopf",
            "45_schnellstrasse_gerade", "46_schnellstrasse_bogen_15", "47_schnellstrasse_bogen_30", "48_schnellstrasse_abfahrt",
            "49_schnellstrasse_auffahrt", "50_schnellstrasse_viadukt", "51_stuetzmauer_2m", "52_mauerabschluss_4m_rechts",
            "53_mauerabschluss_4m_links", "54_mauerabschluss_2m_rechts", "55_mauerabschluss_2m_links",
        };

        /// <summary>Kachel X, Kachel Z, Hoehe des Modells.</summary>
        static readonly float[,] RoadSize =
        {
            {12f,12f,7.1f},   // 01_rua_gerade
            {12f,12f,7.1f},   // 02_rua_kurve
            {12f,12f,7.1f},   // 03_rua_t
            {12f,12f,4.3f},   // 04_rua_kreuzung
            {18f,20f,7.1f},   // 05_rua_sackgasse
            {12f,24f,9.6f},   // 06_rua_steigung
            {12f,24f,7.3f},   // 07_rua_bushaltestelle
            {12f,12f,4.8f},   // 08_rua_kopfstein
            {24f,24f,8.6f},   // 09_avenida_gerade
            {24f,24f,4.3f},   // 10_avenida_kreuzung
            {24f,24f,8.6f},   // 11_avenida_einmuendung
            {36f,36f,8.6f},   // 12_avenida_kurve
            {36f,36f,7.6f},   // 13_kreisverkehr
            {36f,24f,9.1f},   // 14_copacabana
            {4f,12f,0.2f},   // 15_beco
            {4f,12f,5f},   // 16_escadaria
            {12f,12f,0.2f},   // 17_beco_gabelung
            {12f,24f,8.9f},   // 18_hangstrasse
            {12f,24f,12f},   // 19_viadukt
            {12f,24f,14f},   // 20_tunnel
            {13.2f,11.9f,7.1f},   // 21_rua_bogen_15
            {16.6f,23f,7.1f},   // 22_rua_bogen_30
            {25.6f,18.6f,7.6f},   // 23_avenida_bogen_15
            {30.4f,36f,7.6f},   // 24_avenida_bogen_30
            {39.3f,34.2f,8.6f},   // 25_copacabana_bogen_15
            {24f,24f,4.3f},   // 26_avenida_t
            {24f,24f,7.6f},   // 27_uebergang_avenida_rua
            {12f,4f,5.1f},   // 28_stuetzmauer_4m
            {12f,4f,9.1f},   // 29_stuetzmauer_8m
            {4f,4f,5.1f},   // 30_mauer_ecke_aussen
            {4f,4f,4.9f},   // 31_mauer_ecke_innen
            {12f,10f,8.3f},   // 32_boeschung
            {12f,8f,5.1f},   // 33_mauer_treppe
            {12f,24f,7.5f},   // 34_tunnel_mitte
            {22f,22f,9.6f},   // 35_rua_serpentine_links
            {22f,22f,9.6f},   // 36_rua_serpentine_rechts
            {12f,24f,8.3f},   // 37_rua_steigung_sanft
            {12f,24f,12.8f},   // 38_bruecke_segment
            {12f,24f,14.3f},   // 39_bruecke_steigung
            {12f,48f,12.8f},   // 40_brueckenkopf
            {8f,6f,1f},   // 41_pfeiler_fundament
            {4f,4f,2f},   // 42_pfeiler_schaft_2m
            {4f,4f,8f},   // 43_pfeiler_schaft_8m
            {8f,4f,1.4f},   // 44_pfeiler_kopf
            {24f,24f,11.3f},   // 45_schnellstrasse_gerade
            {27f,29f,11.3f},   // 46_schnellstrasse_bogen_15
            {35.8f,56f,11.3f},   // 47_schnellstrasse_bogen_30
            {36f,48f,11.3f},   // 48_schnellstrasse_abfahrt
            {36f,48f,11.3f},   // 49_schnellstrasse_auffahrt
            {24f,24f,20.2f},   // 50_schnellstrasse_viadukt
            {12f,4f,3.1f},   // 51_stuetzmauer_2m
            {12f,4f,5.1f},   // 52_mauerabschluss_4m_rechts
            {12f,4f,5.1f},   // 53_mauerabschluss_4m_links
            {12f,4f,3.2f},   // 54_mauerabschluss_2m_rechts
            {12f,4f,3.1f},   // 55_mauerabschluss_2m_links
        };

        /// <summary>Anschlussmaske je Kachel, in Unity-Richtungen.</summary>
        static readonly int[] RoadLinks =
        {
             5,  9, 13, 15,  1,  5,  5,  5,
             5, 15, 13,  9, 15,  5,  5,  5,
            13,  5,  5,  5,  1,  1,  1,  1,
             1, 13,  5,  0,  0,  0,  0,  0,
             5,  5,  1,  1,  5,  5,  5,  5,
             0,  0,  0,  0,  5,  1,  1,  5,
             5,  5,  0,  0,  0,  0,  0,
        };

        /// <summary>
        /// Die Anschlusspunkte jeder Kachel, in Unity-Koordinaten relativ
        /// zum Pivot: Name, Position, Blickrichtung in Grad.
        ///
        /// Hieran wird verkettet. Zwei Teile passen, wenn ihre Anker
        /// aufeinanderliegen und ihre Richtungen 180 Grad auseinander
        /// stehen. Ohne diese Tabelle gibt es Luecken, weil Kachelmitte
        /// und Anschluss bei Boegen, Rampen und Abfahrten nicht
        /// zusammenfallen.
        /// </summary>
        public struct Anchor
        {
            public string name;
            public Vector3 pos;
            public float yaw;
        }

        static readonly Anchor[][] RoadAnchors =
        {
            // 01_rua_gerade
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f } },
            // 02_rua_kurve
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_E", pos = new Vector3(-6f, 0f, 0f), yaw = -90f } },
            // 03_rua_t
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_E", pos = new Vector3(-6f, 0f, 0f), yaw = -90f } },
            // 04_rua_kreuzung
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_E", pos = new Vector3(-6f, 0f, 0f), yaw = -90f }, new Anchor { name = "Anschluss_Rua_W", pos = new Vector3(6f, 0f, 0f), yaw = 90f } },
            // 05_rua_sackgasse
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 10f), yaw = 0f } },
            // 06_rua_steigung
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 3f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 07_rua_bushaltestelle
            new[] { new Anchor { name = "Bushaltestelle", pos = new Vector3(-4.5f, 0.15f, 0f), yaw = 90f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 08_rua_kopfstein
            new[] { new Anchor { name = "Bonde_Schienen", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f } },
            // 09_avenida_gerade
            new[] { new Anchor { name = "Anschluss_Avenida_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 10_avenida_kreuzung
            new[] { new Anchor { name = "Anschluss_Avenida_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_E", pos = new Vector3(-12f, 0f, 0f), yaw = -90f }, new Anchor { name = "Anschluss_Avenida_W", pos = new Vector3(12f, 0f, 0f), yaw = 90f } },
            // 11_avenida_einmuendung
            new[] { new Anchor { name = "Anschluss_Avenida_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_E", pos = new Vector3(-12f, 0f, 0f), yaw = -90f } },
            // 12_avenida_kurve
            new[] { new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 18f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_E", pos = new Vector3(-18f, 0f, 0f), yaw = -90f } },
            // 13_kreisverkehr
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -18f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 18f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_E", pos = new Vector3(-18f, 0f, 0f), yaw = -90f }, new Anchor { name = "Anschluss_Rua_W", pos = new Vector3(18f, 0f, 0f), yaw = 90f } },
            // 14_copacabana
            new[] { new Anchor { name = "Anschluss_Avenida_N", pos = new Vector3(6f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(6f, 0f, 12f), yaw = 0f }, new Anchor { name = "Strand", pos = new Vector3(-14.5f, -0.45f, 0f), yaw = -90f } },
            // 15_beco
            new[] { new Anchor { name = "Anschluss_Beco_N", pos = new Vector3(-0f, 0.05f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Beco_S", pos = new Vector3(-0f, 0.05f, 6f), yaw = 0f } },
            // 16_escadaria
            new[] { new Anchor { name = "Anschluss_Beco_S", pos = new Vector3(-0f, 0f, 6f), yaw = 0f }, new Anchor { name = "Anschluss_Beco_N", pos = new Vector3(-0f, 4f, -6f), yaw = -180f } },
            // 17_beco_gabelung
            new[] { new Anchor { name = "Anschluss_Beco_N", pos = new Vector3(-0f, 0.05f, -6f), yaw = -180f }, new Anchor { name = "Anschluss_Beco_S", pos = new Vector3(-0f, 0.05f, 6f), yaw = 0f }, new Anchor { name = "Anschluss_Beco_E", pos = new Vector3(-6f, 0.05f, 0f), yaw = -90f } },
            // 18_hangstrasse
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 4f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 19_viadukt
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 6f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 6f, 12f), yaw = 0f }, new Anchor { name = "Unterfahrt", pos = new Vector3(-0f, 0f, 0f), yaw = -90f } },
            // 20_tunnel
            new[] { new Anchor { name = "Tunnel_Eingang", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 21_rua_bogen_15
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_Ende", pos = new Vector3(1.363f, 0f, -10.353f), yaw = -195f } },
            // 22_rua_bogen_30
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_Ende", pos = new Vector3(5.359f, 0f, -20f), yaw = -210f } },
            // 23_avenida_bogen_15
            new[] { new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_Ende", pos = new Vector3(2.044f, 0f, -15.529f), yaw = -195f } },
            // 24_avenida_bogen_30
            new[] { new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_Ende", pos = new Vector3(8.038f, 0f, -30f), yaw = -210f } },
            // 25_copacabana_bogen_15
            new[] { new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_Ende", pos = new Vector3(-4.089f, 0f, -31.058f), yaw = -165f }, new Anchor { name = "Strand", pos = new Vector3(-23.83f, -0.45f, -12.661f), yaw = -90f } },
            // 26_avenida_t
            new[] { new Anchor { name = "Anschluss_Avenida_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Avenida_E", pos = new Vector3(-12f, 0f, 0f), yaw = -90f } },
            // 27_uebergang_avenida_rua
            new[] { new Anchor { name = "Anschluss_Avenida_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f } },
            // 28_stuetzmauer_4m
            new[] { new Anchor { name = "Oben", pos = new Vector3(-0f, 4f, 0f), yaw = -180f }, new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 2.5f), yaw = 0f } },
            // 29_stuetzmauer_8m
            new[] { new Anchor { name = "Oben", pos = new Vector3(-0f, 8f, 0f), yaw = -180f }, new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 2.5f), yaw = 0f } },
            // 30_mauer_ecke_aussen
            new[] { new Anchor { name = "Ecke", pos = new Vector3(-0f, 0f, 0f), yaw = 0f } },
            // 31_mauer_ecke_innen
            new[] { new Anchor { name = "Ecke", pos = new Vector3(-0f, 0f, 0f), yaw = 0f } },
            // 32_boeschung
            new[] { new Anchor { name = "Oben", pos = new Vector3(-0f, 4f, -4f), yaw = -180f }, new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 5.6f), yaw = 0f } },
            // 33_mauer_treppe
            new[] { new Anchor { name = "Anschluss_Beco_Unten", pos = new Vector3(-0f, 0f, 4.6f), yaw = 0f }, new Anchor { name = "Anschluss_Beco_Oben", pos = new Vector3(-0f, 4f, -4f), yaw = -180f } },
            // 34_tunnel_mitte
            new[] { new Anchor { name = "Tunnel_Innen", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 35_rua_serpentine_links
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_Ende", pos = new Vector3(16f, 3f, -16f), yaw = -270f } },
            // 36_rua_serpentine_rechts
            new[] { new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Rua_Ende", pos = new Vector3(-16f, 3f, -16f), yaw = -90f } },
            // 37_rua_steigung_sanft
            new[] { new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-0f, 1.5f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 38_bruecke_segment
            new[] { new Anchor { name = "Anschluss_Bruecke_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Pfeiler_S", pos = new Vector3(-0f, -2.6f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Bruecke_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Pfeiler_N", pos = new Vector3(-0f, -2.6f, -12f), yaw = 0f } },
            // 39_bruecke_steigung
            new[] { new Anchor { name = "Anschluss_Bruecke_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f }, new Anchor { name = "Pfeiler_S", pos = new Vector3(-0f, -2.6f, 12f), yaw = 0f }, new Anchor { name = "Anschluss_Bruecke_N", pos = new Vector3(-0f, 2f, -12f), yaw = -180f }, new Anchor { name = "Pfeiler_N", pos = new Vector3(-0f, -0.6f, -12f), yaw = 0f } },
            // 40_brueckenkopf
            new[] { new Anchor { name = "Anschluss_Bruecke_S", pos = new Vector3(-0f, 0f, 24f), yaw = 0f }, new Anchor { name = "Anschluss_Bruecke_N", pos = new Vector3(-0f, 5f, -24f), yaw = -180f } },
            // 41_pfeiler_fundament
            new[] { new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Oben", pos = new Vector3(-0f, 1f, 0f), yaw = 0f } },
            // 42_pfeiler_schaft_2m
            new[] { new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Oben", pos = new Vector3(-0f, 2f, 0f), yaw = 0f } },
            // 43_pfeiler_schaft_8m
            new[] { new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Oben", pos = new Vector3(-0f, 8f, 0f), yaw = 0f } },
            // 44_pfeiler_kopf
            new[] { new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Traeger_Unterkante", pos = new Vector3(-0f, 1.4f, 0f), yaw = 0f } },
            // 45_schnellstrasse_gerade
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_N", pos = new Vector3(-0f, 0f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(-0f, 0f, 12f), yaw = 0f } },
            // 46_schnellstrasse_bogen_15
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Schnellstrasse_Ende", pos = new Vector3(3.407f, 0f, -25.882f), yaw = -195f } },
            // 47_schnellstrasse_bogen_30
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(-0f, 0f, 0f), yaw = 0f }, new Anchor { name = "Anschluss_Schnellstrasse_Ende", pos = new Vector3(13.397f, 0f, -50f), yaw = -210f } },
            // 48_schnellstrasse_abfahrt
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(6f, 0f, 24f), yaw = 0f }, new Anchor { name = "Anschluss_Schnellstrasse_N", pos = new Vector3(6f, 0f, -24f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_N", pos = new Vector3(-12f, 0f, -24f), yaw = -180f } },
            // 49_schnellstrasse_auffahrt
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(6f, 0f, 24f), yaw = 0f }, new Anchor { name = "Anschluss_Schnellstrasse_N", pos = new Vector3(6f, 0f, -24f), yaw = -180f }, new Anchor { name = "Anschluss_Rua_S", pos = new Vector3(-12f, 0f, 24f), yaw = 0f } },
            // 50_schnellstrasse_viadukt
            new[] { new Anchor { name = "Anschluss_Schnellstrasse_N", pos = new Vector3(-0f, 9f, -12f), yaw = -180f }, new Anchor { name = "Anschluss_Schnellstrasse_S", pos = new Vector3(-0f, 9f, 12f), yaw = 0f }, new Anchor { name = "Unterfahrt_West", pos = new Vector3(7f, 0f, 0f), yaw = -90f }, new Anchor { name = "Unterfahrt_Ost", pos = new Vector3(-7f, 0f, 0f), yaw = 90f } },
            // 51_stuetzmauer_2m
            new[] { new Anchor { name = "Oben", pos = new Vector3(-0f, 2f, 0f), yaw = -180f }, new Anchor { name = "Unten", pos = new Vector3(-0f, 0f, 2.5f), yaw = 0f } },
            // 52_mauerabschluss_4m_rechts
            new[] { new Anchor { name = "Anschluss_Mauer", pos = new Vector3(6f, 0f, 2f), yaw = 90f }, new Anchor { name = "Oben", pos = new Vector3(5f, 3.667f, 0f), yaw = -180f } },
            // 53_mauerabschluss_4m_links
            new[] { new Anchor { name = "Anschluss_Mauer", pos = new Vector3(-6f, 0f, 2f), yaw = -90f }, new Anchor { name = "Oben", pos = new Vector3(-5f, 3.667f, 0f), yaw = 180f } },
            // 54_mauerabschluss_2m_rechts
            new[] { new Anchor { name = "Anschluss_Mauer", pos = new Vector3(6f, 0f, 2f), yaw = 90f }, new Anchor { name = "Oben", pos = new Vector3(5f, 1.833f, 0f), yaw = -180f } },
            // 55_mauerabschluss_2m_links
            new[] { new Anchor { name = "Anschluss_Mauer", pos = new Vector3(-6f, 0f, 2f), yaw = -90f }, new Anchor { name = "Oben", pos = new Vector3(-5f, 1.833f, 0f), yaw = 180f } },
        };

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

        /// <summary>Alle Anker einer Kachel; bei Gebaeuden leer.</summary>
        public static Anchor[] Anchors(int index) =>
            index < HouseCount ? System.Array.Empty<Anchor>() : RoadAnchors[index - RoadBase];

        /// <summary>Sucht einen Anker nach Namensteil, etwa "Ende" oder "Rua_N".</summary>
        public static bool TryAnchor(int index, string enthaelt, out Anchor a)
        {
            foreach (var x in Anchors(index))
                if (x.name.Contains(enthaelt)) { a = x; return true; }
            a = default;
            return false;
        }

        /// <summary>
        /// Wo ein Anker landet, wenn die Kachel an <paramref name="pos"/> mit
        /// <paramref name="yaw"/> Grad steht.
        /// </summary>
        public static Vector3 AnchorWorld(in Anchor a, Vector3 pos, float yaw)
        {
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return pos + new Vector3(a.pos.x * c + a.pos.z * s,
                                     a.pos.y,
                                     -a.pos.x * s + a.pos.z * c);
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
        /// Sucht Rasterkachel und Drehung fuer eine Anschlussmaske.
        ///
        /// Die Reihenfolge ist Absicht: zuerst die Kachel mit den meisten
        /// Armen, aber verglichen wird auf Gleichheit. Eine Kachel mit einem
        /// Arm zu viel liesse den Verkehr an einer Kante enden, an der nichts
        /// weitergeht - genau die Luecken, die im Spiel auffallen.
        /// </summary>
        public static bool FindTile(int required, out int model, out float yaw)
        {
            int[] candidates = { RoadCross, RoadT, RoadCurve, RoadStraight, RoadSackgasse };
            foreach (int m in candidates)
            {
                int baseMask = Get(m).links;
                for (int q = 0; q < 4; q++)
                    if (Rotate(baseMask, q) == required) { model = m; yaw = q * 90f; return true; }
            }
            model = RoadStraight; yaw = 0f;
            return false;
        }

        /// <summary>Dasselbe fuer Avenidas.</summary>
        public static bool FindAvenida(int required, out int model, out float yaw)
        {
            int[] candidates = { AvenidaCross, AvenidaT, AvenidaStraight };
            foreach (int m in candidates)
            {
                int baseMask = Get(m).links;
                for (int q = 0; q < 4; q++)
                    if (Rotate(baseMask, q) == required) { model = m; yaw = q * 90f; return true; }
            }
            model = AvenidaStraight; yaw = 0f;
            return false;
        }
    }
}
