using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools ▸ Rio Heists ▸ Kanal …  – Werkzeuge für den Maya-Kanal-Baukasten (Assets/RioHeists/Kanalisation).
/// 1. „Kanal an Palast andocken“: ausgewähltes Teil mit Anker „Anschluss_Palast“ (17_kanal_anschluss_palast oder das
///    Beispielnetz) an den nächsten Palast-Anker „Kanal_Anschluss“ setzen.
/// 2. „Zwei Anker verbinden“: zwei Anker auswählen (Strg/Cmd-Klick). Das zuletzt angeklickte (aktive) ist das Ziel und bleibt stehen,
///    das Teil des anderen Ankers wird angesetzt. „Unten“/„Oben“ = stapeln, sonst andocken (Blickrichtungen entgegengesetzt).
/// 3. „Kanal-Teile einrichten“: Hitboxen (RioKollision), Static-Flags, Beute/Ausrüstung (RioHeist) und die Skripte
///    RioGeheimtor/RioBootslift für alle ausgewählten Teile.
/// </summary>
public static class RioKanalWerkzeug
{
    [MenuItem("Tools/Rio Heists/Kanal an Palast andocken")]
    static void AnPalast()
    {
        var teil = Selection.activeTransform;
        if (teil == null) { Meldung("Bitte das Kanal-Teil (Anschluss Palast oder Beispielnetz) auswählen."); return; }
        teil = Wurzel(teil);
        var a = RioKanal.Finde(teil, "Anschluss_Palast");
        if (a == null) { Meldung("Das ausgewählte Teil hat keinen Anker „Anschluss_Palast“."); return; }
        var ziel = RioKanal.Naechster("Kanal_Anschluss", a.position);
        if (ziel == null) { Meldung("Kein Palast mit Anker „Kanal_Anschluss“ in der Szene (Casa Império zuerst platzieren)."); return; }
        Undo.RecordObject(teil, "Kanal andocken");
        RioKanal.Andocken(teil, a, ziel);
        Meldung("Angedockt an " + Wurzel(ziel).name + ". Die Maya-Wand im Palast (UG3_Kanalwand) ist die Sprengstelle.");
    }

    [MenuItem("Tools/Rio Heists/Zwei Anker verbinden")]
    static void Verbinden()
    {
        var sel = Selection.transforms;
        var ziel = Selection.activeTransform;
        if (sel.Length != 2 || ziel == null) { Meldung("Genau zwei Anker auswählen; der zuletzt angeklickte ist das Ziel."); return; }
        var anker = sel[0] == ziel ? sel[1] : sel[0];
        var teil = Wurzel(anker);
        if (teil == Wurzel(ziel)) { Meldung("Beide Anker gehören zum selben Teil."); return; }
        Undo.RecordObject(teil, "Kanal verbinden");
        bool senkrecht = anker.name == "Unten" || anker.name == "Oben" || ziel.name == "Unten" || ziel.name == "Oben";
        if (senkrecht) RioKanal.Stapeln(teil, anker, ziel);
        else RioKanal.Andocken(teil, anker, ziel);
    }

    [MenuItem("Tools/Rio Heists/Kanal-Teile einrichten (Auswahl)")]
    static void Einrichten()
    {
        var fertig = new HashSet<Transform>();
        foreach (var t in Selection.transforms)
        {
            var w = Wurzel(t);
            if (!fertig.Add(w)) continue;
            Undo.RegisterFullObjectHierarchyUndo(w.gameObject, "Kanal einrichten");
            RioKollisionEditor.Einrichten(w.gameObject, true);
            RioHeist.Einrichten(w.gameObject);
            if (RioKanal.Finde(w, "Kanal_Geheimtor") != null && w.GetComponent<RioGeheimtor>() == null) w.gameObject.AddComponent<RioGeheimtor>();
            if (RioKanal.Finde(w, "Kanal_Bootslift") != null && w.GetComponent<RioBootslift>() == null) w.gameObject.AddComponent<RioBootslift>();
        }
        Meldung(fertig.Count + " Kanal-Teil(e) eingerichtet.");
    }

    static Transform Wurzel(Transform t)
    {
        var p = PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject);
        return p != null ? p.transform : RioKanal.TeilVon(t);
    }

    static void Meldung(string s) { EditorUtility.DisplayDialog("Rio Heists – Kanal", s, "OK"); }
}
