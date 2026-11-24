using System.Collections.Generic;
using UnityEngine;
using Rio.World;

namespace Rio.WorldGen
{
    /// <summary>
    /// Das Strassennetz, erst als Graph geplant und dann in Kacheln
    /// uebersetzt.
    ///
    /// Vorher lief es umgekehrt: fuer jede Rasterzelle wurde eine Kachel
    /// gesetzt und hinterher geprueft, ob der Platz frei war. War er nicht
    /// frei, fiel die Kachel aus - und riss ein Loch in eine Strasse, von dem
    /// niemand etwas mitbekam. Das Ergebnis waren 15.012 Kacheln in 12.924
    /// Teilnetzen; das groesste zusammenhaengende Stueck hatte 417 Kacheln.
    ///
    /// Jetzt ist der Graph die Wahrheit. Ein Knoten ist ein Stueck Strasse,
    /// eine Kante eine befahrbare Verbindung. Erst wenn der Graph steht - und
    /// auf einen Zusammenhang reduziert ist - werden Kacheln daraus. Welche
    /// Kachel, ergibt sich aus dem Grad des Knotens. Dadurch kann keine
    /// Strasse mehr im Nichts enden: wo wirklich Schluss ist, steht ein
    /// Wendehammer.
    ///
    /// Alle Viertel haben ihr eigenes gedrehtes Raster. Zwei verschieden
    /// gedrehte Raster lassen sich mit rechteckigen Kacheln nicht aneinander
    /// stossen - deshalb liegen die Viertel nicht aneinander, sondern werden
    /// durch Avenidas verbunden, die ueber Bogenkacheln die Richtung wechseln.
    /// </summary>
    public class RioStrassenplan
    {
        public const float Cell = RioHousePlanner.Cell;      // 12 m
        public const float Avenida = 24f;                    // Kachellaenge der Avenida

        public enum Art { Rua, Avenida }

        public struct Knoten
        {
            public Vector2 p;        // Weltposition der Kachelmitte
            public float yaw;        // Ausrichtung des lokalen Rasters hier
            public Art art;
            public int viertel;      // -1 fuer die Strecke zwischen den Vierteln
            public float h;          // Hoehe, spaeter gesetzt
        }

        readonly List<Knoten> _k = new List<Knoten>();
        readonly List<int> _kante = new List<int>();   // 4 Eintraege je Knoten: lokal N, E, S, W
        readonly RioWorldTerrain _t;
        readonly RioHousePlanner _planner;

        /// <summary>Je Viertel: Knotenindex pro Rasterzelle, fuer das Andocken.</summary>
        readonly Dictionary<int, int[]> _gitter = new Dictionary<int, int[]>();
        readonly Dictionary<int, (int w, int h, int nx, int nz)> _gitterMass =
            new Dictionary<int, (int, int, int, int)>();

        public int KnotenZahl => _k.Count;
        public Knoten Get(int i) => _k[i];
        public int Nachbar(int i, int richtung) => _kante[i * 4 + richtung];

        public int Verbindungen { get; private set; }
        public int Wendehammer { get; private set; }
        public int Entfernt { get; private set; }
        public int AvenidaKacheln { get; private set; }

        public RioStrassenplan(RioWorldTerrain t, RioHousePlanner planner)
        {
            _t = t;
            _planner = planner;
        }

        // ------------------------------------------------------------------
        // Knoten und Kanten
        // ------------------------------------------------------------------

        int NeuerKnoten(Vector2 p, float yaw, Art art, int viertel)
        {
            _k.Add(new Knoten { p = p, yaw = yaw, art = art, viertel = viertel, h = 0f });
            _kante.Add(-1); _kante.Add(-1); _kante.Add(-1); _kante.Add(-1);
            return _k.Count - 1;
        }

        /// <summary>
        /// Verbindet zwei Knoten. <paramref name="richtung"/> ist die lokale
        /// Richtung von a aus gesehen (0 = +Z, 1 = +X, 2 = -Z, 3 = -X); bei b
        /// wird die Gegenrichtung eingetragen.
        /// </summary>
        void Verbinde(int a, int richtung, int b)
        {
            if (a < 0 || b < 0) return;
            _kante[a * 4 + richtung] = b;
            _kante[b * 4 + ((richtung + 2) & 3)] = a;
            Verbindungen++;
        }

        public int Grad(int i)
        {
            int g = 0;
            for (int r = 0; r < 4; r++) if (_kante[i * 4 + r] >= 0) g++;
            return g;
        }

        int Maske(int i)
        {
            int m = 0;
            if (_kante[i * 4 + 0] >= 0) m |= RioModelCatalog.N;
            if (_kante[i * 4 + 1] >= 0) m |= RioModelCatalog.E;
            if (_kante[i * 4 + 2] >= 0) m |= RioModelCatalog.S;
            if (_kante[i * 4 + 3] >= 0) m |= RioModelCatalog.W;
            return m;
        }

        // ------------------------------------------------------------------
        // Viertel
        // ------------------------------------------------------------------

        /// <summary>
        /// Legt in jedem Viertel ein Strassenraster an.
        ///
        /// Das Muster ist wie vorher: Linien im Abstand der Viertelperiode,
        /// plus eine Ringstrasse am Rand, damit keine Querstrasse ins Feld
        /// laeuft. Neu ist, dass daraus Knoten und Kanten werden und nicht
        /// direkt Kacheln - und dass das Ergebnis auf einen Zusammenhang
        /// reduziert wird.
        /// </summary>
        public void Viertel()
        {
            for (int di = 0; di < RioDistricts.All.Length; di++)
                Viertel(di);
        }

        void Viertel(int di)
        {
            var d = RioDistricts.All[di];
            RioDistricts.Raster(d.Type, out int px, out int pz);

            int nx = Mathf.CeilToInt(d.A / Cell) + 2;
            int nz = Mathf.CeilToInt(d.B / Cell) + 2;
            int w = nx * 2 + 1, h = nz * 2 + 1;

            var gueltig = new bool[w * h];
            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                var welt = Welt(d, ix - nx, iz - nz);
                if (RioDistricts.Norm(d, welt) > 1f) continue;
                if (RioDistricts.IndexAt(welt) != di) continue;
                if (!_planner.IstStadt(welt)) continue;
                gueltig[iz * w + ix] = true;
            }

            var strasse = new bool[w * h];
            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                if (!gueltig[iz * w + ix]) continue;
                bool linie = (ix - nx) % px == 0 || (iz - nz) % pz == 0;
                bool rand = !Hole(gueltig, w, h, ix + 1, iz) || !Hole(gueltig, w, h, ix - 1, iz)
                         || !Hole(gueltig, w, h, ix, iz + 1) || !Hole(gueltig, w, h, ix, iz - 1);
                strasse[iz * w + ix] = linie || rand;
            }

