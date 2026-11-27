using System.Collections.Generic;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Die Weltkarte als Daten - abgezeichnet von der Kartenskizze auf dem
    /// Whiteboard. Alle Koordinaten stehen in Skizzen-Pixeln, so wie sie dort
    /// liegen, und werden ueber <see cref="ToWorld"/> in Meter umgerechnet.
    /// So bleibt die Skizze die Quelle der Wahrheit: wer etwas verschieben
    /// will, vergleicht mit dem Whiteboard, nicht mit Weltkoordinaten.
    /// </summary>
    public static class RioWorldLayout
    {
        /// <summary>Meter pro Skizzenpixel. Daraus ergeben sich die rund 20 km2.</summary>
        public const float MetersPerPixel = 8f;

        /// <summary>Bildmitte der Skizze; dort liegt der Weltnullpunkt.</summary>
        const float OriginX = 845f;
        const float OriginY = 540f;

        public const float SeaLevel = 0f;
        public const float CityHeight = 7f;      // flaches Stadtniveau
        public const float LakeLevel = 5.5f;
        public const float CoastRamp = 140f;     // Meter vom Ufer bis Stadtniveau

        public static Vector2 ToWorld(float px, float py) =>
            new Vector2((px - OriginX) * MetersPerPixel, (OriginY - py) * MetersPerPixel);

        public static Vector2[] ToWorld(float[,] pts)
        {
            var result = new Vector2[pts.GetLength(0)];
            for (int i = 0; i < result.Length; i++)
                result[i] = ToWorld(pts[i, 0], pts[i, 1]);
            return result;
        }

        // ------------------------------------------------------------------
        // Landmassen
        // ------------------------------------------------------------------

        /// <summary>Rio: die groessere Insel im Westen, mit Bucht im Suedosten.</summary>
        public static readonly float[,] RioOutlinePx =
        {
            {600,268},{660,300},{700,332},{702,400},{758,430},{790,470},{800,520},
            {780,558},{800,600},{790,642},{770,680},{822,700},{870,730},{880,790},
            {840,832},{760,852},{650,856},{560,840},{480,800},{420,740},{390,660},
            {380,580},{395,500},{420,420},{450,350},{490,300},{540,276}
        };

        /// <summary>Niteroi: kleiner, zerklueftet, mit Bucht von Suedwesten.</summary>
        public static readonly float[,] NiteroiOutlinePx =
        {
            {1010,250},{1070,235},{1140,246},{1200,270},{1260,320},{1300,400},
            {1310,500},{1300,580},{1270,660},{1210,730},{1130,770},{1060,780},
            {1010,750},{990,700},{1030,660},{1080,640},{1090,600},{1050,570},
            {990,560},{960,520},{950,460},{975,400},{960,350},{975,290}
        };

        // ------------------------------------------------------------------
        // Gelaende
        // ------------------------------------------------------------------

        public struct Mountain
        {
            public Vector2 Center;
            public float Radius;
            public float Height;
            public Mountain(float px, float py, float radiusPx, float height)
            {
                Center = ToWorld(px, py);
                Radius = radiusPx * MetersPerPixel;
                Height = height;
            }
        }

        /// <summary>Die braunen Kreise auf der Skizze: Morros entlang der Suedkueste.</summary>
        public static readonly Mountain[] Mountains =
        {
            new Mountain(680, 792, 46f, 310f),   // Hausberg mit der Jesus-Statue
            new Mountain(742, 812, 30f, 180f),
            new Mountain(812, 792, 36f, 230f),
            new Mountain(606, 806, 28f, 150f),
            new Mountain(455, 690, 34f, 170f),   // Huegel im Westen
            new Mountain(430, 470, 30f, 140f),
            new Mountain(1245, 690, 30f, 160f),  // Niteroi Sued
            new Mountain(1285, 430, 34f, 190f),  // Niteroi Ost
            new Mountain(1120, 300, 26f, 130f),
        };

        /// <summary>Der See im Suedwesten von Rio.</summary>
        public static readonly Vector2 LakeCenter = ToWorld(556, 700);
        public static readonly float LakeRadius = 54f * MetersPerPixel;

        /// <summary>Der gelbe Strandbogen an Rios Suedostkueste.</summary>
        public static readonly Vector2 BeachCenter = ToWorld(812, 702);
        public static readonly float BeachRadius = 85f * MetersPerPixel;

        // ------------------------------------------------------------------
        // Strassen
        // ------------------------------------------------------------------

        /// <summary>Die Bruecke ueber die Bucht, von Rios Nordspitze nach Niteroi.</summary>
        public static readonly float[,] BridgePx = { {606,284},{760,282},{900,282},{1008,282} };

        /// <summary>Hauptstrasse: von der Bruecke die Westkueste hinunter bis zum Strand.</summary>
        public static readonly float[,] MainRoadPx =
        {
            {606,286},{560,318},{520,352},{492,432},{500,500},{540,566},
            {592,626},{628,690},{662,744},{700,772},{760,776},{812,742}
        };

        /// <summary>Querverbindung quer durch Rios Zentrum.</summary>
        public static readonly float[,] CrossRoadPx =
        {
            {436,618},{520,600},{600,586},{660,560},{712,540},{760,500},{790,470}
        };

        /// <summary>Ringstrasse in Niteroi.</summary>
        public static readonly float[,] NiteroiRoadPx =
        {
            {1012,258},{1070,300},{1100,370},{1070,452},{1100,520},{1180,520},
            {1230,560},{1248,620},{1230,690},{1160,740}
        };

        public static readonly float[][,] Roads = { MainRoadPx, CrossRoadPx, NiteroiRoadPx };

        // ------------------------------------------------------------------
        // Orte aus der Legende
        // ------------------------------------------------------------------

        public enum Poi
        {
            Hafen, FlughafenGross, FlughafenKlein, JeweleryKlein, JeweleryGross,
            BankGross, BankKlein, Krankenhaus, Clothstore, Stadion, Casino,
            Nightclub, Militaerbasis, JesusStatue
        }

        public struct Place
        {
            public Poi Kind;
            public Vector2 Position;
            public string Label;
            public Place(Poi kind, float px, float py, string label)
            {
                Kind = kind; Position = ToWorld(px, py); Label = label;
            }
        }

        public static readonly List<Place> Places = new List<Place>
        {
            // --- Rio ---
            new Place(Poi.Hafen,          525, 448, "Hafen"),
            // Halbinsel an der Ostkueste, wie Santos Dumont in der Bucht
            new Place(Poi.FlughafenGross, 800, 532, "Flughafen Rio"),
            new Place(Poi.JeweleryKlein,  560, 524, "Jewelery Sul"),
            new Place(Poi.BankGross,      737, 524, "Banco Central"),
            new Place(Poi.Krankenhaus,    650, 570, "Hospital Rio"),
            new Place(Poi.Clothstore,     730, 580, "Clothstore"),
            new Place(Poi.BankKlein,      656, 620, "Banco do Morro"),
            new Place(Poi.Stadion,        566, 620, "Estadio"),
            new Place(Poi.FlughafenKlein, 436, 620, "Airstrip Oeste"),
            new Place(Poi.JesusStatue,    680, 762, "Jesus-Statue"),

            // --- Niteroi ---
            new Place(Poi.Nightclub,     1030, 258, "Nightclub"),
            new Place(Poi.Krankenhaus,   1150, 376, "Hospital Niteroi"),
            new Place(Poi.JeweleryGross, 1020, 430, "Jewelery Niteroi"),
            new Place(Poi.Casino,         986, 466, "Casino"),
            new Place(Poi.BankGross,     1070, 456, "Banco Niteroi"),
            new Place(Poi.FlughafenKlein,1186, 490, "Airstrip Niteroi"),
            new Place(Poi.Militaerbasis, 1236, 586, "Militaerbasis"),
        };
    }
}
