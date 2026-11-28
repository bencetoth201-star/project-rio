using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools ▸ Rio Häuser ▸ Platzhalter ersetzen
/// Sucht Platzhalter-Objekte in der Szene, misst ihre Grundfläche und setzt ein passendes Rio-Haus ein.
/// Vorderseite des Hauses = blaue Achse (Z / forward) des Platzhalters. Unterkante = Unterkante des Platzhalters.
/// </summary>
public class RioHausPlatzierer : EditorWindow
{
    string ordner = "Assets/RioHaeuser/Modelle";
    string praefix = "Platzhalter";
    bool nurAuswahl = false;
    float toleranz = 0.5f;
    bool drehenErlaubt = false;
    bool platzhalterLoeschen = false;
    bool collider = true;
    bool statisch = true;
    bool farben = true;
    int seed = 12345;

    [MenuItem("Tools/Rio Häuser/Platzhalter ersetzen")]
    static void Oeffnen()
    {
        GetWindow<RioHausPlatzierer>("Rio Häuser");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Modelle", EditorStyles.boldLabel);
        ordner = EditorGUILayout.TextField("Ordner", ordner);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Platzhalter", EditorStyles.boldLabel);
        praefix = EditorGUILayout.TextField("Name beginnt mit", praefix);
        nurAuswahl = EditorGUILayout.Toggle("Nur ausgewählte", nurAuswahl);
        toleranz = EditorGUILayout.FloatField("Toleranz (m)", toleranz);
        drehenErlaubt = EditorGUILayout.Toggle("Um 90° drehen erlaubt", drehenErlaubt);
        platzhalterLoeschen = EditorGUILayout.Toggle("Platzhalter löschen", platzhalterLoeschen);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Häuser", EditorStyles.boldLabel);
        collider = EditorGUILayout.Toggle("Hitboxen einrichten", collider);
        statisch = EditorGUILayout.Toggle("Als statisch markieren", statisch);
        farben = EditorGUILayout.Toggle("Zufallsfarben", farben);
        seed = EditorGUILayout.IntField("Seed", seed);

        EditorGUILayout.HelpBox(
            "Kategorie über den Namen des Platzhalters festlegen (optional):\n" +
            "…_sehrarm, …_arm, …_einfach, …_mittel, …_gehoben, …_reich, …_luxus\n" +
            "oder ein bestimmtes Haus: …_boteco, …_villa_zona_sul usw.\n" +
            "Nur per Name: …_tankstelle, …_bank, …_kirche, …_lagerhalle, …_praca, …_park, …_club, …_tuning, …_kleider, …_stadion, …_juwelier,\n" +
            "…_centro/…_hochhaus (Türme), …_ecke (Eckgebäude: zweite Straße an +X)\n" +
            "Ohne Angabe wird das größte passende Haus gewählt.\n" +
            "Heist-Platzhalter (…_palast, …_imperio, …_megabank, …_heist, …_yacht) werden hier übersprungen –\n" +
            "die setzt Tools ▸ Rio Heists ▸ Platzhalter ersetzen (Paket RioHeists).",
            MessageType.Info);

        if (GUILayout.Button("Platzhalter ersetzen", GUILayout.Height(32))) Ersetzen();
        if (GUILayout.Button("Farben der Auswahl neu würfeln")) Wuerfeln();
    }

    struct Kandidat
    {
        public RioHausDaten.Haus haus;
        public GameObject prefab;
    }

