using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools ▸ Rio ▸ Hitboxen einrichten (Auswahl / ganze Szene)
/// Richtet für jedes Rio-Modell die mitgelieferten Hitboxen ein (siehe RioKollision)
/// und markiert es als statisch – bewegliche Teile (Türen, Tore, Laser, Aufzüge, Boote) bleiben beweglich.
/// </summary>
public static class RioKollisionEditor
{
    [MenuItem("Tools/Rio/Hitboxen einrichten (Auswahl)")]
    static void Auswahl()
    {
        int modelle = 0, collider = 0;
        foreach (var go in Selection.gameObjects)
        {
            collider += Einrichten(go, true);
            modelle++;
        }
        EditorUtility.DisplayDialog("Rio Hitboxen", $"{modelle} Modelle, {collider} Hitboxen eingerichtet.", "OK");
    }

    [MenuItem("Tools/Rio/Hitboxen einrichten (ganze Szene)")]
    static void Szene()
    {
        var wurzeln = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
        {
            if (!t.name.Contains(RioKollision.KollisionTag) || t.parent == null) continue;
            var p = t.parent;
            while (p.parent != null && RioKollision.IstBeweglich(p)) p = p.parent;   // Hitbox eines beweglichen Teils -> Modell
            wurzeln.Add(p.gameObject);
        }
        int collider = 0;
        foreach (var go in wurzeln) collider += Einrichten(go, true);
        EditorUtility.DisplayDialog("Rio Hitboxen", $"{wurzeln.Count} Modelle, {collider} Hitboxen eingerichtet.", "OK");
    }

    /// <summary>Hitboxen + Static-Flags für ein Modell (mit Undo).</summary>
    public static int Einrichten(GameObject modell, bool statisch)
    {
        Undo.RegisterFullObjectHierarchyUndo(modell, "Rio Hitboxen");
        int n = RioKollision.Einrichten(modell);
        if (statisch) StatischMarkieren(modell.transform, false);
        return n;
    }

    /// <summary>Static-Flags setzen, aber nie auf beweglichen Teilen und ihren Kindern (sonst bewegen sie sich nicht).</summary>
    public static void StatischMarkieren(Transform t, bool inBeweglichem)
    {
        bool beweglich = inBeweglichem || RioKollision.IstBeweglich(t);
        var flags = (StaticEditorFlags)0;
        if (!beweglich)
        {
            flags = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
            if (!t.name.Contains(RioKollision.KollisionTag) && !t.name.Contains(RioKollision.TriggerTag))
                flags |= StaticEditorFlags.OccluderStatic;
        }
        GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
        for (int i = 0; i < t.childCount; i++) StatischMarkieren(t.GetChild(i), beweglich);
    }
}
