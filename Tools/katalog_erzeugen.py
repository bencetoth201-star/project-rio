"""Erzeugt RioModelCatalog.cs aus strassen.json.

Die JSON-Datei kommt mit dem Modell-ZIP und ist die Wahrheit ueber Groesse,
Hoehe und Anschlusspunkte jedes Teils. Von Hand gepflegte Tabellen sind beim
letzten Mal auseinandergelaufen, sobald neue Teile dazukamen.

Achsen: glTFast spiegelt beim Import die X-Achse. Alles, was hier
geschrieben wird, steht deshalb schon in Unity-Koordinaten:

    Position  (x, y, z)  ->  (-x, y, z)
    Drehung   yaw        ->  -yaw

Ein Linksbogen bleibt dabei ein Linksbogen, weil Unity selbst linkshaendig
ist - die beiden Spiegelungen heben sich gegenseitig auf.
"""
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STRASSEN = os.path.join(ROOT, "Assets", "RioStrassen", "strassen.json")
ZIEL = os.path.join(ROOT, "Assets", "_Project", "Editor", "World", "RioModelCatalog.cs")

# Himmelsrichtungen als Bitmaske, in Unity-Richtungen
N, E, S, W = 1, 2, 4, 8

# Nur diese Anschlusstypen zaehlen fuer die Rastermaske. "Mauer" verbindet
# Mauern aneinander, nicht Strassen, und hat im Raster nichts zu suchen.
FAHRBAHN = ("Rua", "Avenida", "Beco", "Schnellstrasse", "Bruecke")

# Name im Generator -> die zwei Ziffern vor dem Slug
NAMEN = {
    "RoadStraight": "01", "RoadCurve": "02", "RoadT": "03", "RoadCross": "04",
    "RoadSackgasse": "05", "RuaSteigung": "06", "RuaBushaltestelle": "07",
    "RoadCobble": "08",
    "AvenidaStraight": "09", "AvenidaCross": "10", "AvenidaRuaJoin": "11",
    "AvenidaKurve": "12", "Kreisverkehr": "13", "Copacabana": "14",
    "Beco": "15", "Escadaria": "16", "BecoGabelung": "17",
    "Hangstrasse": "18", "Viadukt": "19", "Tunnel": "20",
    "RuaBogen15": "21", "RuaBogen30": "22",
    "AvenidaBogen15": "23", "AvenidaBogen30": "24", "CopacabanaBogen15": "25",
    "AvenidaT": "26", "AvenidaRuaUebergang": "27",
    "Stuetzmauer4": "28", "Stuetzmauer8": "29",
    "MauerEckeAussen": "30", "MauerEckeInnen": "31",
    "Boeschung": "32", "MauerTreppe": "33",
    "TunnelMitte": "34",
    "SerpentineLinks": "35", "SerpentineRechts": "36", "RuaSteigungSanft": "37",
    "BrueckeSegment": "38", "BrueckeSteigung": "39", "Brueckenkopf": "40",
    "PfeilerFundament": "41", "PfeilerSchaft2": "42", "PfeilerSchaft8": "43",
    "PfeilerKopf": "44",
    "SchnellGerade": "45", "SchnellBogen15": "46", "SchnellBogen30": "47",
    "SchnellAbfahrt": "48", "SchnellAuffahrt": "49", "SchnellViadukt": "50",
    "Stuetzmauer2": "51",
    "MauerAbschluss4R": "52", "MauerAbschluss4L": "53",
    "MauerAbschluss2R": "54", "MauerAbschluss2L": "55",
}

HAUS_HOEHE = [3.3, 5.0, 9.2, 9.6, 9.1, 7.2, 10.0, 9.1, 8.8, 22.5, 8.1, 7.2,
              9.6, 31.5, 8.6, 6.1, 10.6, 13.7, 16.0, 139.2, 118.2, 22.1,
              12.2, 8.2, 8.6, 22.0, 6.5, 7.6]

HAUS_NAMEN = [("Tankstelle", 15), ("VillaLeblon", 16), ("VillaJoatinga", 17),
              ("Bank", 18), ("TorreCentro", 19), ("Edificio70", 20),
              ("Igreja", 21), ("Lagerhalle", 22), ("EckPadaria", 23),
              ("EckSobrado", 24), ("EckPredio", 25), ("Praca", 26),
              ("Parque", 27)]


def richtung(x, z):
    """Welche Kante des Rechtecks ein Anker bedient, in Unity-Richtungen."""
    if abs(x) < 0.01 and abs(z) < 0.01:
        return 0                        # Anker im Pivot: Bogenstart
    if abs(z) >= abs(x):
        return N if z > 0 else S
    return E if x > 0 else W


def maske(anchors):
    """Anschlussmaske einer Kachel, aus den Ankerpositionen abgeleitet.

    Die Buchstaben im Ankernamen stehen in glTF-Richtungen und sind nach der
    X-Spiegelung irrefuehrend; die Position ist die Wahrheit.
    """
    m = 0
    hat_ende = False
    for name, werte in anchors.items():
        x, _y, z, _yaw = werte
        teil = name.split("_")
        if teil[0] != "Anschluss" or len(teil) < 3 or teil[1] not in FAHRBAHN:
            continue
        if teil[2] == "Ende":
            hat_ende = True
            continue
        m |= richtung(-x, z)            # -x: X-Spiegelung
    # Boegen werden ueber ihre Anker verkettet, nicht ueber die Maske gesucht.
    # Sie bekommen nur den Startanschluss, damit FindTile sie niemals fuer
    # eine Kreuzung auswaehlt.
    return N if hat_ende else m