    List<Kandidat> ModelleLaden()
    {
        var liste = new Dictionary<string, Kandidat>();
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { ordner }))
        {
            string pfad = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(pfad);
            if (go == null || !RioHausDaten.Finde(go.name, out var haus)) continue;
            // .glb bevorzugen, falls dasselbe Haus auch als .obj im Ordner liegt
            if (liste.ContainsKey(haus.slug) && !pfad.EndsWith(".glb")) continue;
            liste[haus.slug] = new Kandidat { haus = haus, prefab = go };
        }
        return liste.Values.ToList();
    }

    List<Transform> PlatzhalterFinden()
    {
        IEnumerable<Transform> quelle;
        if (nurAuswahl)
            quelle = Selection.transforms;
        else
        {
            var alle = new List<Transform>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var szene = SceneManager.GetSceneAt(i);
                if (!szene.isLoaded) continue;
                foreach (var root in szene.GetRootGameObjects())
                    alle.AddRange(root.GetComponentsInChildren<Transform>(false));
            }
            quelle = alle;
        }
        return quelle.Where(t => t != null && t.gameObject.activeInHierarchy && !IstHeistPlatzhalter(t.name) &&
                                 t.name.StartsWith(praefix, System.StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Casa Império und die Yacht liegen im Heist-Paket (Tools ▸ Rio Heists ▸ Platzhalter ersetzen).</summary>
    public static bool IstHeistPlatzhalter(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("palast") || n.Contains("imperio") || n.Contains("megabank") || n.Contains("heist") ||
               n.Contains("yacht") || n.Contains("soberana");
    }

    static void Fussabdruck(Transform t, out Vector3 mitteUnten, out float breite, out float tiefe)
    {
        Bounds lb;
        var mf = t.GetComponent<MeshFilter>();
        var bc = t.GetComponent<BoxCollider>();
        if (mf != null && mf.sharedMesh != null) lb = mf.sharedMesh.bounds;
        else if (bc != null) lb = new Bounds(bc.center, bc.size);
        else lb = new Bounds(Vector3.zero, Vector3.one);
        Vector3 s = t.lossyScale;
        breite = Mathf.Abs(lb.size.x * s.x);
        tiefe = Mathf.Abs(lb.size.z * s.z);
        mitteUnten = t.TransformPoint(new Vector3(lb.center.x, lb.min.y, lb.center.z));
    }

    static readonly (string schluessel, string klasse)[] Klassen =
    {
        ("sehrarm", "Sehr arm"), ("sehr_arm", "Sehr arm"), ("sehr arm", "Sehr arm"),
        ("luxus", "Luxus"), ("gehoben", "Gehoben"), ("reich", "Reich"),
        ("centro", "Centro"), ("hochhaus", "Centro"), ("gewerbe", "Gewerbe"), ("lager", "Gewerbe"),
        ("ecke", "Ecke"), ("frei", "Freifläche"), ("mittel", "Mittel"), ("einfach", "Einfach"), ("arm", "Arm"),
    };

    // Diese Klassen werden nie zufällig gesetzt, nur per Name/Kategorie im Platzhalter-Namen
    static readonly HashSet<string> NurPerName = new HashSet<string> { "Spezial", "Centro", "Gewerbe", "Ecke", "Freifläche" };

    static List<Kandidat> Filtern(string name, List<Kandidat> alle)
    {
        string n = name.ToLowerInvariant();
        // 1) bestimmtes Gebäude im Namen? (Spezial-Gebäude reicht das erste Wort, z. B. "tankstelle")
        foreach (var k in alle)
        {
            string ohneNummer = k.haus.slug.Substring(k.haus.slug.IndexOf('_') + 1);
            string erstesWort = ohneNummer.Split('_')[0];
            bool alias = NurPerName.Contains(k.haus.klasse) && k.haus.klasse != "Ecke" && k.haus.klasse != "Centro" &&
                         ((erstesWort != "casa" && n.Contains(erstesWort)) || (erstesWort == "banco" && n.Contains("bank")) ||
                          (erstesWort == "igreja" && n.Contains("kirche")) || (erstesWort == "parque" && n.Contains("park")) ||
                          (erstesWort == "clube" && (n.Contains("club") || n.Contains("disco"))) ||
                          (erstesWort == "garagem" && (n.Contains("tuning") || n.Contains("werkstatt"))) ||
                          (erstesWort == "mega" && (n.Contains("kleider") || n.Contains("clothing") || n.Contains("loja") || n.Contains("moda"))) ||
                          (erstesWort == "estadio" && (n.Contains("stadion") || n.Contains("stadium") || n.Contains("maracana"))) ||
                          (erstesWort == "esmeralda" && !n.Contains("palast") && !n.Contains("imperio") && (n.Contains("juwel") || n.Contains("jewel") || n.Contains("joia"))) ||
                          (erstesWort == "casa" && (n.Contains("imperio") || n.Contains("palast") || n.Contains("heist"))));
            if (n.Contains(ohneNummer) || alias)
                return new List<Kandidat> { k };
        }
        // 2) Kategorie im Namen? (auch Centro, Gewerbe, Ecke, Freifläche)
        foreach (var (schluessel, klasse) in Klassen)
            if (n.Contains(schluessel))
                return alle.Where(k => k.haus.klasse == klasse && k.haus.klasse != "Spezial").ToList();
        // 3) ohne Angabe: nur normale Wohnhäuser
        return alle.Where(k => !NurPerName.Contains(k.haus.klasse)).ToList();
    }

    void Ersetzen()
    {
        var modelle = ModelleLaden();
        if (modelle.Count == 0)
        {
            EditorUtility.DisplayDialog("Rio Häuser", "Im Ordner „" + ordner + "“ wurden keine Häuser gefunden.", "OK");
            return;
        }
        var platzhalter = PlatzhalterFinden();
        if (platzhalter.Count == 0)
        {
            EditorUtility.DisplayDialog("Rio Häuser", "Keine aktiven Objekte gefunden, deren Name mit „" + praefix + "“ beginnt.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Rio Häuser platzieren");
        int gruppe = Undo.GetCurrentGroup();
        var rng = new System.Random(seed);
        int ok = 0, zuGross = 0;

        foreach (var ph in platzhalter)
        {
            Fussabdruck(ph, out Vector3 pos, out float b, out float t);
            var auswahl = Filtern(ph.name, modelle);
            if (auswahl.Count == 0) auswahl = modelle.Where(k => !NurPerName.Contains(k.haus.klasse)).ToList();

            // passende Häuser (Grundstück passt in den Platzhalter), auf Wunsch auch um 90° gedreht
            var passend = new List<(Kandidat k, bool gedreht, float flaeche)>();
            foreach (var k in auswahl)
            {
                float w = k.haus.lotBreite, d = k.haus.lotTiefe;
                if (w <= b + toleranz && d <= t + toleranz) passend.Add((k, false, w * d));
                else if (drehenErlaubt && d <= b + toleranz && w <= t + toleranz) passend.Add((k, true, w * d));
            }

            Kandidat wahl;
            bool drehen = false;
            if (passend.Count > 0)
            {
                // unter den gut füllenden Häusern (>= 70 % der besten Fläche) zufällig wählen -> Abwechslung
                float beste = passend.Max(p => p.flaeche);
                var gute = passend.Where(p => p.flaeche >= beste * 0.7f).ToList();
                var p0 = gute[rng.Next(gute.Count)];
                wahl = p0.k;
                drehen = p0.gedreht;
            }
            else
            {
                wahl = auswahl.OrderBy(k => k.haus.lotBreite * k.haus.lotTiefe).First();
                zuGross++;
                Debug.LogWarning($"[Rio Häuser] „{ph.name}“ ({b:0.#} × {t:0.#} m) ist kleiner als jedes passende Haus – " +
                                 $"{wahl.haus.name} ({wahl.haus.lotBreite} × {wahl.haus.lotTiefe} m) gesetzt.", ph);
            }

            var haus = (GameObject)PrefabUtility.InstantiatePrefab(wahl.prefab, ph.parent);
            Undo.RegisterCreatedObjectUndo(haus, "Rio Haus");
            Quaternion rot = ph.rotation * (drehen ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity);
            haus.transform.SetPositionAndRotation(pos, rot);
            Vector3 ps = ph.parent != null ? ph.parent.lossyScale : Vector3.one;
            haus.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);
            haus.name = wahl.prefab.name;

            if (collider)
                RioKollision.Einrichten(haus);                      // mitgelieferte Hitboxen (…__kollision / …__trigger_n)

            if (statisch)
                RioKollisionEditor.StatischMarkieren(haus.transform, false);   // bewegliche Teile bleiben beweglich

            if (farben)
            {
                var f = Undo.AddComponent<RioHausFarben>(haus);
                f.seed = rng.Next(1, int.MaxValue);
                f.Anwenden();
            }

            if (platzhalterLoeschen) Undo.DestroyObjectImmediate(ph.gameObject);
            else
            {
                Undo.RecordObject(ph.gameObject, "Platzhalter ausblenden");
                ph.gameObject.SetActive(false);
            }
            ok++;
        }

        Undo.CollapseUndoOperations(gruppe);
        string text = ok + " Häuser gesetzt.";
        if (zuGross > 0) text += "\n" + zuGross + " Platzhalter waren zu klein – Details in der Konsole.";
        EditorUtility.DisplayDialog("Rio Häuser", text, "OK");
    }

    void Wuerfeln()
    {
        int n = 0;
        foreach (var go in Selection.gameObjects)
            foreach (var f in go.GetComponentsInChildren<RioHausFarben>())
            {
                Undo.RecordObject(f, "Farben würfeln");
                f.seed = Random.Range(1, int.MaxValue);
                f.Anwenden();
                EditorUtility.SetDirty(f);
                n++;
            }
        if (n == 0) EditorUtility.DisplayDialog("Rio Häuser", "Wähle zuerst ein oder mehrere Häuser aus.", "OK");
    }
}
