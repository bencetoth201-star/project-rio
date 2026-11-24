using System.Collections.Generic;
using UnityEngine;
using Rio.World;

namespace Rio.WorldGen
{
    /// <summary>
    /// Das Strassennetz als Graph ueber die gesetzten Kacheln.
    ///
    /// Bisher wurde jede Kachel einzeln auf das Raster gesetzt und darauf
    /// gehofft, dass die Nachbarn passen. Sie passten nicht: Avenidas knickten
    /// um 20 Grad ab, wo keine Kurve existiert, Kacheln standen auf ihrer
    /// Mittelhoehe statt auf der Hoehe ihrer Kanten, und Strassen hoerten
    /// mitten im Feld auf. Nichts davon war sichtbar, weil niemand gemessen
    /// hat.
    ///
    /// Diese Klasse rechnet aus den Ankern im Katalog nach, was wirklich
    /// zusammenhaengt:
    ///
    ///   Kante       zwei Anker liegen aufeinander und zeigen gegeneinander
    ///   Offenes Ende  ein Anker ohne Partner - dort endet die Strasse im Nichts
    ///   Insel       eine Zusammenhangskomponente, die nicht am Hauptnetz haengt
    ///
    /// Aus diesen drei Zahlen laesst sich das Netz reparieren, statt es nach
    /// Augenmass zu beurteilen.
    /// </summary>
    public class RioStrassennetz
    {
        /// <summary>Wie genau zwei Anker aufeinanderliegen muessen.</summary>
        public const float Toleranz = 1.2f;

        /// <summary>Hoehenraster der Stadt in Metern - die Steigung "sanft".</summary>
        public const float Stufe = 1.5f;

        public struct Anker
        {
            public int kachel;          // Index in der Platzierungsliste
            public int nummer;           // Index im Ankerfeld der Kachel
            public Vector3 pos;          // Weltposition
            public float yaw;            // Blickrichtung, zeigt aus der Kachel hinaus
            public string typ;           // Rua, Avenida, Beco, Schnellstrasse, Bruecke
            public int partner;          // Index des Gegenankers, -1 wenn offen
        }

        readonly List<RioHouseWorld.Placement> _p;
        readonly List<Anker> _anker = new List<Anker>();
        readonly List<int>[] _nachbarn;
        int[] _komponente;

        public IReadOnlyList<Anker> Anker_ => _anker;
        public int KachelZahl { get; private set; }
        public int Kanten { get; private set; }
        public int OffeneEnden { get; private set; }
        public int Inseln { get; private set; }
        public int GroessteInsel { get; private set; }

        public RioStrassennetz(List<RioHouseWorld.Placement> placements)
        {
            _p = placements;
            _nachbarn = new List<int>[placements.Count];
            Sammeln();
            Verknuepfen();
            Komponenten();
        }

        // ------------------------------------------------------------------
        // Anker einsammeln
        // ------------------------------------------------------------------

        static bool IstFahrbahn(string name, out string typ)
        {
            typ = null;
            // Anschluss_<Typ>_<Seite>; alles andere (Pfeiler, Strand, Bonde,
            // Tunnel_Eingang, Oben/Unten der Mauern) verbindet keine Fahrbahn.
            if (!name.StartsWith("Anschluss_")) return false;
            string rest = name.Substring("Anschluss_".Length);
            int u = rest.IndexOf('_');
            if (u <= 0) return false;
            typ = rest.Substring(0, u);
            return typ == "Rua" || typ == "Avenida" || typ == "Beco"
                || typ == "Schnellstrasse" || typ == "Bruecke";
        }

        void Sammeln()
        {
            for (int i = 0; i < _p.Count; i++)
            {
                _nachbarn[i] = new List<int>();
                var pl = _p[i];
                if (!RioModelCatalog.Get(pl.model).isRoad) continue;

                var liste = RioModelCatalog.Anchors(pl.model);
                bool istStrasse = false;

                for (int k = 0; k < liste.Length; k++)
                {
                    if (!IstFahrbahn(liste[k].name, out string typ)) continue;
                    istStrasse = true;
                    _anker.Add(new Anker
                    {
                        kachel = i,
                        nummer = k,
                        pos = RioModelCatalog.AnchorWorld(liste[k], pl.position, pl.yaw),
                        yaw = Mathf.Repeat(pl.yaw + liste[k].yaw, 360f),
                        typ = typ,
                        partner = -1
                    });
                }
                if (istStrasse) KachelZahl++;
            }
        }

