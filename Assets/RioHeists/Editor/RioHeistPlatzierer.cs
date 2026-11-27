using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools ▸ Rio Heists ▸ Platzhalter ersetzen
/// Platzhalter (Name beginnt mit „Platzhalter“) mit „palast“, „imperio“, „megabank“ oder „heist“ im Namen → Casa Império,
/// mit „yacht“ oder „soberana“ → Megayacht SOBERANA, zusätzlich „abgeriegelt“ im Namen → Casa Império abgeriegelt (03). Position = Mitte der Platzhalter-Unterkante (Yacht: Unterkante = Wasserlinie),
/// Rotation = Platzhalter-Rotation. Richtet Hitboxen (RioKollision), Static-Flags und die Beute-Komponenten (RioHeist) ein.
/// </summary>
public static class RioHeistPlatzierer
{
    const string Ordner = "Assets/RioHeists/Modelle/";

    [MenuItem("Tools/Rio Heists/Platzhalter ersetzen")]
    static void Ersetzen()
    {
        var palast = AssetDatabase.LoadAssetAtPath<GameObject>(Ordner + "01_casa_imperio.glb");
        var yacht = AssetDatabase.LoadAssetAtPath<GameObject>(Ordner + "02_yacht_soberana.glb");
        var zu = AssetDatabase.LoadAssetAtPath<GameObject>(Ordner + "03_casa_imperio_abgeriegelt.glb");
        if (palast == null && yacht == null)
        {
            EditorUtility.DisplayDialog("Rio Heists", "Keine Modelle in " + Ordner + " gefunden (glTFast installiert?).", "OK");
            return;
        }
        var liste = new List<Transform>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var szene = SceneManager.GetSceneAt(i);
            if (!szene.isLoaded) continue;
            foreach (var root in szene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(false))
                    if (t.name.StartsWith("Platzhalter", System.StringComparison.OrdinalIgnoreCase)) liste.Add(t);
        }
        Undo.SetCurrentGroupName("Rio Heists platzieren");
        int gruppe = Undo.GetCurrentGroup();
        int ok = 0;
        foreach (var ph in liste)
        {
            string n = ph.name.ToLowerInvariant();
            GameObject prefab = null;
            if (n.Contains("yacht") || n.Contains("soberana")) prefab = yacht;
            else if (n.Contains("abgeriegelt")) prefab = zu;
            else if (n.Contains("palast") || n.Contains("imperio") || n.Contains("megabank") || n.Contains("heist")) prefab = palast;
            if (prefab == null) continue;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ph.parent);
            Undo.RegisterCreatedObjectUndo(go, "Rio Heist");
            go.transform.SetPositionAndRotation(MitteUnten(ph), ph.rotation);
            Vector3 ps = ph.parent != null ? ph.parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);
            go.name = prefab.name;
            RioKollisionEditor.Einrichten(go, true);               // Hitboxen + statisch (bewegliche Teile bleiben beweglich)
            RioHeist.Einrichten(go);                               // Beute-Komponenten
            Undo.RecordObject(ph.gameObject, "Platzhalter ausblenden");
            ph.gameObject.SetActive(false);
            ok++;
        }
        Undo.CollapseUndoOperations(gruppe);
        EditorUtility.DisplayDialog("Rio Heists", ok + " Heist-Schauplatz/-plätze gesetzt.", "OK");
    }

    /// <summary>Tools ▸ Rio Heists ▸ Abriegelung vorbereiten: Casa Império (01) auswählen → die abgeriegelte Version (03) wird
    /// deckungsgleich daneben angelegt (inaktiv), Hitboxen + Beute eingerichtet und ein Objekt „Casa_Imperio_Abriegelung“ mit
    /// RioAbriegelung erzeugt. Im Spiel: FindObjectOfType&lt;RioAbriegelung&gt;().Abriegeln();</summary>
    [MenuItem("Tools/Rio Heists/Abriegelung vorbereiten (Auswahl = Casa Império)")]
    static void AbriegelungVorbereiten()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { EditorUtility.DisplayDialog("Rio Heists", "Bitte die platzierte Casa Império (01_casa_imperio) auswählen.", "OK"); return; }
        var normal = PrefabUtility.GetOutermostPrefabInstanceRoot(sel) ?? sel;
        var zu = AssetDatabase.LoadAssetAtPath<GameObject>(Ordner + "03_casa_imperio_abgeriegelt.glb");
        if (zu == null) { EditorUtility.DisplayDialog("Rio Heists", "03_casa_imperio_abgeriegelt.glb fehlt in " + Ordner, "OK"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(zu, normal.transform.parent);
        Undo.RegisterCreatedObjectUndo(go, "Abriegelung");
        go.transform.SetPositionAndRotation(normal.transform.position, normal.transform.rotation);
        go.transform.localScale = normal.transform.localScale;
        go.name = zu.name;
        RioKollisionEditor.Einrichten(go, true);
        RioHeist.Einrichten(go);
        var steuer = new GameObject("Casa_Imperio_Abriegelung");
        Undo.RegisterCreatedObjectUndo(steuer, "Abriegelung");
        steuer.transform.SetParent(normal.transform.parent, false);
        var ab = steuer.AddComponent<RioAbriegelung>();
        ab.Normal = normal;
        ab.Abgeriegelt = go;
        go.SetActive(false);
        Selection.activeGameObject = steuer;
        EditorUtility.DisplayDialog("Rio Heists", "Abgeriegelte Version angelegt (inaktiv). Im Spiel RioAbriegelung.Abriegeln() aufrufen.", "OK");
    }

    static Vector3 MitteUnten(Transform t)
    {
        Bounds lb;
        var mf = t.GetComponent<MeshFilter>();
        var bc = t.GetComponent<BoxCollider>();
        if (mf != null && mf.sharedMesh != null) lb = mf.sharedMesh.bounds;
        else if (bc != null) lb = new Bounds(bc.center, bc.size);
        else lb = new Bounds(Vector3.zero, Vector3.one);
        return t.TransformPoint(new Vector3(lb.center.x, lb.min.y, lb.center.z));
    }
}
