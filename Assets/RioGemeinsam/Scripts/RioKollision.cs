using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitboxen für alle Rio-Modelle (Häuser, Straßen, Strand, Hafen, Yacht).
/// Jedes Modell enthält unsichtbare Extra-Nodes:
///   "&lt;Ebene&gt;__kollision"    vereinfachte feste Hitbox (MeshCollider, nicht convex)
///   "&lt;Ebene&gt;__trigger_&lt;n&gt;"  Auslöser für Laser, Pool, Flutwasser (MeshCollider convex + isTrigger)
/// Alle anderen Mesh-Nodes sind nur Grafik und bekommen KEINEN Collider.
/// Bewegliche Teile (Türen, Tore, Laser, Aufzüge, Boote …) sind Nodes ohne "__" mit Kindern:
/// sie bekommen einen kinematischen Rigidbody, ihre Hitbox bewegt sich mit.
/// Aufruf: RioKollision.Einrichten(modell) – im Editor über Tools ▸ Rio ▸ Hitboxen einrichten,
/// zur Laufzeit direkt nach dem Instanziieren.
/// </summary>
public static class RioKollision
{
    public const string KollisionTag = "__kollision";
    public const string TriggerTag = "__trigger";

    /// <summary>Richtet alle Hitboxen unter root ein. Gibt die Anzahl der Collider zurück.</summary>
    public static int Einrichten(GameObject root)
    {
        int n = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var go = mf.gameObject;
            foreach (var alt in go.GetComponents<MeshCollider>()) Entfernen(alt);   // alte Voll-Collider (ältere Platzierer-Version)
            bool kol = go.name.Contains(KollisionTag);
            bool trig = go.name.Contains(TriggerTag);
            if (!kol && !trig || mf.sharedMesh == null) continue;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            if (trig)
            {
                mc.convex = true;
                mc.isTrigger = true;
            }
            n++;
        }
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!IstBeweglich(t) || t.GetComponent<Rigidbody>() != null) continue;
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        return n;
    }

    /// <summary>Bewegliches Teil = eigener Pivot-Node: kein "__" im Namen, hat Mesh-Kinder, ist nicht die Wurzel.</summary>
    public static bool IstBeweglich(Transform t)
    {
        if (t.parent == null || t.name.Contains("__") || t.childCount == 0) return false;
        for (int i = 0; i < t.childCount; i++)
            if (t.GetChild(i).name.Contains("__")) return true;
        return false;
    }

    /// <summary>Alle Hitbox- und Trigger-Collider eines beweglichen Teils (z. B. zum Abschalten eines Lasers).</summary>
    public static List<Collider> ColliderVon(Transform teil)
    {
        var liste = new List<Collider>();
        foreach (var c in teil.GetComponentsInChildren<Collider>(true)) liste.Add(c);
        return liste;
    }

    static void Entfernen(Object o)
    {
        if (Application.isPlaying) Object.Destroy(o);
        else Object.DestroyImmediate(o);
    }
}
