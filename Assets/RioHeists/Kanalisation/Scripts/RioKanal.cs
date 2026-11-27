using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maya-Kanalisation (Baukasten): Teile über ihre Anker zusammenstecken – wie beim Straßenbaukasten.
/// Waagerecht: "Anschluss_Kanal_N/S/E/W" (Wasserlinie), "Anschluss_Palast" ↔ Palast-Anker "Kanal_Anschluss".
///   → Andocken(teil, teilAnker, zielAnker): gleiche Position, Blickrichtungen entgegengesetzt.
/// Senkrecht: "Unten" des neuen Teils auf "Oben" des unteren Teils (Bootsversteck → Liftschacht → … → Garage,
///   Einstieg → Schacht rund → … → Gullydeckel). → Stapeln(teil, teilUnten, zielOben): gleiche Position und Drehung.
/// glTFast spiegelt X: deshalb immer mit den Anker-Transforms rechnen, nie mit Zahlen aus der Anleitung.
/// </summary>
public static class RioKanal
{
    /// <summary>Teil so verschieben/drehen, dass teilAnker auf zielAnker sitzt und in die Gegenrichtung schaut.</summary>
    public static void Andocken(Transform teil, Transform teilAnker, Transform zielAnker)
    {
        Vector3 f = Vector3.ProjectOnPlane(-zielAnker.forward, Vector3.up);
        Vector3 a = Vector3.ProjectOnPlane(teilAnker.forward, Vector3.up);
        if (f.sqrMagnitude < 1e-6f || a.sqrMagnitude < 1e-6f) { Stapeln(teil, teilAnker, zielAnker); return; }
        Quaternion dreh = Quaternion.FromToRotation(a.normalized, f.normalized);
        dreh = Quaternion.Euler(0f, dreh.eulerAngles.y, 0f);                       // nur um Y drehen
        teil.rotation = dreh * teil.rotation;
        teil.position += zielAnker.position - teilAnker.position;
    }

    /// <summary>Senkrechtes Stapeln: teilAnker ("Unten") genau auf zielAnker ("Oben"), gleiche Drehung um Y.</summary>
    public static void Stapeln(Transform teil, Transform teilAnker, Transform zielAnker)
    {
        float dy = zielAnker.eulerAngles.y - teilAnker.eulerAngles.y;
        teil.rotation = Quaternion.Euler(0f, dy, 0f) * teil.rotation;
        teil.position += zielAnker.position - teilAnker.position;
    }

    /// <summary>Anker oder bewegliches Teil per Name unter root suchen.</summary>
    public static Transform Finde(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>Alle Transforms mit diesem Namen in den geladenen Szenen (z. B. "Kanal_Anschluss", "Bootslift_Ziel").</summary>
    public static List<Transform> AlleInSzene(string name)
    {
        var liste = new List<Transform>();
        foreach (var t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
            if (t.name == name) liste.Add(t);
        return liste;
    }

    /// <summary>Nächster Transform mit diesem Namen zu einem Punkt (oder null).</summary>
    public static Transform Naechster(string name, Vector3 von)
    {
        Transform beste = null;
        float d = float.MaxValue;
        foreach (var t in AlleInSzene(name))
        {
            float e = (t.position - von).sqrMagnitude;
            if (e < d) { d = e; beste = t; }
        }
        return beste;
    }

    /// <summary>Oberstes Modell-Objekt zu einem Anker (die glTF-Instanz in der Szene).</summary>
    public static Transform TeilVon(Transform anker)
    {
        Transform t = anker;
        while (t.parent != null && !t.name.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) && !t.name.Contains("kanal_") && !t.name.Contains("casa_imperio"))
            t = t.parent;
        return t;
    }
}
