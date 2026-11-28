using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Ersetzt die Platzhalter-Kloetze der Orte aus der Legende durch die echten
    /// Modelle und setzt die Einzelstuecke, die keinen Ort auf der Karte haben
    /// (Hochhaeuser, Palast, Drughouse, Werkstatt, Hafen, Strand).
    ///
    /// Drei Phasen, weil die Welt in dieser Reihenfolge entsteht:
    /// 1. <see cref="Planen"/> - vor der Stadt. Sucht fuer jedes Modell einen
    ///    Platz in der Naehe seines Orts: genug Land, nicht zu steil, keine
    ///    Ueberschneidung mit anderen Wahrzeichen. Die Flaeche wird danach aus
    ///    der Stadtmaske genommen und im Belegungsraster gesperrt, damit dort
    ///    weder Strassen noch Haeuser entstehen.
    /// 2. <see cref="Stempeln"/> - im Gelaendeschritt. Ebnet die Grundflaeche
    ///    auf eine Hoehe ein, so wie die Strassen ihre Kacheln anmelden.
    /// 3. <see cref="Setzen"/> - nachdem die Welt gebaut ist. Erzeugt die
    ///    Modelle in der Szene, mit Hitboxen, Farben und Heist-Komponenten.
    ///
    /// Gedreht wird nur um ganze Vielfache von 90 Grad, ausser am Hafen, der
    /// der Kueste folgt. Das Belegungsraster und die Gelaendestempel drehen
    /// andersherum als Unity, deshalb bekommen sie hier immer -yaw.
    /// </summary>
    public static class RioWahrzeichen
    {
        // ------------------------------------------------------------------
        // Modellpfade
        // ------------------------------------------------------------------

        const string Online = "Assets/RioOnline/Modelle/";
        const string Haeuser = "Assets/RioHaeuser/Modelle/";
        const string Heists = "Assets/RioHeists/Modelle/";
        const string Kanal = "Assets/RioHeists/Kanalisation/";
        const string Hafen = "Assets/RioHafen/Modelle/";
        const string Strand = "Assets/RioStrand/Modelle/";
        const string Fahrzeuge = "Assets/RioFahrzeuge/Modelle/";

        const string Flughafen = Online + "flughafen/flughafen_santos_dumont.glb";
        const string Flugzeug = Online + "flughafen/flugzeug.glb";
        const string Krankenhaus = Online + "krankenhaus/krankenhaus.glb";
        const string Militaer = Online + "militaerbasis/militaerbasis.glb";
        const string Casino = Online + "rubin_casino/rubin_casino.glb";
        const string Cristo = Online + "cristo/cristo_redentor.glb";
        const string Drughouse = Online + "drughouse/drughouse.glb";
        const string HochhausBarra = Online + "hochhaeuser/hochhaus_barra.glb";
        const string HochhausCentro = Online + "hochhaeuser/hochhaus_centro.glb";
        const string HochhausCopa = Online + "hochhaeuser/hochhaus_copacabana.glb";
        const string Sportwagen = Online + "sportwagen/sportwagen.glb";
        const string WaffenVan = Online + "van/waffen_van.glb";
        const string Club = Haeuser + "29_clube_subsolo.glb";
        const string Garagem = Haeuser + "30_garagem_021.glb";
        const string Moda = Haeuser + "31_mega_moda.glb";
        const string Estadio = Haeuser + "32_estadio_do_rio.glb";
        const string Joias = Haeuser + "33_esmeralda_joias.glb";
        const string Imperio = Heists + "01_casa_imperio.glb";
        const string Yacht = Heists + "02_yacht_soberana.glb";
        const string ImperioZu = Heists + "03_casa_imperio_abgeriegelt.glb";
        const string KanalNetz = Kanal + "10_kanal_beispielnetz.glb";
        const string ValenteGt = Fahrzeuge + "valente_gt.glb";

        // ------------------------------------------------------------------
        // Daten
        // ------------------------------------------------------------------

        /// <summary>Ein Modell innerhalb eines Wahrzeichens, relativ zu dessen Pivot.</summary>
        public class Teil
        {
            public string Pfad;
            public Vector3 Lokal;
            public float Yaw;
            public string[] Aus = System.Array.Empty<string>();
            public bool Farben = true;
            public bool Heist;
        }

        /// <summary>Ein gedrehtes Rechteck in Weltkoordinaten. Yaw wie in Unity.</summary>
        public struct Flaeche
        {
            public Vector2 Mitte, Groesse;
            public float Yaw;
            public Flaeche(Vector2 mitte, Vector2 groesse, float yaw) { Mitte = mitte; Groesse = groesse; Yaw = yaw; }

            public bool Enthaelt(Vector2 p, float rand)
            {
                Vector2 l = InsLokale(p - Mitte, Yaw);
                return Mathf.Abs(l.x) <= Groesse.x * 0.5f + rand && Mathf.Abs(l.y) <= Groesse.y * 0.5f + rand;
            }

            /// <summary>Achsparallele Huelle, fuer schnelle Ueberschneidungstests.</summary>
            public Rect Huelle(float rand)
            {
                float rad = Yaw * Mathf.Deg2Rad;
                float cs = Mathf.Abs(Mathf.Cos(rad)), sn = Mathf.Abs(Mathf.Sin(rad));
                float ex = (Groesse.x * cs + Groesse.y * sn) * 0.5f + rand;
                float ez = (Groesse.x * sn + Groesse.y * cs) * 0.5f + rand;
                return new Rect(Mitte.x - ex, Mitte.y - ez, ex * 2f, ez * 2f);
            }
        }

        public class Eintrag
        {
            public string Name;
            /// <summary>Ort aus der Legende, dessen Platzhalter dieses Wahrzeichen ersetzt. Null bei Einzelstuecken.</summary>
            public string Label;
            public Vector2 Pos;
            public float Yaw;
            public float Hoehe;
            public float Rand = 24f;
            public bool Stempeln = true;
            public readonly List<Teil> Teile = new List<Teil>();
            /// <summary>Gesperrte Flaechen: keine Strassen, keine Haeuser.</summary>
            public readonly List<Flaeche> Sperre = new List<Flaeche>();
            /// <summary>Flaechen, die auf eine eigene Hoehe gestempelt werden (zusaetzlich zur Sperre).</summary>
            public readonly List<(Flaeche f, float h, float rand)> Ebnen = new List<(Flaeche, float, float)>();
            /// <summary>Hier wird das Gelaende ausgespart (Untergeschosse, Schaechte).</summary>
            public readonly List<Flaeche> Loecher = new List<Flaeche>();
            /// <summary>Fahrzeuge an Ankern: (Anker im ersten Teil, Modellpfad).</summary>
            public readonly List<(string anker, string pfad)> AnAnkern = new List<(string, string)>();
        }

        static List<Eintrag> _liste;
        static readonly List<Flaeche> _sperren = new List<Flaeche>();
        static readonly List<Flaeche> _loecher = new List<Flaeche>();

        public static IReadOnlyList<Eintrag> Liste => _liste ?? (IReadOnlyList<Eintrag>)System.Array.Empty<Eintrag>();

        /// <summary>Ersetzt ein Wahrzeichen den Platzhalter dieses Orts?</summary>
        public static bool Ersetzt(string label)
        {
            if (_liste == null || label == null) return false;
            foreach (var e in _liste) if (e.Label == label) return true;
            return false;
        }

        /// <summary>Liegt der Punkt (mit Rand) auf einer gesperrten Flaeche?</summary>
        public static bool Belegt(Vector2 p, float rand)
        {
            if (_liste == null) return false;
            foreach (var f in _sperren) if (f.Enthaelt(p, rand)) return true;
            return false;
        }

        /// <summary>Soll das Gelaende hier ausgespart werden?</summary>
        public static bool ImLoch(Vector2 p)
        {
            if (_liste == null) return false;
            foreach (var f in _loecher) if (f.Enthaelt(p, 0f)) return true;
            return false;
        }

        public static void Zuruecksetzen()
        {
            _liste = null;
            _sperren.Clear();
            _loecher.Clear();
        }

        // ------------------------------------------------------------------
        // Phase 1: Planen
        // ------------------------------------------------------------------

        public static void Planen(RioWorldTerrain t)
        {
            Zuruecksetzen();
            _liste = new List<Eintrag>();
            _cache.Clear();

            // Der Hafen zuerst: er braucht die Kueste, alles andere ist flexibler.
            foreach (var place in RioWorldLayout.Places)
                if (place.Kind == RioWorldLayout.Poi.Hafen)
                    HafenPlanen(t, place);

            // Die kleinen Landebahnen bleiben Kloetze, liegen aber mitten in
            // den Gang-Vierteln. Ihre Flaeche wird gesperrt und eingeebnet,
            // sonst stehen Huetten auf der Piste.
            foreach (var place in RioWorldLayout.Places)
                if (place.Kind == RioWorldLayout.Poi.FlughafenKlein)
                    LandebahnFreihalten(t, place);

            // Die grossen Flaechen zuerst, dann die kleinen - so finden die
            // kleinen noch Luecken, die grossen nicht.
            foreach (var place in RioWorldLayout.Places)
            {
                switch (place.Kind)
                {
                    case RioWorldLayout.Poi.FlughafenGross:
                        FlughafenPlanen(t, place);
                        break;
                    case RioWorldLayout.Poi.Militaerbasis:
                        Modell(t, place, "Militaerbasis", Militaer, 600f, 0.9f, 30f);
                        break;
                    case RioWorldLayout.Poi.Stadion:
                        Modell(t, place, "Estadio_do_Rio", Estadio, 450f, 0.95f, 20f);
                        break;
                }
            }

            foreach (var place in RioWorldLayout.Places)
            {
                switch (place.Kind)
                {
                    case RioWorldLayout.Poi.JesusStatue:
                        CristoPlanen(t, place);
                        break;
                    case RioWorldLayout.Poi.BankGross when place.Label == "Banco Central":
                        ImperioPlanen(t, place);
                        break;
                    case RioWorldLayout.Poi.Krankenhaus:
                        Modell(t, place, "Krankenhaus " + place.Label, Krankenhaus, 350f, 0.95f, 16f);
                        break;
                    case RioWorldLayout.Poi.Casino:
                        Modell(t, place, "Rubin_Casino", Casino, 350f, 0.95f, 14f);
                        break;
                    case RioWorldLayout.Poi.Nightclub:
                        Modell(t, place, "Clube_Subsolo", Club, 250f, 0.95f, 10f);
                        break;
                    case RioWorldLayout.Poi.JeweleryGross:
                    case RioWorldLayout.Poi.JeweleryKlein:
                        Modell(t, place, "Esmeralda_Joias " + place.Label, Joias, 200f, 0.95f, 8f);
                        break;
                    case RioWorldLayout.Poi.Clothstore:
                        Modell(t, place, "Mega_Moda", Moda, 200f, 0.95f, 8f);
                        break;
                }
            }

            // Einzelstuecke ohne Ort auf der Karte, in der Naehe passender Gegenden
            Vector2 centro = PlaceAt("Banco Central");
            Einzeln(t, "Hochhaus_Centro", HochhausCentro, centro + new Vector2(260f, -120f), 450f, 12f);
            Einzeln(t, "Hochhaus_Copacabana", HochhausCopa,
                    RioWorldLayout.BeachCenter + (centro - RioWorldLayout.BeachCenter).normalized * 520f, 450f, 12f);
            Einzeln(t, "Hochhaus_Barra", HochhausBarra,
                    RioWorldLayout.LakeCenter + new Vector2(RioWorldLayout.LakeRadius + 180f, 60f), 500f, 12f);
            var garage = Einzeln(t, "Garagem_021", Garagem, RioWorldLayout.ToWorld(560, 470), 450f, 10f);
            if (garage != null)
            {
                garage.AnAnkern.Add(("Auto_Abholen_1", ValenteGt));
                garage.AnAnkern.Add(("Auto_Abholen_2", Sportwagen));
            }

            // Das Drughouse steht im Gang-Viertel Zona Oeste, noerdlich der Landebahn
            var drug = Einzeln(t, "Drughouse", Drughouse, RioWorldLayout.ToWorld(455, 560), 300f, 30f);
            if (drug != null)
                drug.Teile.Add(new Teil { Pfad = WaffenVan, Lokal = new Vector3(14f, 0f, 4f), Yaw = 0f, Farben = false });

            foreach (var e in _liste)
            {
                _sperren.AddRange(e.Sperre);
                _loecher.AddRange(e.Loecher);
            }
        }

        static Vector2 PlaceAt(string label)
        {
            foreach (var p in RioWorldLayout.Places) if (p.Label == label) return p.Position;
            return Vector2.zero;
        }

        /// <summary>Ein Modell fuer einen Ort aus der Legende.</summary>
        static Eintrag Modell(RioWorldTerrain t, RioWorldLayout.Place place, string name, string pfad,
                              float radius, float minLand, float maxSpanne, string[] aus = null)
        {
            var e = Suchen(t, name, pfad, place.Position, radius, minLand, maxSpanne, aus);
            if (e != null) e.Label = place.Label;
            return e;
        }

        /// <summary>Ein Einzelstueck ohne Ort auf der Karte.</summary>
        static Eintrag Einzeln(RioWorldTerrain t, string name, string pfad, Vector2 start,
                               float radius, float maxSpanne)
        {
            return Suchen(t, name, pfad, start, radius, 0.98f, maxSpanne, null);
        }

        static Eintrag Suchen(RioWorldTerrain t, string name, string pfad, Vector2 start, float radius,
                              float minLand, float maxSpanne, string[] aus)
        {
            if (!Grundflaeche(pfad, aus, out Vector2 fMitte, out Vector2 fGroesse))
            {
                Debug.LogWarning($"[Rio] Wahrzeichen {name}: Modell {pfad} fehlt.");
                return null;
            }

            if (!PlatzSuchen(t, start, radius, fMitte, fGroesse, minLand, maxSpanne,
                             out Vector2 pos, out float yaw, out float hoehe))
            {
                Debug.LogWarning($"[Rio] Wahrzeichen {name}: kein Platz im Umkreis von {radius:F0} m gefunden, Platzhalter bleibt.");
                return null;
            }

            var e = new Eintrag { Name = name, Pos = pos, Yaw = yaw, Hoehe = hoehe };
            e.Teile.Add(new Teil { Pfad = pfad, Aus = aus ?? System.Array.Empty<string>() });
            e.Sperre.Add(new Flaeche(pos + Drehen(fMitte, yaw), fGroesse, yaw));
            _liste.Add(e);
            return e;
        }

        /// <summary>
        /// Sucht in Ringen um den Startpunkt den naechstgelegenen Platz, der
        /// genug Land hat, flach genug ist und kein anderes Wahrzeichen und
        /// keinen Bankplatz beruehrt.
        /// </summary>
        static bool PlatzSuchen(RioWorldTerrain t, Vector2 start, float radius, Vector2 fMitte, Vector2 fGroesse,
                                float minLand, float maxSpanne,
                                out Vector2 bestPos, out float bestYaw, out float bestHoehe)
        {
            bestPos = start; bestYaw = 0f; bestHoehe = 0f;
            float bestScore = float.MaxValue;
            float schritt = Mathf.Clamp(Mathf.Max(fGroesse.x, fGroesse.y) * 0.1f, 8f, 40f);
            var hoehen = new List<float>(512);

            for (float r = 0f; r <= radius; r += schritt)
            {
                int n = r < 1f ? 1 : Mathf.Max(6, Mathf.CeilToInt(2f * Mathf.PI * r / schritt));
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f;
                    Vector2 pos = start + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;

                    for (int y = 0; y < 4; y++)
                    {
                        float yaw = y * 90f;
                        var f = new Flaeche(pos + Drehen(fMitte, yaw), fGroesse, yaw);
                        if (Kollidiert(f)) continue;
                        if (!Pruefen(t, f, minLand, maxSpanne, hoehen, out float spanne, out float land, out float median))
                            continue;

                        float score = r / Mathf.Max(radius, 1f) + spanne * 0.02f + (1f - land);
                        if (score < bestScore)
                        {
                            bestScore = score; bestPos = pos; bestYaw = yaw; bestHoehe = median;
                        }
                    }
                }
                // Der erste Ring mit einem Treffer ist nah genug - weiter
                // draussen wird der Score nur noch schlechter.
                if (bestScore < float.MaxValue && r > 0f && bestScore < (r + schritt) / Mathf.Max(radius, 1f))
                    break;
            }
            return bestScore < float.MaxValue;
        }

        static bool Kollidiert(Flaeche f)
        {
            var h = f.Huelle(8f);
            foreach (var e in _liste)
                foreach (var s in e.Sperre)
                    if (h.Overlaps(s.Huelle(0f))) return true;

            // Banken setzt der Hausplaner selbst, die bleiben, wo sie sind
            foreach (var p in RioWorldLayout.Places)
            {
                if (p.Kind != RioWorldLayout.Poi.BankGross && p.Kind != RioWorldLayout.Poi.BankKlein) continue;
                if (p.Label == "Banco Central") continue;
                if (f.Enthaelt(p.Position, 40f)) return true;
            }

            // Bruecke: Anfang und Ende freihalten
            var bruecke = RioWorldLayout.ToWorld(RioWorldLayout.BridgePx);
            if (f.Enthaelt(bruecke[0], 120f) || f.Enthaelt(bruecke[bruecke.Length - 1], 120f)) return true;
            return false;
        }

        static bool Pruefen(RioWorldTerrain t, Flaeche f, float minLand, float maxSpanne, List<float> hoehen,
                            out float spanne, out float land, out float median)
        {
            spanne = 0f; land = 0f; median = 0f;
            hoehen.Clear();
            float sx = Mathf.Max(6f, f.Groesse.x / 14f);
            float sz = Mathf.Max(6f, f.Groesse.y / 14f);
            int gesamt = 0, anLand = 0;
            float min = float.MaxValue, max = float.MinValue;
            int erlaubtNass = 0;

            for (float lx = -f.Groesse.x * 0.5f; lx <= f.Groesse.x * 0.5f + 0.01f; lx += sx)
            for (float lz = -f.Groesse.y * 0.5f; lz <= f.Groesse.y * 0.5f + 0.01f; lz += sz)
            {
                Vector2 p = f.Mitte + Drehen(new Vector2(lx, lz), f.Yaw);
                if (p.x < t.MinX + 50f || p.x > t.MaxX - 50f || p.y < t.MinZ + 50f || p.y > t.MaxZ - 50f) return false;
                gesamt++;
                if (t.IsInLake(p)) return false;
                if (!t.IsLand(p)) { erlaubtNass++; continue; }
                anLand++;
                float h = t.SampleHeight(p);
                hoehen.Add(h);
                if (h < min) min = h;
                if (h > max) max = h;
            }

            if (gesamt == 0 || anLand == 0) return false;
            land = anLand / (float)gesamt;
            if (land < minLand) return false;
            spanne = max - min;
            if (spanne > maxSpanne) return false;

            hoehen.Sort();
            median = Mathf.Max(3f, hoehen[hoehen.Count / 2]);
            return true;
        }

        // ------------------------------------------------------------------
        // Besondere Plaetze
        // ------------------------------------------------------------------

        /// <summary>
        /// Die Statue bringt ihren eigenen Huegel mit. Sie kommt auf den
        /// Hausberg, ihr Huegel ersetzt die Kuppe.
        /// </summary>
        static void CristoPlanen(RioWorldTerrain t, RioWorldLayout.Place place)
        {
            if (!Grundflaeche(Cristo, null, out Vector2 fMitte, out Vector2 fGroesse)) return;
            Vector2 gipfel = RioWorldLayout.Mountains[0].Center;

            // Drehung so, dass die Statue zur Stadt schaut (Front = +Z des Modells)
            Vector2 zurStadt = (PlaceAt("Banco Central") - gipfel).normalized;
            float yaw = Mathf.Round(Mathf.Atan2(zurStadt.x, zurStadt.y) * Mathf.Rad2Deg / 90f) * 90f;

            Vector2 pos = gipfel - Drehen(fMitte, yaw);
            var f = new Flaeche(gipfel, fGroesse, yaw);

            // Plateau auf der Hoehe, die der Berg in der Flaeche im Mittel hat
            var hoehen = new List<float>();
            for (float lx = -fGroesse.x * 0.5f; lx <= fGroesse.x * 0.5f; lx += 8f)
            for (float lz = -fGroesse.y * 0.5f; lz <= fGroesse.y * 0.5f; lz += 8f)
                hoehen.Add(t.SampleHeight(gipfel + Drehen(new Vector2(lx, lz), yaw)));
            hoehen.Sort();

            var e = new Eintrag
            {
                Name = "Cristo_Redentor", Label = place.Label,
                Pos = pos, Yaw = yaw, Hoehe = hoehen[hoehen.Count / 2], Rand = 40f
            };
            e.Teile.Add(new Teil { Pfad = Cristo, Farben = false });
            e.Sperre.Add(f);
            _liste.Add(e);
        }

        /// <summary>
        /// Casa Império als Banco Central, mit der Maya-Kanalisation darunter.
        /// Das Gelaende bekommt unter dem Palast ein Loch (drei Untergeschosse
        /// bis -21 m) und eines ueber Garage und Gullydeckel des Kanalnetzes.
        /// </summary>
        static void ImperioPlanen(RioWorldTerrain t, RioWorldLayout.Place place)
        {
            var e = Suchen(t, "Casa_Imperio", Imperio, place.Position, 300f, 0.98f, 10f, null);
            if (e == null) return;
            e.Label = place.Label;
            e.Teile[0].Heist = true;

            // Lage des Kanalnetzes einmal zur Probe ausrechnen: Palast und Netz
            // kurz in die Szene, andocken, messen, wieder weg.
            var palastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Imperio);
            var netzPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KanalNetz);
            if (palastPrefab == null || netzPrefab == null) return;

            var palast = Object.Instantiate(palastPrefab);
            var netz = Object.Instantiate(netzPrefab);
            try
            {
                palast.transform.SetPositionAndRotation(new Vector3(e.Pos.x, e.Hoehe, e.Pos.y), Quaternion.Euler(0f, e.Yaw, 0f));

                // Loch nur unter den Untergeschossen. Der Rest des Grundstuecks
                // hat keinen eigenen Boden, dort bleibt das Gelaende.
                var ug = WeltHuelle(palast, "UG", nurOben: true);
                if (ug.size.x > 1f) e.Loecher.Add(new Flaeche(ug.center, ug.size - new Vector2(1f, 1f), 0f));

                var ziel = RioKanal.Finde(palast.transform, "Kanal_Anschluss");
                var anker = RioKanal.Finde(netz.transform, "Anschluss_Palast");
                if (ziel == null || anker == null)
                {
                    Debug.LogWarning("[Rio] Kanalnetz: Anker Kanal_Anschluss/Anschluss_Palast nicht gefunden.");
                    return;
                }
                RioKanal.Andocken(netz.transform, anker, ziel);

                Vector3 np = netz.transform.position;
                float nYaw = netz.transform.eulerAngles.y;
                e.Teile.Add(new Teil
                {
                    Pfad = KanalNetz, Heist = true,
                    Lokal = Quaternion.Euler(0f, -e.Yaw, 0f) * (np - new Vector3(e.Pos.x, e.Hoehe, e.Pos.y)),
                    Yaw = nYaw - e.Yaw
                });

                // Grundflaeche des ganzen Netzes sperren und auf Palasthoehe ebnen
                var huelle = WeltHuelle(netz, null, nurOben: false);
                var netzFlaeche = new Flaeche(huelle.center, huelle.size, 0f);
                e.Sperre.Add(netzFlaeche);
                e.Ebnen.Add((netzFlaeche, e.Hoehe, 20f));

                // Loecher nur dort, wo das Netz an die Oberflaeche kommt: in
                // der Garage (Bodenklappen zum Bootslift) und am Gullydeckel.
                // Die Schaechte selbst liegen unter der Erde.
                foreach (var stichwort in new[] { "Garage", "Gullydeckel" })
                {
                    var teil = WeltHuelle(netz, stichwort, nurOben: false);
                    if (teil.size.x > 0.5f && teil.size.y > 0.5f && teil.size.x < 60f && teil.size.y < 60f)
                        e.Loecher.Add(new Flaeche(teil.center, teil.size + new Vector2(1f, 1f), 0f));
                }
            }
            finally
            {
                Object.DestroyImmediate(palast);
                Object.DestroyImmediate(netz);
            }
        }

        /// <summary>
        /// Flaeche der kleinen Landebahn aus BuildAirport (780 x 30 m entlang X,
        /// Terminal 70 x 28 m noerdlich daneben) sperren und einebnen. Kein
        /// Label: der Platzhalter-Klotz bleibt, das hier ist nur die Sperre.
        /// </summary>
        static void LandebahnFreihalten(RioWorldTerrain t, RioWorldLayout.Place place)
        {
            var f = new Flaeche(place.Position + new Vector2(0f, 14f), new Vector2(820f, 90f), 0f);
            var hoehen = new List<float>();
            for (float lx = -400f; lx <= 400f; lx += 20f)
            for (float lz = -40f; lz <= 40f; lz += 20f)
            {
                var p = f.Mitte + new Vector2(lx, lz);
                if (t.IsLand(p)) hoehen.Add(t.SampleHeight(p));
            }
            if (hoehen.Count == 0) return;
            hoehen.Sort();

            var e = new Eintrag
            {
                Name = "Landebahn " + place.Label, Label = null,
                Pos = place.Position, Yaw = 0f, Hoehe = Mathf.Max(3f, hoehen[hoehen.Count / 2]), Rand = 30f
            };
            e.Sperre.Add(f);
            _liste.Add(e);
        }

        /// <summary>
        /// Der Flughafen als Halbinsel in der Bucht, wie Santos Dumont: die
        /// Westkante mit Terminals und Parkplaetzen liegt ein Stueck auf dem
        /// Land, Vorfeld und Landebahn ragen ins Wasser. Das Modell bringt
        /// seinen eigenen Boden mit (Unterseite bis -6,6 m), deshalb wird das
        /// Gelaende unter dem Wasserteil abgesenkt statt aufgeschuettet - so
        /// bleibt die Form der Halbinsel sichtbar und nicht ein Rechteck aus
        /// Gelaende. Auf dem Landteil wird auf Flughafenhoehe abgetragen, mit
        /// breitem Rand, damit die Stadt sanft herunterkommt.
        /// </summary>
        static void FlughafenPlanen(RioWorldTerrain t, RioWorldLayout.Place place)
        {
            var aus = new[] { "SDU_Wasser" };
            if (!Grundflaeche(Flughafen, aus, out Vector2 fMitte, out Vector2 fGroesse)) return;

            // Yaw 180: Terminals (lokal +X) nach Westen zur Stadt, Landebahn
            // nach Osten aufs Wasser, Laengsachse Nord-Sued an der Kueste entlang.
            const float yaw = 180f;
            float breite = fGroesse.x, laenge = fGroesse.y;
            Vector2 start = place.Position;

            // Kuestenlinie entlang der ganzen Laenge abtasten: von weit im
            // Land nach Osten laufen, bis Wasser kommt.
            const float scheibe = 30f;
            var kueste = new List<float>();
            var proZ = new List<(float z, float x)>();
            for (float z = start.y - laenge * 0.5f + scheibe * 0.5f; z < start.y + laenge * 0.5f; z += scheibe)
            {
                for (float x = start.x - 900f; x < start.x + 900f; x += 6f)
                {
                    if (t.IsLand(new Vector2(x, z))) continue;
                    if (x > start.x - 900f + 6f) { kueste.Add(x); proZ.Add((z, x)); }
                    break;
                }
            }
            if (kueste.Count < 5)
            {
                Debug.LogWarning("[Rio] Flughafen: keine Ostkueste neben dem Startpunkt gefunden.");
                return;
            }
            kueste.Sort();
            float kuesteX = kueste[kueste.Count / 2];

            const float aufLand = 150f;          // so weit greift die Westkante ins Land
            const float hoehe = 1.2f;            // Wasser im Modell liegt bei -1,2 -> unser Meeresspiegel
            float west = kuesteX - aufLand;
            var mitte = new Vector2(west + breite * 0.5f, start.y);

            var e = new Eintrag
            {
                Name = "Flughafen_Santos_Dumont", Label = place.Label,
                Pos = mitte - Drehen(fMitte, yaw), Yaw = yaw, Hoehe = hoehe, Stempeln = false
            };
            e.Teile.Add(new Teil { Pfad = Flughafen, Aus = aus, Farben = false });

            var ganz = new Flaeche(mitte, new Vector2(breite, laenge), 0f);
            e.Sperre.Add(ganz);

            // Scheibe fuer Scheibe entlang der Kueste: was Land ist, kommt auf
            // Flughafenhoehe (mit Rand zur Stadt hin), was Wasser ist, unter
            // den Meeresspiegel. Ein Rechteck ueber die ganze Laenge hatte an
            // den Enden Sandzungen ins Wasser geschoben, weil die Kueste dort
            // schraeg laeuft.
            float ost = west + breite;
            foreach (var (z, kx) in proZ)
            {
                float landBis = Mathf.Clamp(kx, west, ost);
                if (landBis > west + 1f)
                    e.Ebnen.Add((new Flaeche(new Vector2((west + landBis) * 0.5f, z), new Vector2(landBis - west, scheibe), 0f),
                                 hoehe - 0.3f, 40f));
                // Wasser ohne Rand, sonst mittelt es sich mit dem Landteil
                e.Ebnen.Add((new Flaeche(new Vector2((landBis + ost) * 0.5f, z), new Vector2(ost - landBis, scheibe), 0f),
                             -4f, 0f));
            }

            // Zwischen Stadt und Flughafen einen Streifen freihalten, als Zufahrt
            e.Sperre.Add(new Flaeche(new Vector2(west - 20f, start.y), new Vector2(40f, laenge), 0f));

            _liste.Add(e);
        }

        /// <summary>
        /// Containerhafen an der naechsten Kueste: 20 Kaikanten, Containerschiff
        /// und Megayacht am Liegeplatz, Kraene, Stapelflaeche, Buero und Tor.
        /// Hinter der Kaikante wird auf +3 m aufgeschuettet, davor ein
        /// Hafenbecken auf -14 m ausgebaggert.
        /// </summary>
        static void HafenPlanen(RioWorldTerrain t, RioWorldLayout.Place place)
        {
            Vector2 p0 = place.Position;

            // Richtung zum naechsten Wasser
            float bestD = float.MaxValue;
            Vector2 bestDir = Vector2.right;
            for (int i = 0; i < 72; i++)
            {
                float a = i / 72f * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (float d = 20f; d < 3000f && d < bestD; d += 20f)
                {
                    if (t.IsLand(p0 + dir * d)) continue;
                    bestD = d; bestDir = dir;
                    break;
                }
            }
            if (bestD == float.MaxValue) return;

            // Kuestenlinie quer dazu abtasten und eine Gerade hineinlegen
            Vector2 quer = new Vector2(-bestDir.y, bestDir.x);
            Vector2 kueste = p0 + bestDir * bestD;
            var punkte = new List<Vector2>();
            for (float s = -260f; s <= 260f; s += 40f)
            {
                Vector2 q = kueste + quer * s - bestDir * 400f;
                for (float d = 0f; d < 900f; d += 4f)
                {
                    if (t.IsLand(q + bestDir * d)) continue;
                    punkte.Add(q + bestDir * d);
                    break;
                }
            }
            if (punkte.Count < 4) return;

            Vector2 mitte = Vector2.zero;
            foreach (var pt in punkte) mitte += pt;
            mitte /= punkte.Count;
            float sxx = 0f, sxy = 0f, syy = 0f;
            foreach (var pt in punkte)
            {
                var d = pt - mitte;
                sxx += d.x * d.x; sxy += d.x * d.y; syy += d.y * d.y;
            }
            float winkel = 0.5f * Mathf.Atan2(2f * sxy, sxx - syy);
            Vector2 tangente = new Vector2(Mathf.Cos(winkel), Mathf.Sin(winkel));
            Vector2 normale = new Vector2(-tangente.y, tangente.x);
            if (Vector2.Dot(normale, bestDir) < 0f) normale = -normale;      // zeigt aufs Wasser

            // Lokales +Z = Wasserseite
            float yaw = Mathf.Atan2(normale.x, normale.y) * Mathf.Rad2Deg;
            const int kacheln = 20;
            const float laenge = kacheln * 24f;
            Vector2 pivot = mitte - normale * 6f;     // Kaikante (lokal z = +6) auf der Kuestenlinie

            var e = new Eintrag { Name = "Hafen", Label = place.Label, Pos = pivot, Yaw = yaw, Hoehe = 0f, Stempeln = false };

            // Land: Kai und Stapelflaeche auf +3, Wasser: Becken auf -14
            var land = new Flaeche(pivot + Drehen(new Vector2(0f, -65f), yaw), new Vector2(laenge + 20f, 142f), yaw);
            var becken = new Flaeche(pivot + Drehen(new Vector2(0f, 46f), yaw), new Vector2(laenge + 20f, 80f), yaw);
            e.Sperre.Add(land);
            e.Sperre.Add(becken);
            e.Ebnen.Add((land, 2.95f, 30f));
            e.Ebnen.Add((becken, -14f, 30f));

            float x0 = -laenge * 0.5f;
            for (int i = 0; i < kacheln; i++)
                e.Teile.Add(new Teil { Pfad = Hafen + "03_kaikante_24.glb", Lokal = new Vector3(x0 + 12f + i * 24f, 0f, 0f), Farben = false });

            // Containerschiff am westlichen Liegeplatz, Yacht am oestlichen
            e.Teile.Add(new Teil { Pfad = Hafen + "01_containerschiff.glb", Lokal = new Vector3(x0 + 160f, 0f, 28f) });
            e.Teile.Add(new Teil { Pfad = Yacht, Lokal = new Vector3(laenge * 0.5f - 90f, 0f, 20f), Heist = true });
            e.Teile.Add(new Teil { Pfad = Hafen + "02_schlepper.glb", Lokal = new Vector3(x0 + 330f, 0f, 66f), Yaw = 180f });

            // Containerbruecken auf den Schienen (Kran_Mitte: z -11.75, y 3)
            foreach (float kx in new[] { x0 + 80f, x0 + 150f, x0 + 220f })
                e.Teile.Add(new Teil { Pfad = Hafen + "08_containerkran.glb", Lokal = new Vector3(kx, 3f, -11.75f) });

            // Stapelflaeche
            var rng = new System.Random(4711);
            for (float cx = x0 + 24f; cx <= -x0 - 24f; cx += 16f)
            for (float cz = -48f; cz >= -112f; cz -= 16f)
            {
                if (rng.NextDouble() < 0.22) continue;
                e.Teile.Add(new Teil { Pfad = Hafen + "13_containerstapel.glb", Lokal = new Vector3(cx, 3f, cz) });
            }
            foreach (float px in new[] { -80f, 80f })
                e.Teile.Add(new Teil { Pfad = Hafen + "09_portalkran.glb", Lokal = new Vector3(px, 3f, -80f), Yaw = 90f });
            e.Teile.Add(new Teil { Pfad = Hafen + "10_containerstapler.glb", Lokal = new Vector3(-40f, 3f, -38f) });
            e.Teile.Add(new Teil { Pfad = Hafen + "10_containerstapler.glb", Lokal = new Vector3(60f, 3f, -38f), Yaw = 180f });
            for (int i = 0; i < 4; i++)
                e.Teile.Add(new Teil { Pfad = Hafen + "17_flutlichtmast.glb", Lokal = new Vector3(x0 + 60f + i * 120f, 3f, -36f), Farben = false });
            e.Teile.Add(new Teil { Pfad = Hafen + "14_hafenbuero.glb", Lokal = new Vector3(-x0 - 24f, 3f, -124f), Yaw = 180f });
            e.Teile.Add(new Teil { Pfad = Hafen + "15_hafentor.glb", Lokal = new Vector3(x0 + 40f, 3f, -130f), Yaw = 180f });

            _liste.Add(e);
        }

        // ------------------------------------------------------------------
        // An den Hausplaner und den Strassenplan
        // ------------------------------------------------------------------

        /// <summary>Sperrflaechen ins Belegungsraster eintragen, damit dort kein Haus hinkommt.</summary>
        public static void Belegen(RioHousePlanner planner)
        {
            if (_liste == null) return;
            foreach (var f in _sperren)
                planner.Occupy(f.Mitte, f.Groesse + new Vector2(12f, 12f), -f.Yaw);
        }

        /// <summary>Grundflaechen auf ihre Hoehe einebnen. Laeuft zwischen StempelBeginnen und StempelAnwenden.</summary>
        public static void Stempeln(RioWorldTerrain t)
        {
            if (_liste == null) return;
            foreach (var e in _liste)
            {
                if (e.Stempeln)
                    foreach (var f in e.Sperre)
                        t.Stempeln(f.Mitte, f.Groesse, -f.Yaw, e.Hoehe - 0.05f, e.Rand);
                foreach (var (f, h, rand) in e.Ebnen)
                    t.Stempeln(f.Mitte, f.Groesse, -f.Yaw, h - 0.05f, rand);
            }
        }

        // ------------------------------------------------------------------
        // Phase 3: Setzen
        // ------------------------------------------------------------------

        public static int Setzen(GameObject root, RioWorldTerrain t)
        {
            if (_liste == null || root == null) return 0;

            var wurzel = new GameObject("Wahrzeichen").transform;
            wurzel.SetParent(root.transform, false);

            int n = 0;
            foreach (var e in _liste)
            {
                if (e.Teile.Count == 0) continue;      // reine Sperrflaeche, z. B. Landebahn
                var gruppe = new GameObject(e.Name).transform;
                gruppe.SetParent(wurzel, false);
                gruppe.SetPositionAndRotation(new Vector3(e.Pos.x, e.Hoehe, e.Pos.y), Quaternion.Euler(0f, e.Yaw, 0f));

                GameObject erstes = null;
                foreach (var teil in e.Teile)
                {
                    var go = Instanz(teil.Pfad, gruppe, teil.Lokal, teil.Yaw);
                    if (go == null) continue;
                    foreach (var aus in teil.Aus)
                    {
                        var node = RioKanal.Finde(go.transform, aus);
                        if (node != null) node.gameObject.SetActive(false);
                    }
                    Einrichten(go, teil.Farben, teil.Heist, RioHousePlanner.Hash((int)e.Pos.x + n, (int)e.Pos.y));
                    if (erstes == null) erstes = go;
                    n++;
                }

                if (erstes != null)
                {
                    foreach (var (anker, pfad) in e.AnAnkern)
                    {
                        var a = RioKanal.Finde(erstes.transform, anker);
                        if (a == null) continue;
                        var auto = Instanz(pfad, gruppe, Vector3.zero, 0f);
                        if (auto == null) continue;
                        auto.transform.SetPositionAndRotation(a.position, Quaternion.Euler(0f, a.eulerAngles.y, 0f));
                        Einrichten(auto, true, false, RioHousePlanner.Hash((int)a.position.x, (int)a.position.z));
                        n++;
                    }
                }

                if (e.Name == "Casa_Imperio" && erstes != null) AbriegelungVorbereiten(erstes, gruppe);
            }

            n += FlugzeugeSetzen(wurzel, t);
            n += StrandSetzen(root.transform, t);
            return n;
        }

        static GameObject Instanz(string pfad, Transform parent, Vector3 lokal, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
            if (prefab == null)
            {
                Debug.LogWarning("[Rio] Modell fehlt: " + pfad);
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = lokal;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.name = prefab.name;
            return go;
        }

        /// <summary>
        /// Hitboxen: Modelle mit mitgelieferten __kollision-Nodes bekommen
        /// genau diese, alle anderen einen MeshCollider pro Teil.
        /// </summary>
        static void Einrichten(GameObject go, bool farben, bool heist, int seed)
        {
            bool mitHitboxen = false;
            foreach (var tr in go.GetComponentsInChildren<Transform>(true))
                if (tr.name.Contains(RioKollision.KollisionTag)) { mitHitboxen = true; break; }

            if (mitHitboxen)
            {
                RioKollisionEditor.Einrichten(go, true);
            }
            else
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (mf.sharedMesh == null || mf.GetComponent<Collider>() != null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                RioKollisionEditor.StatischMarkieren(go.transform, false);
            }

            if (heist)
            {
                RioHeist.Einrichten(go);
                if (RioKanal.Finde(go.transform, "Kanal_Geheimtor") != null && go.GetComponent<RioGeheimtor>() == null)
                    go.AddComponent<RioGeheimtor>();
                if (RioKanal.Finde(go.transform, "Kanal_Bootslift") != null && go.GetComponent<RioBootslift>() == null)
                    go.AddComponent<RioBootslift>();
            }

            if (farben && HatSlots(go))
            {
                var f = go.AddComponent<RioHausFarben>();
                f.seed = Mathf.Max(1, seed & 0x7fffffff);
                f.Anwenden();
            }
        }

        static bool HatSlots(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.name.Contains("slot_")) return true;
            return false;
        }

        /// <summary>Wie Tools ▸ Rio Heists ▸ Abriegelung vorbereiten: Version 03 deckungsgleich und inaktiv daneben.</summary>
        static void AbriegelungVorbereiten(GameObject normal, Transform gruppe)
        {
            var zu = Instanz(ImperioZu, gruppe, normal.transform.localPosition, normal.transform.localEulerAngles.y);
            if (zu == null) return;
            Einrichten(zu, true, true, 1);
            var steuer = new GameObject("Casa_Imperio_Abriegelung");
            steuer.transform.SetParent(gruppe, false);
            var ab = steuer.AddComponent<RioAbriegelung>();
            ab.Normal = normal;
            ab.Abgeriegelt = zu;
            zu.SetActive(false);
        }

        /// <summary>Auf jede kleine Landebahn zwei geparkte Flugzeuge.</summary>
        static int FlugzeugeSetzen(Transform wurzel, RioWorldTerrain t)
        {
            int n = 0;
            foreach (var place in RioWorldLayout.Places)
            {
                if (place.Kind != RioWorldLayout.Poi.FlughafenKlein) continue;
                float g = t.SampleHeight(place.Position) + 0.3f;
                var gruppe = new GameObject("Flugzeuge " + place.Label).transform;
                gruppe.SetParent(wurzel, false);
                foreach (float dx in new[] { -260f, 240f })
                {
                    var go = Instanz(Flugzeug, gruppe, Vector3.zero, 0f);
                    if (go == null) continue;
                    go.transform.SetPositionAndRotation(new Vector3(place.Position.x + dx, g, place.Position.y),
                                                        Quaternion.Euler(0f, dx < 0f ? 90f : -90f, 0f));
                    Einrichten(go, false, false, 1);
                    n++;
                }
            }
            return n;
        }

        // ------------------------------------------------------------------
        // Strand
        // ------------------------------------------------------------------

        struct StrandTyp
        {
            public string Datei; public int Anzahl; public float Abstand;
            public float MinKueste, MaxKueste;
            public StrandTyp(string datei, int anzahl, float abstand, float minK, float maxK)
            { Datei = datei; Anzahl = anzahl; Abstand = abstand; MinKueste = minK; MaxKueste = maxK; }
        }

        /// <summary>
        /// Strandobjekte auf dem Sandstreifen am Strandbogen. Nach der
        /// Anleitung: Kioske und Duschen nah an der Promenade, Schirme und
        /// Tuecher nah am Wasser, Rettungstuerme mit Abstand.
        /// </summary>
        static int StrandSetzen(Transform root, RioWorldTerrain t)
        {
            var typen = new[]
            {
                new StrandTyp("04_rettungsturm.glb",     4, 300f, 10f, 18f),
                new StrandTyp("07_strandkiosk.glb",      4, 160f, 15f, 21f),
                new StrandTyp("08_eisladen.glb",         2, 160f, 15f, 21f),
                new StrandTyp("11_barraca.glb",         10,  45f, 10f, 20f),
                new StrandTyp("05_duschen.glb",          5, 110f, 14f, 21f),
                new StrandTyp("15_volleyball.glb",       2, 150f, 10f, 18f),
                new StrandTyp("16_fussballtor.glb",      2, 150f,  8f, 16f),
                new StrandTyp("17_fitness.glb",          2, 200f, 16f, 21f),
                new StrandTyp("19_leihraeder.glb",       2, 200f, 16f, 21f),
                new StrandTyp("10_kokos_stand.glb",      3, 120f, 14f, 21f),
                new StrandTyp("09_eiswagen.glb",         3, 100f, 10f, 20f),
                new StrandTyp("02_palmengruppe.glb",     8,  60f, 16f, 21f),
                new StrandTyp("01_kokospalme.glb",      24,  18f, 12f, 21f),
                new StrandTyp("03_haengematte.glb",      2, 150f, 16f, 21f),
                new StrandTyp("20_fischerboot.glb",      3,  40f,  3f,  8f),
                new StrandTyp("18_surfbretter.glb",      3,  90f,  8f, 16f),
                new StrandTyp("06_muell_warnflagge.glb", 8,  70f,  8f, 20f),
                new StrandTyp("13_sonnenschirm_set.glb",45,   9f,  4f, 14f),
                new StrandTyp("14_strandtuch_set.glb",  30,   7f,  3f, 12f),
                new StrandTyp("12_sandburg.glb",         5,  40f,  3f,  9f),
            };

            // Kandidaten: Land, Sand, im Strandbogen
            var kandidaten = new List<(Vector2 p, float k)>();
            Vector2 c = RioWorldLayout.BeachCenter;
            float r = RioWorldLayout.BeachRadius;
            for (float x = c.x - r; x <= c.x + r; x += 3f)
            for (float z = c.y - r; z <= c.y + r; z += 3f)
            {
                var p = new Vector2(x, z);
                if (Vector2.Distance(p, c) > r) continue;
                float k = t.DistanceToCoast(p);
                if (k < 2f || k > 22f) continue;
                float h = t.SampleHeight(p);
                if (h < 0.15f || h > 4f) continue;
                if (Belegt(p, 4f)) continue;
                kandidaten.Add((p, k));
            }
            if (kandidaten.Count == 0)
            {
                Debug.LogWarning("[Rio] Strand: keine Sandflaeche im Strandbogen gefunden.");
                return 0;
            }

            var strand = new GameObject("Strand").transform;
            strand.SetParent(root, false);

            var rng = new System.Random(812702);
            var gesetzt = new List<(Vector2 p, float r)>();
            int n = 0;

            foreach (var typ in typen)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Strand + typ.Datei);
                if (prefab == null) continue;
                if (!Grundflaeche(Strand + typ.Datei, null, out _, out Vector2 gr)) continue;
                float eigen = Mathf.Max(gr.x, gr.y) * 0.5f + 1f;
                var eigene = new List<Vector2>();

                for (int versuch = 0; versuch < typ.Anzahl * 60 && eigene.Count < typ.Anzahl; versuch++)
                {
                    var (p, k) = kandidaten[rng.Next(kandidaten.Count)];
                    if (k < typ.MinKueste || k > typ.MaxKueste) continue;

                    bool frei = true;
                    foreach (var (q, qr) in gesetzt)
                        if (Vector2.Distance(p, q) < qr + eigen) { frei = false; break; }
                    if (!frei) continue;
                    foreach (var q in eigene)
                        if (Vector2.Distance(p, q) < typ.Abstand) { frei = false; break; }
                    if (!frei) continue;

                    // Front (+Z) zum Meer: gegen den Anstieg der Kuestenentfernung
                    float gx = t.DistanceToCoast(p + new Vector2(2f, 0f)) - t.DistanceToCoast(p - new Vector2(2f, 0f));
                    float gz = t.DistanceToCoast(p + new Vector2(0f, 2f)) - t.DistanceToCoast(p - new Vector2(0f, 2f));
                    var meer = -new Vector2(gx, gz);
                    float yaw = meer.sqrMagnitude > 1e-6f ? Mathf.Atan2(meer.x, meer.y) * Mathf.Rad2Deg : 0f;
                    if (typ.Datei.Contains("palme") || typ.Datei.Contains("sandburg") || typ.Datei.Contains("tuch"))
                        yaw += (float)rng.NextDouble() * 50f - 25f;

                    var go = Instanz(Strand + typ.Datei, strand, Vector3.zero, 0f);
                    if (go == null) break;
                    go.transform.SetPositionAndRotation(new Vector3(p.x, t.SampleHeight(p), p.y), Quaternion.Euler(0f, yaw, 0f));
                    Einrichten(go, true, false, rng.Next(1, int.MaxValue));
                    eigene.Add(p);
                    gesetzt.Add((p, eigen));
                    n++;
                }
            }
            return n;
        }

        // ------------------------------------------------------------------
        // Hilfen
        // ------------------------------------------------------------------

        /// <summary>Lokale Verschiebung (x, z) um yaw drehen, wie Quaternion.Euler(0, yaw, 0).</summary>
        public static Vector2 Drehen(Vector2 v, float yaw)
        {
            var w = Quaternion.Euler(0f, yaw, 0f) * new Vector3(v.x, 0f, v.y);
            return new Vector2(w.x, w.z);
        }

        static Vector2 InsLokale(Vector2 v, float yaw) => Drehen(v, -yaw);

        static readonly Dictionary<string, (Vector2 mitte, Vector2 groesse)> _cache =
            new Dictionary<string, (Vector2, Vector2)>();

        /// <summary>
        /// Grundflaeche eines Modells in seinem eigenen System, aus den
        /// Vertices gerechnet. Die Mesh-Bounds der importierten glb-Dateien
        /// stehen auf null und taugen dafuer nicht. Hitbox-Nodes und die
        /// abgeschalteten Nodes zaehlen nicht mit.
        /// </summary>
        static bool Grundflaeche(string pfad, string[] aus, out Vector2 mitte, out Vector2 groesse)
        {
            string key = pfad + "|" + (aus == null ? "" : string.Join(",", aus));
            if (_cache.TryGetValue(key, out var c)) { mitte = c.mitte; groesse = c.groesse; return true; }
            mitte = Vector2.zero; groesse = Vector2.zero;

            var go = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
            if (go == null) return false;

            var inv = go.transform.worldToLocalMatrix;
            bool erstes = true;
            float x0 = 0, x1 = 0, z0 = 0, z1 = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.name.Contains("__kollision") || mf.name.Contains("__trigger")) continue;
                if (aus != null && UnterAus(mf.transform, go.transform, aus)) continue;
                var m = inv * mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (erstes) { x0 = x1 = w.x; z0 = z1 = w.z; erstes = false; continue; }
                    if (w.x < x0) x0 = w.x; if (w.x > x1) x1 = w.x;
                    if (w.z < z0) z0 = w.z; if (w.z > z1) z1 = w.z;
                }
            }
            if (erstes) return false;
            mitte = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
            groesse = new Vector2(x1 - x0, z1 - z0);
            _cache[key] = (mitte, groesse);
            return true;
        }

        static bool UnterAus(Transform t, Transform wurzel, string[] aus)
        {
            for (var p = t; p != null && p != wurzel; p = p.parent)
                foreach (var a in aus) if (p.name == a) return true;
            return false;
        }

        /// <summary>
        /// Achsparallele xz-Huelle aller Meshes einer Instanz in Weltkoordinaten.
        /// Mit Stichwort nur Nodes, in deren Pfad es vorkommt; mit nurOben nur
        /// Nodes, deren oberster Knoten unter der Wurzel damit anfaengt.
        /// </summary>
        static Rect WeltHuelle(GameObject go, string stichwort, bool nurOben)
        {
            bool erstes = true;
            float x0 = 0, x1 = 0, z0 = 0, z1 = 0;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.name.Contains("__kollision") || mf.name.Contains("__trigger")) continue;
                if (stichwort != null)
                {
                    if (nurOben)
                    {
                        var oben = mf.transform;
                        while (oben.parent != null && oben.parent != go.transform) oben = oben.parent;
                        if (!oben.name.StartsWith(stichwort)) continue;
                    }
                    else if (!PfadEnthaelt(mf.transform, go.transform, stichwort)) continue;
                }
                var m = mf.transform.localToWorldMatrix;
                foreach (var v in mf.sharedMesh.vertices)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (erstes) { x0 = x1 = w.x; z0 = z1 = w.z; erstes = false; continue; }
                    if (w.x < x0) x0 = w.x; if (w.x > x1) x1 = w.x;
                    if (w.z < z0) z0 = w.z; if (w.z > z1) z1 = w.z;
                }
            }
            return erstes ? new Rect() : new Rect(x0, z0, x1 - x0, z1 - z0);
        }

        static bool PfadEnthaelt(Transform t, Transform wurzel, string stichwort)
        {
            for (var p = t; p != null && p != wurzel; p = p.parent)
                if (p.name.Contains(stichwort)) return true;
            return false;
        }
    }
}