            var knoten = new int[w * h];
            for (int i = 0; i < knoten.Length; i++) knoten[i] = -1;

            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                if (!strasse[iz * w + ix]) continue;
                knoten[iz * w + ix] = NeuerKnoten(Welt(d, ix - nx, iz - nz), d.Rotation, Art.Rua, di);
            }

            // Kanten nur nach +Z und +X, damit jede Kante einmal entsteht
            for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
            {
                int a = knoten[iz * w + ix];
                if (a < 0) continue;
                if (iz + 1 < h) Verbinde(a, 0, knoten[(iz + 1) * w + ix]);
                if (ix + 1 < w) Verbinde(a, 1, knoten[iz * w + ix + 1]);
            }

            _gitter[di] = knoten;
            _gitterMass[di] = (w, h, nx, nz);
        }

        static Vector2 Welt(in RioDistricts.District d, int lx, int lz) =>
            RioDistricts.ToWorld(d, new Vector2(lx * Cell, lz * Cell));

        static bool Hole(bool[] f, int w, int h, int x, int z) =>
            x >= 0 && z >= 0 && x < w && z < h && f[z * w + x];

        // ------------------------------------------------------------------
        // Avenidas zwischen den Vierteln
        // ------------------------------------------------------------------

        /// <summary>
        /// Verbindet die Viertel paarweise mit Avenidas.
        ///
        /// Start und Ziel sind echte Randknoten der beiden Viertelraster, also
        /// Punkte, die zum lokalen Netz gehoeren. Dazwischen wird gegriffen,
        /// was der Baukasten hergibt: gerade Kacheln und Boegen mit 15 und 30
        /// Grad. Weil jeder Bogen ueber seine Anker angedockt wird, entsteht
        /// dabei keine Luecke - auch nicht an den Richtungswechseln, an denen
        /// der alte Router einfach um 20 Grad abknickte.
        /// </summary>
        public void AvenidasVerbinden(List<RioHouseWorld.Placement> ziel)
        {
            var belegt = new HashSet<long>();      // Knoten/Richtung schon benutzt
            int n = RioDistricts.Verbindungen.GetLength(0);

            for (int e = 0; e < n; e++)
            {
                int ia = RioDistricts.Verbindungen[e, 0];
                int ib = RioDistricts.Verbindungen[e, 1];
                StreckeBauen(ia, ib, belegt, ziel);
            }
        }

        /// <summary>
        /// Wo der Uebergang Rua -> Avenida landet, wenn er an einen Randknoten
        /// angedockt wird - ohne ihn zu setzen.
        ///
        /// Die Uebergangskachel dockt mit ihrem Rua-Anschluss an die freie
        /// Seite des Rasterknotens an und zeigt mit ihrem Avenida-Anschluss
        /// nach draussen. Dieser Avenida-Anschluss ist der Punkt, an dem eine
        /// Strecke anfangen oder ankommen muss.
        /// </summary>
        bool UebergangPose(int knoten, int richtung, out Vector3 pivot, out float yaw,
                           out Vector3 aussen, out float aussenYaw)
        {
            pivot = default; yaw = 0f; aussen = default; aussenYaw = 0f;

            var kn = _k[knoten];
            var dir = LokalZuWelt(kn.yaw, richtung);
            float dirYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            // Dort sitzt der Anschluss der Rasterkachel: halbe Zelle nach aussen
            var fuge = new Vector3(kn.p.x + dir.x * Cell * 0.5f, kn.h,
                                   kn.p.y + dir.y * Cell * 0.5f);

            if (!RioStrassennetz.Andocken(RioModelCatalog.AvenidaRuaUebergang, "Anschluss_Rua_N",
                                          fuge, Mathf.Repeat(dirYaw + 180f, 360f), out pivot, out yaw))
                return false;

            return RioStrassennetz.AnderesEnde(RioModelCatalog.AvenidaRuaUebergang,
                                               "Anschluss_Rua_N", pivot, yaw, out aussen, out aussenYaw);
        }

        /// <summary>
        /// Setzt den Uebergang und haengt ihn als echte Kante in den Graphen.
        ///
        /// Die Kante ist der Punkt: der Randknoten wurde ja gerade deshalb
        /// gewaehlt, weil dort noch keine Kante lag - und ohne Kante hat seine
        /// Rasterkachel in diese Richtung auch keinen Anschluss. Vorher hingen
        /// deshalb 24 von 25 Uebergaengen neben der Strasse.
        /// </summary>
        void UebergangSetzen(int knoten, int richtung, Vector3 pivot, float yaw,
                             List<RioHouseWorld.Placement> ziel)
        {
            int stummel = NeuerKnoten(new Vector2(pivot.x, pivot.z), yaw, Art.Avenida, -1);
            Verbinde(knoten, richtung, stummel);

            var kns = _k[stummel];
            kns.h = _t.SampleHeight(kns.p) + 0.05f;
            _k[stummel] = kns;

            pivot.y = kns.h;
            ziel.Add(new RioHouseWorld.Placement
            {
                position = pivot,
                yaw = yaw,
                model = RioModelCatalog.AvenidaRuaUebergang,
                colorSeed = RioHousePlanner.Hash((int)pivot.x, (int)pivot.z)
            });
            _planner.Occupy(new Vector2(pivot.x, pivot.z),
                            RioHousePlanner.Lot(RioModelCatalog.AvenidaRuaUebergang), yaw);
        }

        /// <summary>
        /// Haengt einen Knoten ohne eigene Kachel an einen Rasterknoten.
        ///
        /// Damit bekommt die Rasterkachel am Viertelrand ihren Anschluss in
        /// diese Richtung - sie war ja gerade deshalb ausgewaehlt, weil dort
        /// noch keine Kante lag. Die Kachel, die dort wirklich steht, kommt
        /// von der Verbindungsstrecke.
        /// </summary>
        void Stummel(int knoten, int richtung)
        {
            var kn = _k[knoten];
            var dir = LokalZuWelt(kn.yaw, richtung);
            int s2 = NeuerKnoten(kn.p + dir * Cell, kn.yaw, Art.Avenida, -1);
            Verbinde(knoten, richtung, s2);
            var k2 = _k[s2];
            k2.h = _t.SampleHeight(k2.p) + 0.05f;
            _k[s2] = k2;
        }

        struct Andockstelle
        {
            public int knoten;
            public int richtung;
            public Vector3 pivot;     // wohin die Uebergangskachel kommt
            public float yaw;
            public Vector3 pose;      // der Avenida-Anschluss nach draussen
            public float poseYaw;
        }

        /// <summary>
        /// Alle Stellen, an denen eine Avenida ein Viertel erreichen kann:
        /// Randknoten mit einer freien Richtung, die ungefaehr nach draussen
        /// zeigt.
        ///
        /// Dass es viele sind, ist der entscheidende Punkt. Ein einzelner
        /// vorgegebener Zielpunkt ist mit 24-m-Geraden und Boegen in 15er
        /// Schritten praktisch nie exakt erreichbar - die erreichbaren Posen
        /// liegen diskret. Mit hundert zulaessigen Zielen trifft die Suche
        /// dagegen fast immer eines genau, und dann passen die Anker ohne
        /// Luecke.
        /// </summary>
        List<Andockstelle> Andockstellen(int di, Vector2 herkunft, HashSet<long> belegt, bool rua)
        {
            var liste = new List<Andockstelle>();
            if (!_gitter.TryGetValue(di, out var gitter)) return liste;

            var mitte = RioDistricts.All[di].Center;

            foreach (int k in gitter)
            {
                if (k < 0) continue;
                var kn = _k[k];
                if (kn.viertel == -2) continue;
                if (Grad(k) == 0 || Grad(k) >= 4) continue;

                for (int r = 0; r < 4; r++)
                {
                    if (_kante[k * 4 + r] >= 0) continue;
                    if (belegt.Contains(((long)k << 3) | (uint)r)) continue;

                    var dir = LokalZuWelt(kn.yaw, r);
                    // Nach draussen heisst: weg von der Viertelmitte
                    if (Vector2.Dot(dir, (kn.p - mitte).normalized) < 0.25f) continue;
                    // und grob in Richtung der Herkunft
                    if (Vector2.Dot(dir, (herkunft - kn.p).normalized) < 0f) continue;

                    if (rua)
                    {
                        // Ohne Uebergangskachel: die Strasse faengt direkt an
                        // der Fuge der Rasterkachel an, eine halbe Zelle
                        // ausserhalb des Knotens.
                        float dirYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                        liste.Add(new Andockstelle
                        {
                            knoten = k, richtung = r,
                            pivot = new Vector3(kn.p.x, kn.h, kn.p.y), yaw = dirYaw,
                            pose = new Vector3(kn.p.x + dir.x * Cell * 0.5f, kn.h,
                                               kn.p.y + dir.y * Cell * 0.5f),
                            poseYaw = dirYaw
                        });
                        continue;
                    }

                    if (!UebergangPose(k, r, out Vector3 pivot, out float yaw,
                                       out Vector3 pose, out float poseYaw))
                        continue;

                    liste.Add(new Andockstelle
                    {
                        knoten = k, richtung = r,
                        pivot = pivot, yaw = yaw,
                        pose = pose, poseYaw = poseYaw
                    });
                }
            }
            return liste;
        }

        /// <summary>Lokale Rasterrichtung in eine Weltrichtung.</summary>
        public static Vector2 LokalZuWelt(float yaw, int richtung)
        {
            float grad = yaw + richtung * 90f;
            float rad = grad * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }

        /// <summary>
        /// Die Teile, aus denen eine Avenida-Strecke besteht.
        ///
        /// Ein Bogen, der ueber seinen Ende-Anker angedockt wird, dreht in die
        /// andere Richtung - deshalb genuegt je Winkel ein Modell, so wie es
        /// die Anleitung zum Baukasten beschreibt.
        /// </summary>
        static readonly (int model, string anker)[] AvenidaTeile =
        {
            (RioModelCatalog.AvenidaStraight, "Anschluss_Avenida_S"),
            (RioModelCatalog.AvenidaBogen15,  "Anschluss_Avenida_S"),
            (RioModelCatalog.AvenidaBogen15,  "Anschluss_Avenida_Ende"),
            (RioModelCatalog.AvenidaBogen30,  "Anschluss_Avenida_S"),
            (RioModelCatalog.AvenidaBogen30,  "Anschluss_Avenida_Ende"),
        };

        /// <summary>
        /// Dasselbe eine Nummer kleiner: 12-m-Geraden statt 24-m, und Boegen
        /// mit 40 m Radius statt 60 m.
        ///
        /// Fuer kurze Verbindungen zwischen Nachbarvierteln ist das die
        /// richtige Wahl. Eine kurze Kette kann kaum Restfehler aufnehmen -
        /// die Gelenke haben zu wenig Hebel - und mit dem feineren Raster
        /// trifft die Suche den Gegenanschluss viel oefter von selbst. Dass
        /// zwei benachbarte Viertel durch eine normale Strasse verbunden sind
        /// und nicht durch eine vierspurige Avenida, ist ausserdem das, was
        /// man in Rio sieht.
        /// </summary>
        static readonly (int model, string anker)[] RuaTeile =
        {
            (RioModelCatalog.RoadStraight, "Anschluss_Rua_S"),
            (RioModelCatalog.RuaBogen15,   "Anschluss_Rua_S"),
            (RioModelCatalog.RuaBogen15,   "Anschluss_Rua_Ende"),
            (RioModelCatalog.RuaBogen30,   "Anschluss_Rua_S"),
            (RioModelCatalog.RuaBogen30,   "Anschluss_Rua_Ende"),
        };

        /// <summary>Bis zu dieser Luftlinie wird mit Rua verbunden, darueber mit Avenida.</summary>
        public const float RuaGrenze = 420f;

        struct Zustand
        {
            public Vector3 pos;      // offenes Ende
            public float yaw;        // Richtung des offenen Endes
            public float weg;        // bisherige Streckenlaenge
            public int vorher;       // Index des Vorgaengers, -1 am Anfang
            public int teil;         // welches Teil hierher gefuehrt hat
        }

        const int Strahl = 28;       // so viele Teilstrecken bleiben je Schritt
        const int MaxSchritte = 150;

        /// <summary>
        /// Legt eine Avenida zwischen zwei Vierteln.
        ///
        /// Gesucht wird mit einer Strahlsuche ueber die Teile des Baukastens.
        /// Ziel ist nicht ein Punkt, sondern die ganze Menge der Andockstellen
        /// des Zielviertels - siehe <see cref="Andockstellen"/>, dort steht
        /// auch, warum.
        ///
        /// Gelegt wird erst, wenn die Suche einen Anschluss exakt getroffen
        /// hat. Eine halbe Avenida, die im Feld aufhoert, waere schlechter als
        /// keine.
        /// </summary>
        void StreckeBauen(int ia, int ib, HashSet<long> belegt,
                          List<RioHouseWorld.Placement> ziel)
        {
            var va = RioDistricts.All[ia];
            var vb = RioDistricts.All[ib];

            // Beide Bauweisen probieren, in der Reihenfolge, die zur
            // Entfernung passt - und nehmen, was zusammengeht. Eine feste
            // Schwelle waere Raten: ob eine Kette den Gegenanschluss trifft,
            // haengt nicht an der Luftlinie, sondern daran, wie die beiden
            // Raster zueinander stehen.
            for (int modus = 0; modus < 2; modus++)
                if (Strecke(ia, ib, modus == 1, belegt, ziel)) return;
            OffeneStrecken++;
        }

        bool Strecke(int ia, int ib, bool rua, HashSet<long> belegt,
                     List<RioHouseWorld.Placement> ziel)
        {
            var va = RioDistricts.All[ia];
            var vb = RioDistricts.All[ib];
            var teile = rua ? RuaTeile : AvenidaTeile;

            var starts = Andockstellen(ia, vb.Center, belegt, rua);
            var ziele = Andockstellen(ib, va.Center, belegt, rua);
            if (starts.Count == 0 || ziele.Count == 0) return false;

            // Der Start, der dem Zielviertel am naechsten liegt, zuerst
            starts.Sort((x, y) =>
                Vector2.Distance(new Vector2(x.pose.x, x.pose.z), vb.Center).CompareTo(
                Vector2.Distance(new Vector2(y.pose.x, y.pose.z), vb.Center)));

            // Zielposen in ein Raster, damit die Suche je Schritt in
            // konstanter Zeit pruefen kann, ob sie angekommen ist.
            var raster = new Dictionary<long, List<int>>(ziele.Count * 2);
            for (int i = 0; i < ziele.Count; i++)
            {
                long key = Raster2(ziele[i].pose);
                if (!raster.TryGetValue(key, out var l)) raster[key] = l = new List<int>(2);
                l.Add(i);
            }

            // Mehrere Startstellen probieren und die beste nehmen, nicht die
            // erste: die erreichbaren Posen liegen diskret, und wie gut eine
            // Kette am Ende zusammengeht, sieht man erst nach der
            // Nachjustierung.
            List<int> besteKette = null;
            List<Zustand> besteAlle = null;
            float[] besteGelenk = null;
            Andockstelle besterStart = default, bestesEnde = default;
            float bestesMass = float.MaxValue;

            for (int versuch = 0; versuch < 6 && versuch < starts.Count; versuch++)
            {
                var start = starts[versuch];
                if (!Suche(start, ziele, raster, teile,
                           out List<int> kette, out List<Zustand> alle, out int treffer))
                    continue;

                var ende = ziele[treffer];

                // Die Suche kommt grob an. Den Rest nehmen winzige
                // Gelenkwinkel an den Fugen auf.
                var gelenk = new float[kette.Count];
                float genau = Nachjustieren(alle, kette, start.pose, start.poseYaw, gelenk,
                                            ende.pose, Mathf.Repeat(ende.poseYaw + 180f, 360f),
                                            teile);

                // Restfehler zaehlt, Knick zaehlt mit
                float mass = genau + MaxBetrag(gelenk) * 0.5f;
                if (mass >= bestesMass) continue;

                bestesMass = mass;
                besteKette = kette; besteAlle = alle; besteGelenk = gelenk;
                besterStart = start; bestesEnde = ende;

                if (genau < 0.05f) break;         // besser wird es nicht
            }

            // 25 cm ist weniger als die Toleranz der Ankerpruefung; darueber
            // bliebe eine sichtbare Fuge, und dann ist keine Avenida besser
            // als eine mit Loch.
            if (besteKette == null || bestesMass > 0.25f + MaxKnick * 0.5f)
                return false;

            if (rua)
            {
                Stummel(besterStart.knoten, besterStart.richtung);
                Stummel(bestesEnde.knoten, bestesEnde.richtung);
            }
            else
            {
                UebergangSetzen(besterStart.knoten, besterStart.richtung,
                                besterStart.pivot, besterStart.yaw, ziel);
                UebergangSetzen(bestesEnde.knoten, bestesEnde.richtung,
                                bestesEnde.pivot, bestesEnde.yaw, ziel);
            }

            Vector3 pp = besterStart.pose;
            float yy = besterStart.poseYaw;
            for (int i = 0; i < besteKette.Count; i++)
            {
                var z = besteAlle[besteKette[i]];
                float dock = yy + besteGelenk[i];
                if (Lege(teile[z.teil].model, teile[z.teil].anker, pp, dock, ziel, ref pp, ref yy))
                    AvenidaKacheln++;
            }
            GroessterKnick = Mathf.Max(GroessterKnick, MaxBetrag(besteGelenk));

            belegt.Add(((long)besterStart.knoten << 3) | (uint)besterStart.richtung);
            belegt.Add(((long)bestesEnde.knoten << 3) | (uint)bestesEnde.richtung);
            GeschlosseneStrecken++;
            if (rua) RuaStrecken++; else AvenidaStrecken++;
            return true;
        }

        public int RuaStrecken { get; private set; }
        public int AvenidaStrecken { get; private set; }

        /// <summary>Wie grob die Suche ankommen darf; den Rest machen die Gelenke.</summary>
        public const float Fangweite = 10f;
        public const float FangWinkel = 15f;

        /// <summary>
        /// Was ein einzelnes Gelenk hoechstens aufnehmen darf, in Grad.
        /// Unter einem Grad sieht man die Fuge nicht; darueber wird daraus
        /// ein Knick in der Fahrbahn.
        /// </summary>
        public const float MaxKnick = 1.1f;

        /// <summary>Groesster Knick, der dabei entstanden ist, in Grad.</summary>
        public float GroessterKnick { get; private set; }

        static float MaxBetrag(float[] a)
        {
            float m = 0f;
            foreach (float x in a) m = Mathf.Max(m, Mathf.Abs(x));
            return m;
        }

        /// <summary>
        /// Laeuft eine gefundene Kette ab und liefert, wo sie endet.
        ///
        /// <paramref name="gelenk"/> ist ein kleiner Zusatzwinkel je Fuge. Er
        /// dreht das jeweils folgende Teil um seinen eigenen Anschlusspunkt -
        /// die Anker liegen danach weiter genau aufeinander, es entsteht also
        /// keine Luecke, nur ein Knick.
        /// </summary>
        bool Abspielen(List<Zustand> alle, List<int> kette, Vector3 p0, float y0,
                       float[] gelenk, Vector3[] fugen, (int model, string anker)[] teile,
                       out Vector3 endePos, out float endeYaw)
        {
            endePos = p0; endeYaw = y0;
            for (int i = 0; i < kette.Count; i++)
            {
                if (fugen != null) fugen[i] = endePos;
                var z = alle[kette[i]];
                float dock = endeYaw + (gelenk != null ? gelenk[i] : 0f);
                if (!Probe(teile[z.teil].model, teile[z.teil].anker, endePos, dock,
                           out _, out _, out Vector3 np, out float ny))
                    return false;
                endePos = np; endeYaw = ny;
            }
            return true;
        }

        /// <summary>
        /// Verteilt den Restfehler einer Kette auf die Gelenkwinkel, bis das
        /// Ende genau auf der Zielpose sitzt. Liefert den verbleibenden
        /// Fehler in Metern.
        ///
        /// Das ist ein Gauss-Newton-Schritt auf drei Gleichungen (x, z,
        /// Richtung) mit so vielen Unbekannten, wie die Kette Fugen hat.
        /// Weil es viel mehr Unbekannte als Gleichungen gibt, bleibt pro Fuge
        /// ein winziger Winkel uebrig - bei 30 Fugen und 8 m Restweg sind das
        /// Bruchteile eines Grads. Die Alternative waere, die Kacheln gegen-
        /// einander zu verschieben, und das sind genau die Luecken, die im
        /// Spiel auffallen.
        /// </summary>
        float Nachjustieren(List<Zustand> alle, List<int> kette, Vector3 p0, float y0,
                            float[] gelenk, Vector3 zielPos, float zielYaw,
                            (int model, string anker)[] teile)
        {
            int n = kette.Count;
            if (n == 0) return 0f;
            var fugen = new Vector3[n];

            float rest = float.MaxValue;

            for (int iter = 0; iter < 12; iter++)
            {
                if (!Abspielen(alle, kette, p0, y0, gelenk, fugen, teile,
                               out Vector3 e, out float ey))
                    return float.MaxValue;

                float ex = zielPos.x - e.x;
                float ez = zielPos.z - e.z;
                float ea = Mathf.DeltaAngle(ey, zielYaw) * Mathf.Deg2Rad;

                rest = Mathf.Sqrt(ex * ex + ez * ez);
                if (rest < 0.02f && Mathf.Abs(ea) < 0.0008f) return rest;

                // J (3 x n): Ableitung der Endpose nach jedem Gelenkwinkel.
                // Ein Gelenk dreht alles dahinter um seinen Punkt, also
                // bewegt sich das Ende senkrecht zum Hebelarm.
                // J J^T als 3 x 3 aufsummieren.
                float a00 = 0f, a01 = 0f, a02 = 0f, a11 = 0f, a12 = 0f, a22 = 0f;
                for (int j = 0; j < n; j++)
                {
                    float vx = e.x - fugen[j].x;
                    float vz = e.z - fugen[j].z;
                    float j0 = vz, j1 = -vx, j2 = 1f;
                    a00 += j0 * j0; a01 += j0 * j1; a02 += j0 * j2;
                    a11 += j1 * j1; a12 += j1 * j2; a22 += j2 * j2;
                }

                // Daempfung, damit das Gleichungssystem nicht entartet
                float lam = 1e-3f * (a00 + a11 + a22) + 1e-6f;
                a00 += lam; a11 += lam; a22 += lam;

                if (!Loese3(a00, a01, a02, a11, a12, a22, ex, ez, ea,
                            out float l0, out float l1, out float l2))
                    return rest;

                for (int j = 0; j < n; j++)
                {
                    float vx = e.x - fugen[j].x;
                    float vz = e.z - fugen[j].z;
                    float dh = vz * l0 + (-vx) * l1 + 1f * l2;
                    // Begrenzen. Eine kurze Kette kann nicht viel aufnehmen,
                    // und ein Knick von 28 Grad an einer Fuge ist genau der
                    // Bruch, den man im Spiel sieht. Was nicht hineinpasst,
                    // bleibt als Restfehler stehen - dann wird die Strecke
                    // verworfen statt verbogen.
                    gelenk[j] = Mathf.Clamp(gelenk[j] + dh * Mathf.Rad2Deg,
                                            -MaxKnick, MaxKnick);
                }
            }
            return rest;
        }

        /// <summary>Loest ein symmetrisches 3x3-System. false, wenn entartet.</summary>
        static bool Loese3(float a00, float a01, float a02, float a11, float a12, float a22,
                           float b0, float b1, float b2,
                           out float x0, out float x1, out float x2)
        {
            float c00 = a11 * a22 - a12 * a12;
            float c01 = a02 * a12 - a01 * a22;
            float c02 = a01 * a12 - a02 * a11;
            float det = a00 * c00 + a01 * c01 + a02 * c02;

            x0 = x1 = x2 = 0f;
            if (Mathf.Abs(det) < 1e-12f) return false;

            float c11 = a00 * a22 - a02 * a02;
            float c12 = a02 * a01 - a00 * a12;
            float c22 = a00 * a11 - a01 * a01;
            float inv = 1f / det;

            x0 = (c00 * b0 + c01 * b1 + c02 * b2) * inv;
            x1 = (c01 * b0 + c11 * b1 + c12 * b2) * inv;
            x2 = (c02 * b0 + c12 * b1 + c22 * b2) * inv;
            return true;
        }

        static long Raster2(Vector3 p) =>
            ((long)Mathf.FloorToInt(p.x / 4f) << 24) ^ (uint)Mathf.FloorToInt(p.z / 4f);

        /// <summary>
        /// Die eigentliche Strahlsuche. Liefert die Kette der Teile und welche
        /// Andockstelle getroffen wurde.
        /// </summary>
        bool Suche(in Andockstelle start, List<Andockstelle> ziele,
                   Dictionary<long, List<int>> raster, (int model, string anker)[] teile,
                   out List<int> kette, out List<Zustand> alle, out int treffer)
        {
            kette = null; treffer = -1;
            alle = new List<Zustand>(Strahl * 8);
            alle.Add(new Zustand
            {
                pos = start.pose, yaw = start.poseYaw, weg = 0f, vorher = -1, teil = -1
            });

            var schicht = new List<int> { 0 };
            var naechste = new List<(float kosten, int index)>(Strahl * 8);

            for (int schritt = 0; schritt < MaxSchritte; schritt++)
            {
                naechste.Clear();

                foreach (int zi in schicht)
                {
                    var z = alle[zi];

                    for (int c = 0; c < teile.Length; c++)
                    {
                        if (!Probe(teile[c].model, teile[c].anker, z.pos, z.yaw,
                                   out Vector3 pivot, out _, out Vector3 neuPos, out float neuYaw))
                            continue;

                        var mitte = new Vector2(pivot.x, pivot.z);
                        if (!_t.IsLand(mitte)) continue;
                        if (_t.DistanceToCoast(mitte) < 16f) continue;

                        alle.Add(new Zustand
                        {
                            pos = neuPos, yaw = neuYaw,
                            weg = z.weg + Vector3.Distance(z.pos, neuPos),
                            vorher = zi, teil = c
                        });
                        int neu = alle.Count - 1;

                        // Angekommen? Grob genuegt - den Rest machen die
                        // Gelenke. Exakt zu treffen ist mit 24-m-Geraden und
                        // Boegen in 15er Schritten praktisch unmoeglich: die
                        // erreichbaren Posen liegen diskret.
                        if (Angekommen(neuPos, neuYaw, ziele, raster, out int gefunden))
                        {
                            treffer = gefunden;
                            kette = new List<int>();
                            for (int i = neu; i >= 0 && alle[i].vorher >= 0; i = alle[i].vorher)
                                kette.Add(i);
                            kette.Reverse();
                            return true;
                        }

                        // Auf die naechste Zielpose zusteuern, nicht auf die
                        // Viertelmitte: die Andockstellen liegen am Rand, und
                        // wer auf die Mitte zielt, laeuft an ihnen vorbei.
                        float d = float.MaxValue, dreh = 180f;
                        foreach (var zp in ziele)
                        {
                            float dd = Vector2.Distance(new Vector2(neuPos.x, neuPos.z),
                                                        new Vector2(zp.pose.x, zp.pose.z));
                            if (dd >= d) continue;
                            d = dd;
                            dreh = Mathf.Abs(Mathf.DeltaAngle(neuYaw + 180f, zp.poseYaw));
                        }
                        naechste.Add((d + dreh * 1.8f + z.weg * 0.08f, neu));
                    }
                }

                if (naechste.Count == 0) return false;

                naechste.Sort((x, y) => x.kosten.CompareTo(y.kosten));
                schicht.Clear();
                for (int i = 0; i < naechste.Count && i < Strahl; i++)
                    schicht.Add(naechste[i].index);
            }
            return false;
        }

        static bool Angekommen(Vector3 pos, float yaw, List<Andockstelle> ziele,
                               Dictionary<long, List<int>> raster, out int treffer)
        {
            treffer = -1;
            const int r = 3;        // Fangweite 10 m in einem 4-m-Raster
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                long key = Raster2(pos + new Vector3(dx * 4f, 0f, dz * 4f));
                if (!raster.TryGetValue(key, out var l)) continue;

                foreach (int i in l)
                {
                    var z = ziele[i];
                    if (Vector2.Distance(new Vector2(pos.x, pos.z),
                                         new Vector2(z.pose.x, z.pose.z)) > Fangweite) continue;
                    if (Mathf.Abs(Mathf.DeltaAngle(yaw + 180f, z.poseYaw)) > FangWinkel) continue;
                    treffer = i;
                    return true;
                }
            }
            return false;
        }

        public int OffeneStrecken { get; private set; }
        public int GeschlosseneStrecken { get; private set; }

        /// <summary>Rechnet nach, wo ein Teil landet, ohne es zu setzen.</summary>
        bool Probe(int model, string anker, Vector3 endePos, float endeYaw,
                   out Vector3 pivot, out float yaw, out Vector3 neuPos, out float neuYaw)
        {
            neuPos = endePos; neuYaw = endeYaw;
            if (!RioStrassennetz.Andocken(model, anker, endePos, Mathf.Repeat(endeYaw + 180f, 360f),
                                          out pivot, out yaw))
                return false;
            return RioStrassennetz.AnderesEnde(model, anker, pivot, yaw, out neuPos, out neuYaw);
        }

        bool Lege(int model, string anker, Vector3 endePos, float endeYaw,
                  List<RioHouseWorld.Placement> ziel, ref Vector3 neuPos, ref float neuYaw)
        {
            if (!Probe(model, anker, endePos, endeYaw, out Vector3 pivot, out float yaw,
                       out neuPos, out neuYaw))
                return false;

            pivot.y = _t.SampleHeight(new Vector2(pivot.x, pivot.z)) + 0.05f;
            ziel.Add(new RioHouseWorld.Placement
            {
                position = pivot,
                yaw = yaw,
                model = model,
                colorSeed = RioHousePlanner.Hash((int)pivot.x, (int)pivot.z)
            });
            // Belegen, sonst setzt der Hausplaner Haeuser auf die Strecke
            _planner.Occupy(new Vector2(pivot.x, pivot.z), RioHousePlanner.Lot(model), yaw);
            return true;
        }

        // ------------------------------------------------------------------
        // Aufraeumen
        // ------------------------------------------------------------------

        /// <summary>Kleiner als das ist kein Viertel, sondern ein Rest.</summary>
        public const int MinNetz = 25;

        /// <summary>
        /// Wirft Splitter weg: jedes Teilnetz mit weniger als
        /// <see cref="MinNetz"/> Knoten.
        ///
        /// Ein Viertelraster zerfaellt in Stuecke, wo das Gelaende nicht
        /// bebaubar ist - eine Bucht, ein Steilhang, der Rand der Ellipse.
        /// Die kleinen Stuecke sind im Spiel nicht erreichbar und erzeugen
        /// genau die Strassenstummel, die im Feld stehen.
        ///
        /// Nur das groesste Teilnetz zu behalten waere falsch: die Viertel
        /// haengen im Graphen nicht zusammen, sie werden erst durch die
        /// Avenidas verbunden. Ein globales Maximum liess 16 von 17 Vierteln
        /// verschwinden.
        /// </summary>
        public void AufEinNetzReduzieren()
        {
            var komp = new int[_k.Count];
            for (int i = 0; i < komp.Length; i++) komp[i] = -1;

            var groesse = new List<int>();
            var stapel = new Stack<int>();

            for (int start = 0; start < _k.Count; start++)
            {
                if (komp[start] >= 0) continue;
                int id = groesse.Count;
                int g = 0;
                stapel.Push(start);
                komp[start] = id;

                while (stapel.Count > 0)
                {
                    int c = stapel.Pop();
                    g++;
                    for (int r = 0; r < 4; r++)
                    {
                        int nb = _kante[c * 4 + r];
                        if (nb < 0 || komp[nb] >= 0) continue;
                        komp[nb] = id;
                        stapel.Push(nb);
                    }
                }
                groesse.Add(g);
            }

            int beste = 0;
            for (int i = 1; i < groesse.Count; i++)
                if (groesse[i] > groesse[beste]) beste = i;

            // Knoten der kleinen Netze von ihren Kanten loesen und markieren
            for (int i = 0; i < _k.Count; i++)
            {
                if (groesse[komp[i]] >= MinNetz) continue;
                for (int r = 0; r < 4; r++)
                {
                    int nb = _kante[i * 4 + r];
                    if (nb >= 0) _kante[nb * 4 + ((r + 2) & 3)] = -1;
                    _kante[i * 4 + r] = -1;
                }
                var kn = _k[i];
                kn.viertel = -2;                 // -2 heisst: gestrichen
                _k[i] = kn;
                Entfernt++;
            }

            Teilnetze = groesse.Count;
            GroesstesNetz = groesse[beste];
        }

        public int Teilnetze { get; private set; }
        public int GroesstesNetz { get; private set; }

        // ------------------------------------------------------------------
        // Kacheln
        // ------------------------------------------------------------------

        /// <summary>
        /// Macht aus dem Graphen Kacheln.
        ///
        /// Die Kachel ergibt sich allein aus dem Grad des Knotens. Ein Knoten
        /// mit nur einem Nachbarn bekommt einen Wendehammer - damit endet
        /// keine Strasse mehr im Nichts, sondern an einer Wendeschleife, wie
        /// es sich fuer eine Sackgasse gehoert.
        /// </summary>
        public void Kacheln(List<RioHouseWorld.Placement> ziel, System.Random rng)
        {
            for (int i = 0; i < _k.Count; i++)
            {
                var kn = _k[i];
                if (kn.viertel == -2) continue;
                if (kn.art == Art.Avenida) continue;      // hat seine Kachel schon

                // Zwei Knoten mit Hoehenunterschied teilen sich eine
                // Steigungskachel. Gesetzt wird sie vom unteren der beiden.
                if (_partner != null && _partner[i] >= 0 && _stufe[_partner[i]] != _stufe[i])
                {
                    int nb = _partner[i];
                    if (_stufe[i] > _stufe[nb]) continue;              // der andere macht es
                    Steigungskachel(i, nb, ziel);
                    continue;
                }

                int maske = Maske(i);
                if (maske == 0) continue;

                if (!RioModelCatalog.FindTile(maske, out int model, out float q)) continue;

                if (model == RioModelCatalog.RoadSackgasse) Wendehammer++;
                else if (model == RioModelCatalog.RoadStraight &&
                         (RioHousePlanner.Hash((int)kn.p.x, (int)kn.p.y) % 13) == 0)
                    model = RioModelCatalog.RoadCobble;

                float yaw = q + kn.yaw;
                var pos = new Vector3(kn.p.x, kn.h, kn.p.y);

                // Ueber einen Anker einmessen statt ueber die Kachelmitte.
                // Bei den 12-m-Kacheln kommt dasselbe heraus, weil ihre
                // Anschluesse auf der halben Kachel sitzen. Beim Wendehammer
                // nicht: der ist 18 x 20 m und hat seinen Anschluss 10 m von
                // der Mitte, nicht 6 m. Ueber die Mitte gesetzt fehlten dort
                // 4 m - 894 Sackgassen hingen deshalb neben ihrer Strasse.
                int r0 = -1;
                for (int r = 0; r < 4; r++) if (_kante[i * 4 + r] >= 0) { r0 = r; break; }
                if (r0 >= 0)
                {
                    var dir = LokalZuWelt(kn.yaw, r0);
                    float dirYaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                    var fuge = new Vector3(kn.p.x + dir.x * Cell * 0.5f, kn.h,
                                           kn.p.y + dir.y * Cell * 0.5f);

                    // Der Anker zeigt aus der Kachel hinaus, also in Richtung
                    // des Nachbarn - nicht zu ihm zurueck.
                    string name = AnkerNach(model, yaw, dirYaw);
                    if (name != null &&
                        RioStrassennetz.Andocken(model, name, fuge, dirYaw,
                                                 out Vector3 pivot, out float yaw2))
                    {
                        pos = new Vector3(pivot.x, kn.h, pivot.z);
                        yaw = yaw2;
                    }
                }

                ziel.Add(new RioHouseWorld.Placement
                {
                    position = pos,
                    yaw = yaw,
                    model = model,
                    colorSeed = rng.Next(1, int.MaxValue)
                });

                // Die Strasse belegt ihren Platz, damit kein Haus hineinbaut
                _planner.Occupy(new Vector2(pos.x, pos.z), RioHousePlanner.Lot(model), yaw);
            }
        }

        /// <summary>
        /// Setzt die Steigungskachel zwischen einem unteren und einem oberen
        /// Knoten.
        ///
        /// Die Kachel deckt genau die beiden Rasterzellen ab. Angedockt wird
        /// an ihrem unteren Anschluss - der liegt auf y = 0, der obere auf
        /// y = 1,5 bzw. 3 m. Dadurch sitzen beide Enden genau auf der Hoehe
        /// ihres Knotens, und die Fahrbahn laeuft durch statt zu springen.
        /// </summary>
        void Steigungskachel(int unten, int oben, List<RioHouseWorld.Placement> ziel)
        {
            int d = Mathf.Abs(_stufe[oben] - _stufe[unten]);
            int model = d >= 2 ? RioModelCatalog.RuaSteigung : RioModelCatalog.RuaSteigungSanft;

            var u = _k[unten];
            var o = _k[oben];
            Vector2 richtung = (o.p - u.p).normalized;        // bergauf
            float yaw = Mathf.Atan2(richtung.x, richtung.y) * Mathf.Rad2Deg;

            // Der untere Anschluss sitzt eine halbe Zelle hinter dem unteren
            // Knoten, also am aeusseren Rand des Kachelpaars.
            var fuge = new Vector3(u.p.x - richtung.x * Cell * 0.5f, u.h,
                                   u.p.y - richtung.y * Cell * 0.5f);

            if (!RioStrassennetz.Andocken(model, "Anschluss_Rua_S", fuge,
                                          Mathf.Repeat(yaw + 180f, 360f),
                                          out Vector3 pivot, out float kachelYaw))
                return;

            pivot.y = u.h;
            ziel.Add(new RioHouseWorld.Placement
            {
                position = pivot,
                yaw = kachelYaw,
                model = model,
                colorSeed = RioHousePlanner.Hash((int)pivot.x, (int)pivot.z)
            });
            _planner.Occupy(new Vector2(pivot.x, pivot.z), RioHousePlanner.Lot(model), kachelYaw);
            SteigungKacheln++;
        }

        public int SteigungKacheln { get; private set; }

        /// <summary>
        /// Welcher Anschluss einer Kachel in eine Weltrichtung zeigt, wenn die
        /// Kachel um <paramref name="yaw"/> gedreht steht.
        /// </summary>
        static string AnkerNach(int model, float yaw, float richtungYaw)
        {
            foreach (var a in RioModelCatalog.Anchors(model))
            {
                if (!a.name.StartsWith("Anschluss_")) continue;
                if (Mathf.Abs(Mathf.DeltaAngle(yaw + a.yaw, richtungYaw)) < 20f) return a.name;
            }
            return null;
        }

        /// <summary>Hoehe je Knoten aus dem Gelaende, vor dem Hoehenausgleich.</summary>
        public void HoehenAusGelaende()
        {
            for (int i = 0; i < _k.Count; i++)
            {
                var kn = _k[i];
                kn.h = _t.SampleHeight(kn.p) + 0.05f;
                _k[i] = kn;
            }
        }

        // ------------------------------------------------------------------
        // Hoehen
        // ------------------------------------------------------------------

        int[] _stufe;        // Hoehe je Knoten in Vielfachen von RioStrassennetz.Stufe
        int[] _partner;      // Knoten am anderen Ende einer Steigungskachel, sonst -1

        public int Terrassen { get; private set; }
        public int Steigungen { get; private set; }
        public float GroessteStufe { get; private set; }
        /// <summary>Stufen ohne Steigungskachel - jede davon ist ein Bruch.</summary>
        public int OffeneStufen { get; private set; }

        /// <summary>
        /// Legt die Hoehen so fest, dass benachbarte Kacheln entweder genau
        /// gleich hoch liegen oder sich um eine Stufe unterscheiden, die eine
        /// Steigungskachel ueberbrueckt.
        ///
        /// Das ist die Ursache der schwebenden Objekte. Eine Kachel ist eine
        /// flache Platte; stand sie auf der Gelaendehoehe ihrer Mitte, dann
        /// schwebte sie auf der einen Seite und versank auf der anderen. Schon
        /// 2 Prozent Neigung ergeben auf 12 m eine Stufe von 24 cm, und genau
        /// diese Stufen sieht man im Spiel als Luecken - auf 89 Prozent der
        /// Strecke liegt die Neigung unter 2 Prozent, steile Haenge sind also
        /// nicht das Problem.
        ///
        /// Die Hoehen liegen danach auf einem Raster von 1,5 m - genau dem,
        /// was die Kachel "Steigung sanft" ueberwindet. Kreuzungen werden
        /// flach gehalten, wie in Wirklichkeit auch; ein Hoehenwechsel darf
        /// nur auf einem geraden Stueck passieren.
        /// </summary>
        public void HoehenAusgleichen()
        {
            int n = _k.Count;
            _stufe = new int[n];
            _partner = new int[n];
            var wunsch = new float[n];
            for (int i = 0; i < n; i++) wunsch[i] = _k[i].h;

            // Terrassen als Mengen von Knoten, die zwingend gleich hoch
            // liegen muessen. Zusammengelegt wird mit Union-Find; das ist der
            // Grund, warum das Verfahren terminiert: jede Runde, die einen
            // Hoehenwechsel nicht unterbringt, legt zwei Terrassen zusammen,
            // und zusammenlegen kann man nur endlich oft.
            //
            // Ein Durchlauf, der stattdessen einzelne Knoten einzieht, kreist:
            // das Einebnen einer Kante bringt die naechste aus dem Lot. In der
            // Messung blieben so 617 Stufen ohne Steigungskachel stehen - und
            // jede davon ist ein Bruch in der Fahrbahn.
            _vater = new int[n];
            for (int i = 0; i < n; i++) _vater[i] = i;

            // Kanten, an denen keine Steigungskachel stehen kann: Kreuzungen,
            // Kurven, Enden. Die muessen flach sein - was in Wirklichkeit auch
            // so ist.
            for (int i = 0; i < n; i++)
            for (int r = 0; r < 4; r++)
            {
                int nb = _kante[i * 4 + r];
                if (nb < 0 || nb < i) continue;
                if (!SteigungMoeglich(i, nb)) Vereinen(i, nb);
            }

            for (int runde = 0; runde < 40; runde++)
            {
                TerrassenHoehen(wunsch);

                for (int i = 0; i < n; i++) _partner[i] = -1;
                bool zusammengelegt = false;

                for (int i = 0; i < n; i++)
                for (int r = 0; r < 4; r++)
                {
                    int nb = _kante[i * 4 + r];
                    if (nb < 0 || nb < i) continue;

                    int d = _stufe[nb] - _stufe[i];
                    if (d == 0) continue;                       // gleiche Terrasse

                    bool geht = Mathf.Abs(d) <= 2
                             && SteigungMoeglich(i, nb)
                             && _partner[i] < 0 && _partner[nb] < 0;

                    if (geht) { _partner[i] = nb; _partner[nb] = i; continue; }

                    Vereinen(i, nb);
                    zusammengelegt = true;
                }

                if (!zusammengelegt) break;
            }

            // Zurueckschreiben
            var plateaus = new HashSet<int>();
            for (int i = 0; i < n; i++)
            {
                var kn = _k[i];
                kn.h = _stufe[i] * RioStrassennetz.Stufe;
                _k[i] = kn;
                plateaus.Add(_stufe[i]);
            }
            Terrassen = plateaus.Count;

            GroessteStufe = 0f;
            Steigungen = 0;
            OffeneStufen = 0;
            for (int i = 0; i < n; i++)
            for (int r = 0; r < 4; r++)
            {
                int nb = _kante[i * 4 + r];
                if (nb < 0 || nb < i) continue;
                int d = Mathf.Abs(_stufe[nb] - _stufe[i]);
                if (d == 0) continue;
                Steigungen++;
                GroessteStufe = Mathf.Max(GroessteStufe, d * RioStrassennetz.Stufe);
                if (_partner[i] != nb) OffeneStufen++;
            }
        }

        int[] _vater;

        int Wurzel(int i)
        {
            while (_vater[i] != i) { _vater[i] = _vater[_vater[i]]; i = _vater[i]; }
            return i;
        }

        void Vereinen(int a, int b)
        {
            int wa = Wurzel(a), wb = Wurzel(b);
            if (wa != wb) _vater[wa] = wb;
        }

        /// <summary>
        /// Gibt jeder Terrasse eine Hoehe: das Mittel der Wunschhoehen ihrer
        /// Knoten, gerundet auf das 1,5-m-Raster - genau das, was die Kachel
        /// "Steigung sanft" ueberwindet.
        /// </summary>
        void TerrassenHoehen(float[] wunsch)
        {
            int n = _k.Count;
            var summe = new Dictionary<int, float>(n / 4 + 1);
            var zahl = new Dictionary<int, int>(n / 4 + 1);

            for (int i = 0; i < n; i++)
            {
                int w = Wurzel(i);
                summe.TryGetValue(w, out float sv); summe[w] = sv + wunsch[i];
                zahl.TryGetValue(w, out int zv); zahl[w] = zv + 1;
            }

            var stufe = new Dictionary<int, int>(summe.Count);
            foreach (var kv in summe)
                stufe[kv.Key] = Mathf.RoundToInt(kv.Value / zahl[kv.Key] / RioStrassennetz.Stufe);

            for (int i = 0; i < n; i++) _stufe[i] = stufe[Wurzel(i)];
        }

        /// <summary>
        /// Darf zwischen diesen beiden Knoten eine Steigungskachel stehen?
        ///
        /// Die Steigungskachel ist 12 x 24 m, sie nimmt also die Flaeche von
        /// zwei Rasterzellen ein. Beide Knoten muessen deshalb Durchgangs-
        /// knoten einer geraden Strecke sein - an einer Kreuzung oder in einer
        /// Kurve ist kein Platz dafuer, und flache Kreuzungen sind ohnehin
        /// richtig.
        /// </summary>
        bool SteigungMoeglich(int a, int b)
        {
            if (Grad(a) != 2 || Grad(b) != 2) return false;
            return Gerade(a) && Gerade(b);
        }

        bool Gerade(int i) =>
            (_kante[i * 4 + 0] >= 0 && _kante[i * 4 + 2] >= 0) ||
            (_kante[i * 4 + 1] >= 0 && _kante[i * 4 + 3] >= 0);

        /// <summary>
        /// Meldet alle Flaechen der Stadt beim Gelaende an: Strassen auf ihrer
        /// Kachelhoehe, und die Blockinnenflaechen auf der Hoehe der Strassen
        /// drumherum.
        /// </summary>
        public void GelaendeMelden()
        {
            _t.StempelBeginnen();

            for (int i = 0; i < _k.Count; i++)
            {
                var kn = _k[i];
                if (kn.viertel == -2) continue;

                // Die Kachel selbst, plus ein Stueck Vorland. Der Rand sorgt
                // dafuer, dass das Gelaende weich anschliesst statt eine
                // Kante zu bilden.
                _t.Stempeln(kn.p, new Vector2(Cell, Cell), kn.yaw, kn.h - 0.05f, Cell);
            }

            _t.StempelAnwenden(3);
        }

        /// <summary>Gelaendehoehe entlang des Netzes, nach dem Stempeln.</summary>
        public float GroessterUeberstand()
        {
            float max = 0f;
            for (int i = 0; i < _k.Count; i++)
            {
                if (_k[i].viertel == -2) continue;
                max = Mathf.Max(max, Mathf.Abs(_t.SampleHeight(_k[i].p) - (_k[i].h - 0.05f)));
            }
            return max;
        }

        public override string ToString() =>
            $"{_k.Count - Entfernt:N0} Knoten ({Entfernt:N0} Splitter gestrichen), " +
            $"{Teilnetze:N0} Teilnetze, groesstes {GroesstesNetz:N0}, " +
            $"{Wendehammer:N0} Wendehammer, {AvenidaKacheln:N0} Avenida-Kacheln, "
          + $"{Terrassen:N0} Hoehenstufen, {SteigungKacheln:N0} Steigungskacheln, "
          + $"{OffeneStufen:N0} Stufen ohne Kachel "
          + $"(max {GroessteStufe:F1} m), " +
            $"Strecken {AvenidaStrecken:N0} Avenida + {RuaStrecken:N0} Rua geschlossen / "
          + $"{OffeneStrecken:N0} offen, "
          + $"groesster Knick {GroessterKnick:F2} Grad";
    }
}
