using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hilfen für die Heist-Modelle (Casa Império normal + abgeriegelt, Megayacht SOBERANA, Maya-Kanalisation).
/// RioHeist.Einrichten(modell) hängt an jedes stehlbare Teil eine RioBeute-Komponente (Art + Wert aus dem Node-Namen).
/// Vorher RioKollision.Einrichten(modell) ausführen (macht die Trigger-Collider), der Heist-Platzierer macht beides.
/// </summary>
public static class RioHeist
{
    /// <summary>Werte in R$ (fiktiv) pro Art = Wort nach „_Beute_“ im Node-Namen. Frei anpassen.</summary>
    public static readonly Dictionary<string, int> Werte = new Dictionary<string, int>
    {
        { "Safira", 25000000 }, { "Edelstein", 15000000 }, { "Krone", 8000000 }, { "Gold", 2500000 }, { "Jademaske", 1500000 },
        { "Kunst", 1200000 }, { "Bild", 800000 }, { "Bargeld", 500000 },
        { "Skulptur", 400000 }, { "Schmuck", 300000 }, { "Uhr", 120000 }, { "Tasche", 40000 }, { "Wein", 30000 },
        { "Kerzenleuchter", 20000 }, { "Laptop", 15000 }, { "Champagner", 8000 }, { "Zigarren", 6000 }, { "Handy", 5000 },
    };

    static readonly HashSet<string> GrosseArten = new HashSet<string> { "Gold", "Kunst", "Bild", "Skulptur", "Bombe" };

    /// <summary>Gibt allen Beute-, Schlüssel- und Ausrüstungs-Nodes unter root eine RioBeute-Komponente. Liefert die Liste.</summary>
    public static List<RioBeute> Einrichten(GameObject root)
    {
        var liste = new List<RioBeute>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.Contains("__")) continue;                       // Grafik- und Hitbox-Nodes
            string art = ArtVon(n);
            if (art == null) continue;
            var b = t.GetComponent<RioBeute>();
            if (b == null) b = t.gameObject.AddComponent<RioBeute>();
            b.Art = art;
            b.Wert = Werte.TryGetValue(art, out int w) ? w : 0;
            b.Gross = GrosseArten.Contains(art);
            b.Pool = n.Contains("_Pool");
            b.Ausruestung = n.Contains("_Ausruestung_");
            liste.Add(b);
        }
        return liste;
    }

    /// <summary>„Hauptdeck_Beute_Uhr_Pool1“ → „Uhr“, „Garage_Ausruestung_Bombe_1“ → „Bombe“,
    /// „Brueckendeck_Tresor_Schluessel“ → „Schluessel“, sonst null.</summary>
    public static string ArtVon(string nodeName)
    {
        foreach (string tag in new[] { "_Beute_", "_Ausruestung_" })
        {
            int i = nodeName.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0) continue;
            string rest = nodeName.Substring(i + tag.Length);
            int j = rest.IndexOf('_');
            return j < 0 ? rest : rest.Substring(0, j);
        }
        if (nodeName.Contains("Schluessel")) return "Schluessel";
        if (nodeName.Contains("Tauchausruestung")) return "Tauchausruestung";
        return null;
    }

    /// <summary>Kind-Transform per Name suchen (Anker, bewegliche Teile), egal wie tief.</summary>
    public static Transform Finde(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    /// <summary>Alle Anker, deren Name mit praefix beginnt (z. B. „Wache_“, „Patrouille_Hauptdeck_A_“, „Polizei_Boot_“), nach Namen sortiert.</summary>
    public static List<Transform> Anker(Transform root, string praefix)
    {
        var liste = new List<Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith(praefix, StringComparison.Ordinal) && t.childCount == 0) liste.Add(t);
        liste.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return liste;
    }
}