def kopf(anzahl):
    return [
        "using UnityEngine;",
        "",
        "namespace Rio.WorldGen",
        "{",
        "    /// <summary>",
        "    /// Ein gemeinsamer Index ueber Gebaeude und Strassenkacheln. Der",
        "    /// Streamer kennt nur Nummern - hier steht, was hinter einer Nummer",
        "    /// steckt.",
        "    ///",
        "    /// 0..27   Gebaeude (Reihenfolge wie RioHausDaten)",
        "    /// 28..%d  Strassen-, Bruecken- und Gelaendeteile" % (27 + anzahl),
        "    ///",
        "    /// Alle Richtungen und Anker stehen in Unity-Koordinaten. glTFast",
        "    /// spiegelt beim Import die X-Achse; das ist hier eingerechnet und",
        "    /// darf nicht noch einmal umgedacht werden.",
        "    ///",
        "    /// Erzeugt von Tools/katalog_erzeugen.py - nicht von Hand aendern.",
        "    /// </summary>",
        "    public static class RioModelCatalog",
        "    {",
        "        public const int HouseCount = 28;",
        "        public const int RoadBase = 28;",
        "",
        "        // Richtungen als Bitmaske. N = +Z, E = +X, S = -Z, W = -X.",
        "        public const int N = 1, E = 2, S = 4, W = 8;",
        "",
    ]


ZUGRIFF = '''        public struct Entry
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
}'''


def main():
    teile = json.load(open(STRASSEN, encoding="utf-8"))
    # 00_beispiel_viertel ist nur zum Anschauen und gehoert nicht in den Index
    teile = [t for t in teile if not t["slug"].startswith("00_")]
    teile.sort(key=lambda t: t["slug"])

    index = {t["slug"][:2]: i for i, t in enumerate(teile)}
    fehlt = sorted(k for k, v in NAMEN.items() if v not in index)
    if fehlt:
        sys.exit("Kein Modell gefunden fuer: " + ", ".join(fehlt))

    L = kopf(len(teile))
    A = L.append

    A("        // --- Gebaeude, die der Generator namentlich braucht ---")
    for name, i in HAUS_NAMEN:
        A("        public const int %s = %d;" % (name, i))
    A("")
    A("        // --- Strassen-, Bruecken- und Gelaendeteile ---")
    breite = max(len(k) for k in NAMEN)
    for name, pref in NAMEN.items():
        i = index[pref]
        A("        public const int %s = RoadBase + %2d;   // %s"
          % (name.ljust(breite), i, teile[i]["slug"]))
    A("")
    A("        static readonly float[] HouseHeight = { %s };"
      % ", ".join("%gf" % h for h in HAUS_HOEHE))
    A("")

    A("        static readonly string[] RoadSlugs =")
    A("        {")
    for i in range(0, len(teile), 4):
        A("            " + " ".join('"%s",' % t["slug"] for t in teile[i:i + 4]))
    A("        };")
    A("")

    A("        /// <summary>Kachel X, Kachel Z, Hoehe des Modells.</summary>")
    A("        static readonly float[,] RoadSize =")
    A("        {")
    for t in teile:
        lot = t.get("lot") or [t["size"][0], t["size"][2]]
        A("            {%gf,%gf,%gf},   // %s" % (lot[0], lot[1], t["size"][1], t["slug"]))
    A("        };")
    A("")

    A("        /// <summary>Anschlussmaske je Kachel, in Unity-Richtungen.</summary>")
    A("        static readonly int[] RoadLinks =")
    A("        {")
    for i in range(0, len(teile), 8):
        A("            " + " ".join("%2d," % maske(t["anchors"]) for t in teile[i:i + 8]))
    A("        };")
    A("")

    A("        /// <summary>")
    A("        /// Die Anschlusspunkte jeder Kachel, in Unity-Koordinaten relativ")
    A("        /// zum Pivot: Name, Position, Blickrichtung in Grad.")
    A("        ///")
    A("        /// Hieran wird verkettet. Zwei Teile passen, wenn ihre Anker")
    A("        /// aufeinanderliegen und ihre Richtungen 180 Grad auseinander")
    A("        /// stehen. Ohne diese Tabelle gibt es Luecken, weil Kachelmitte")
    A("        /// und Anschluss bei Boegen, Rampen und Abfahrten nicht")
    A("        /// zusammenfallen.")
    A("        /// </summary>")
    A("        public struct Anchor")
    A("        {")
    A("            public string name;")
    A("            public Vector3 pos;")
    A("            public float yaw;")
    A("        }")
    A("")
    A("        static readonly Anchor[][] RoadAnchors =")
    A("        {")
    for t in teile:
        eintrag = []
        for name, werte in t["anchors"].items():
            x, y, z, yaw = werte
            eintrag.append('new Anchor { name = "%s", pos = new Vector3(%gf, %gf, %gf), yaw = %gf }'
                           % (name, -x, y, z, -yaw))
        A("            // %s" % t["slug"])
        if eintrag:
            A("            new[] { " + ", ".join(eintrag) + " },")
        else:
            A("            new Anchor[0],")
    A("        };")
    A("")
    A(ZUGRIFF)

    open(ZIEL, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")

    print("%d Strassenteile -> %s" % (len(teile), os.path.relpath(ZIEL, ROOT)))
    for i, t in enumerate(teile):
        m = maske(t["anchors"])
        flags = "".join(c for c, b in zip("NESW", (N, E, S, W)) if m & b) or "-"
        print("  %3d  %-34s %s" % (28 + i, t["slug"], flags))


if __name__ == "__main__":
    main()