        // ------------------------------------------------------------------
        // Anker paaren
        // ------------------------------------------------------------------

        static long Zelle(Vector3 p)
        {
            // 2-m-Raster. Gesucht wird in den 27 Nachbarzellen, damit ein Paar
            // auch dann gefunden wird, wenn es eine Zellgrenze kreuzt.
            long x = Mathf.FloorToInt(p.x / 2f) + 100000;
            long y = Mathf.FloorToInt(p.y / 2f) + 10000;
            long z = Mathf.FloorToInt(p.z / 2f) + 100000;
            return (x << 40) | (y << 20) | z;
        }

        void Verknuepfen()
        {
            var raster = new Dictionary<long, List<int>>(_anker.Count);
            for (int i = 0; i < _anker.Count; i++)
            {
                long key = Zelle(_anker[i].pos);
                if (!raster.TryGetValue(key, out var l)) raster[key] = l = new List<int>(4);
                l.Add(i);
            }

            for (int i = 0; i < _anker.Count; i++)
            {
                if (_anker[i].partner >= 0) continue;
                var a = _anker[i];

                int best = -1;
                float bestDist = Toleranz;

                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                for (int dz = -1; dz <= 1; dz++)
                {
                    long key = Zelle(a.pos + new Vector3(dx * 2f, dy * 2f, dz * 2f));
                    if (!raster.TryGetValue(key, out var l)) continue;

                    foreach (int j in l)
                    {
                        if (j == i) continue;
                        var b = _anker[j];
                        if (b.partner >= 0) continue;
                        if (b.kachel == a.kachel) continue;
                        if (b.typ != a.typ) continue;

                        // Die Anker muessen gegeneinander zeigen. 30 Grad
                        // Spiel, damit ein Bogen an eine Gerade passt, aber
                        // nicht so viel, dass zwei parallele Strassen als
                        // verbunden gelten.
                        float diff = Mathf.Abs(Mathf.DeltaAngle(a.yaw + 180f, b.yaw));
                        if (diff > 30f) continue;

                        float d = Vector3.Distance(a.pos, b.pos);
                        if (d < bestDist) { bestDist = d; best = j; }
                    }
                }

                if (best < 0) continue;

                var bb = _anker[best];
                a.partner = best;
                bb.partner = i;
                _anker[i] = a;
                _anker[best] = bb;

                _nachbarn[a.kachel].Add(bb.kachel);
                _nachbarn[bb.kachel].Add(a.kachel);
                Kanten++;
            }

            OffeneEnden = 0;
            foreach (var a in _anker)
                if (a.partner < 0) OffeneEnden++;
        }

        // ------------------------------------------------------------------
        // Zusammenhangskomponenten
        // ------------------------------------------------------------------

        void Komponenten()
        {
            _komponente = new int[_p.Count];
            for (int i = 0; i < _komponente.Length; i++) _komponente[i] = -1;

            var stapel = new Stack<int>();
            int id = 0;
            GroessteInsel = 0;

            for (int start = 0; start < _p.Count; start++)
            {
                if (_komponente[start] >= 0) continue;
                if (_nachbarn[start] == null || _nachbarn[start].Count == 0)
                {
                    // Eine Kachel ganz ohne Anschluss. Nur mitzaehlen, wenn es
                    // ueberhaupt eine Strassenkachel ist.
                    if (RioModelCatalog.Get(_p[start].model).isRoad)
                    {
                        _komponente[start] = id++;
                        if (GroessteInsel < 1) GroessteInsel = 1;
                    }
                    continue;
                }

                int groesse = 0;
                stapel.Push(start);
                _komponente[start] = id;

                while (stapel.Count > 0)
                {
                    int c = stapel.Pop();
                    groesse++;
                    foreach (int n in _nachbarn[c])
                    {
                        if (_komponente[n] >= 0) continue;
                        _komponente[n] = id;
                        stapel.Push(n);
                    }
                }

                if (groesse > GroessteInsel) GroessteInsel = groesse;
                id++;
            }

            Inseln = id;
        }

