using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Färbt alle Materialien "slot_&lt;art&gt;_&lt;n&gt;" eines Rio-Hauses mit Farben aus der passenden Palette.
/// Gleicher Seed = gleiche Farben. Im Editor: Vorschau per MaterialPropertyBlock (ändert keine Assets).
/// Im Spiel: geteilte Material-Varianten (gut für den SRP-Batcher und für Mobile).
/// </summary>
[ExecuteAlways, DisallowMultipleComponent]
public class RioHausFarben : MonoBehaviour
{
    [Tooltip("Gleicher Seed = gleiche Farben. Anderer Seed = andere Farbvariante.")]
    public int seed = 1;

    [Tooltip("Aus = Originalfarben des Modells.")]
    public bool zufallsFarben = true;

    static readonly int[] FarbIds =
    {
        Shader.PropertyToID("_BaseColor"),      // URP / HDRP Lit
        Shader.PropertyToID("_Color"),          // Built-in Standard
        Shader.PropertyToID("baseColorFactor"), // glTFast-Shader
    };

    // Geteilte Varianten: (Originalmaterial, Farbe) -> Material. Gleiche Farbe = gleiches Material.
    static readonly Dictionary<(Material, Color32), Material> Varianten = new Dictionary<(Material, Color32), Material>();

    Renderer[] renderers;
    Material[][] originale;

    void OnEnable()
    {
        Anwenden();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Anwenden(); };
    }
#endif

    /// <summary>Neuen Seed ziehen und Farben anwenden.</summary>
    public void NeuWuerfeln()
    {
        seed = Random.Range(1, int.MaxValue);
        Anwenden();
    }

    public void Anwenden()
    {
        if (renderers == null || originale == null)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originale = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originale[i] = renderers[i].sharedMaterials;
        }

        var farben = FarbenWaehlen();
        if (Application.isPlaying) Laufzeit(farben);
        else Vorschau(farben);
    }

    Dictionary<string, Color> FarbenWaehlen()
    {
        // Slot-Namen sortiert einsammeln -> gleicher Seed gibt immer dieselben Farben
        var slots = new SortedSet<string>();
        foreach (var mats in originale)
            foreach (var m in mats)
            {
                string s = SlotName(m);
                if (s != null) slots.Add(s);
            }

        var rng = new System.Random(seed);
        var farben = new Dictionary<string, Color>();
        foreach (var s in slots)
        {
            string[] teile = s.Split('_');
            if (teile.Length < 2) continue;
            if (!RioHausDaten.Paletten.TryGetValue(teile[1], out string[] palette)) continue;
            if (ColorUtility.TryParseHtmlString(palette[rng.Next(palette.Length)], out Color c))
                farben[s] = c;
        }
        return farben;
    }

    void Vorschau(Dictionary<string, Color> farben)
    {
        var mpb = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = originale[i];
            for (int j = 0; j < mats.Length; j++)
            {
                mpb.Clear();
                string s = SlotName(mats[j]);
                if (s != null && zufallsFarben && farben.TryGetValue(s, out Color c))
                    foreach (int id in FarbIds) mpb.SetColor(id, c);
                r.SetPropertyBlock(mpb, j);
            }
        }
    }

    void Laufzeit(Dictionary<string, Color> farben)
    {
        var leer = new MaterialPropertyBlock();
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            var mats = (Material[])originale[i].Clone();
            for (int j = 0; j < mats.Length; j++)
            {
                r.SetPropertyBlock(leer, j);
                string s = SlotName(mats[j]);
                if (s != null && zufallsFarben && farben.TryGetValue(s, out Color c))
                    mats[j] = Variante(mats[j], c);
            }
            r.sharedMaterials = mats;
        }
    }

    static Material Variante(Material basis, Color c)
    {
        var key = (basis, (Color32)c);
        if (Varianten.TryGetValue(key, out Material m) && m != null) return m;
        m = new Material(basis) { name = basis.name + " #" + ColorUtility.ToHtmlStringRGB(c) };
        foreach (int id in FarbIds)
            if (m.HasProperty(id)) m.SetColor(id, c);
        Varianten[key] = m;
        return m;
    }

    static string SlotName(Material m)
    {
        if (m == null) return null;
        string n = m.name;
        int k = n.IndexOf("slot_", System.StringComparison.Ordinal);
        if (k < 0) return null;
        n = n.Substring(k);
        int ende = n.IndexOfAny(new[] { ' ', '(', '.' });
        return ende > 0 ? n.Substring(0, ende) : n;
    }
}