        public int KomponenteVon(int kachel) => _komponente[kachel];

        /// <summary>Groesse jeder Komponente, indiziert wie KomponenteVon.</summary>
        public int[] KomponentenGroessen()
        {
            var g = new int[Inseln];
            for (int i = 0; i < _komponente.Length; i++)
                if (_komponente[i] >= 0) g[_komponente[i]]++;
            return g;
        }

        public IReadOnlyList<int> NachbarnVon(int kachel) => _nachbarn[kachel];

        // ------------------------------------------------------------------
        // Platzieren am Anker
        // ------------------------------------------------------------------

        /// <summary>
        /// Wo der Pivot einer Kachel liegen muss, damit ihr Anker
        /// <paramref name="ankerName"/> genau auf <paramref name="zielPos"/>
        /// sitzt und in Richtung <paramref name="zielYaw"/> zeigt.
        ///
        /// Das ist die einzige richtige Art, Boegen, Rampen und Abfahrten zu
        /// setzen: bei denen liegt der Anschluss nicht in der Kachelmitte,
        /// deshalb entstehen beim Rechnen mit Kachelmitten Luecken.
        /// </summary>
        public static bool Andocken(int model, string ankerName, Vector3 zielPos, float zielYaw,
                                    out Vector3 pivot, out float yaw)
        {
            pivot = zielPos; yaw = zielYaw;
            if (!RioModelCatalog.TryAnchor(model, ankerName, out var a)) return false;

            yaw = Mathf.Repeat(zielYaw - a.yaw, 360f);
            float rad = yaw * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            var gedreht = new Vector3(a.pos.x * c + a.pos.z * s,
                                      a.pos.y,
                                      -a.pos.x * s + a.pos.z * c);
            pivot = zielPos - gedreht;
            return true;
        }

        /// <summary>
        /// Das andere Ende: der Anker, der nicht <paramref name="ankerName"/>
        /// ist, in Weltkoordinaten. Damit laesst sich eine Kette fortsetzen.
        /// </summary>
        public static bool AnderesEnde(int model, string ankerName, Vector3 pivot, float yaw,
                                       out Vector3 pos, out float richtung)
        {
            pos = pivot; richtung = yaw;
            foreach (var a in RioModelCatalog.Anchors(model))
            {
                if (a.name == ankerName) continue;
                if (!IstFahrbahn(a.name, out _)) continue;
                pos = RioModelCatalog.AnchorWorld(a, pivot, yaw);
                richtung = Mathf.Repeat(yaw + a.yaw, 360f);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Raeumt Strassenstuecke weg, die an kein richtiges Netz anschliessen.
        ///
        /// Es bleiben immer Splitter uebrig: ein Stueck Ringstrasse am Rand
        /// einer Bucht, ein Rest an einem Steilhang, eine Verbindung, die
        /// nicht zustande kam. Im Spiel sind das Strassenstummel mitten im
        /// Feld, auf die man nicht fahren kann. Die Zahl ist klein, der
        /// Eindruck nicht - also weg damit.
        ///
        /// Liefert, wie viele Kacheln entfernt wurden.
        /// </summary>
        public static int SplitterEntfernen(List<RioHouseWorld.Placement> p, int mindestens)
        {
            var netz = new RioStrassennetz(p);
            var groessen = netz.KomponentenGroessen();

            var weg = new bool[p.Count];
            int anzahl = 0;
            for (int i = 0; i < p.Count; i++)
            {
                int k = netz._komponente[i];
                if (k < 0) continue;                       // kein Strassenteil
                if (groessen[k] >= mindestens) continue;
                weg[i] = true;
                anzahl++;
            }
            if (anzahl == 0) return 0;

            int schreib = 0;
            for (int i = 0; i < p.Count; i++)
                if (!weg[i]) p[schreib++] = p[i];
            p.RemoveRange(schreib, p.Count - schreib);
            return anzahl;
        }

        public override string ToString() =>
            $"{KachelZahl:N0} Kacheln, {Kanten:N0} Verbindungen, {OffeneEnden:N0} offene Enden, " +
            $"{Inseln:N0} Teilnetze (groesstes {GroessteInsel:N0})";
    }
}
