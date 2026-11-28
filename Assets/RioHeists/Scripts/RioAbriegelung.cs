using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abriegelung der Casa Império („wie in der Heist-Serie“): tauscht das normale Modell (01_casa_imperio) gegen die
/// abgeriegelte Version (03_casa_imperio_abgeriegelt) und fährt Rollläden, Panzertüren, Gitter und Barrikaden ein.
/// Beide Modelle haben denselben Pivot → gleiche Position/Rotation/Skalierung. Das Skript sitzt auf einem eigenen,
/// immer aktiven GameObject (Tools ▸ Rio Heists ▸ Abriegelung vorbereiten legt alles an).
/// Ablauf im Spiel: P2 legt im Technikraum den Hebel um (Strombox) und drückt den roten Knopf (Sicherheitsbox) →
///   abriegelung.Abriegeln();   Strom ist in der abgeriegelten Version aus (Lampen aus, nur rotes Notlicht, Laser aus).
/// </summary>
public class RioAbriegelung : MonoBehaviour
{
    public GameObject Normal;           // Instanz von 01_casa_imperio
    public GameObject Abgeriegelt;      // Instanz von 03_casa_imperio_abgeriegelt (gleiche Transform-Werte, anfangs inaktiv)
    public float Dauer = 4f;            // Sekunden, bis alle Rollläden unten sind
    public float PanzertuerHub = 6.5f;  // Panzertüren vor den Portalen fallen aus dieser Höhe (m)
    public float GitterHub = 3.2f;      // Gitter vor den Sicherheitsschleusen fallen aus dieser Höhe (m)

    public bool IstAbgeriegelt { get; private set; }

    /// <summary>true = abgeriegelt, false = freigegeben (z. B. Polizei-KI umschalten, Musik ändern).</summary>
    public static event Action<RioAbriegelung, bool> Geaendert;

    readonly List<Transform> rolllaeden = new List<Transform>();   // Abriegelung_<Ebene>_<Vorne|Hinten|Links|Rechts> (Pivot oben → Skalierung Y)
    readonly List<Transform> fallen = new List<Transform>();       // Abriegelung_EG_Portale, Abriegelung_EG_Schleusen (fallen von oben)
    readonly List<Transform> einblenden = new List<Transform>();   // Lkw-Tor-Träger, Hintertür, Dach (wachsen aus der Wand)
    readonly List<GameObject> sofort = new List<GameObject>();     // Notlicht, Alarm-Leuchten, Barrikaden, Panzersperren (an/aus)
    readonly Dictionary<Transform, Vector3> startPos = new Dictionary<Transform, Vector3>();
    readonly Dictionary<Transform, Vector3> startSkala = new Dictionary<Transform, Vector3>();
    Coroutine laeuft;

    void Awake()
    {
        Sammeln();
        if (Abgeriegelt != null) Abgeriegelt.SetActive(false);
        if (Normal != null) Normal.SetActive(true);
    }

    void Sammeln()
    {
        rolllaeden.Clear(); fallen.Clear(); einblenden.Clear(); sofort.Clear();
        if (Abgeriegelt == null) return;
        foreach (Transform t in Abgeriegelt.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (!n.StartsWith("Abriegelung_", StringComparison.Ordinal)) continue;
            if (n.Contains("__"))
            {
                // statische Teile ohne eigenen Pivot: Notlicht, Leuchten, Barrikaden, Panzersperren (Gehäuse bleiben immer sichtbar)
                if (t.parent != null && t.parent.name.StartsWith("Abriegelung_", StringComparison.Ordinal)) continue;   // gehört zu einem Pivot
                if (n.Contains("_Notlicht__") || n.Contains("_Leuchten__") || n.Contains("_Barrikaden__") || n.StartsWith("Abriegelung_Aussen__", StringComparison.Ordinal))
                    sofort.Add(t.gameObject);
                continue;
            }
            startPos[t] = t.localPosition;
            startSkala[t] = t.localScale;
            if (n.EndsWith("_Vorne") || n.EndsWith("_Hinten") || n.EndsWith("_Links") || n.EndsWith("_Rechts")) rolllaeden.Add(t);
            else if (n.EndsWith("_Portale") || n.EndsWith("_Schleusen")) fallen.Add(t);
            else einblenden.Add(t);
        }
    }

    public void Abriegeln()
    {
        if (IstAbgeriegelt) return;
        IstAbgeriegelt = true;
        if (Normal != null && Abgeriegelt != null) Uebernehmen(Normal.transform, Abgeriegelt.transform);
        if (Abgeriegelt != null) Abgeriegelt.SetActive(true);
        if (Normal != null) Normal.SetActive(false);
        if (laeuft != null) StopCoroutine(laeuft);
        laeuft = StartCoroutine(Fahren(true));
        Geaendert?.Invoke(this, true);
    }

    public void Freigeben()
    {
        if (!IstAbgeriegelt) return;
        IstAbgeriegelt = false;
        if (laeuft != null) StopCoroutine(laeuft);
        laeuft = StartCoroutine(Fahren(false));
        Geaendert?.Invoke(this, false);
    }

    IEnumerator Fahren(bool zu)
    {
        foreach (var g in sofort) g.SetActive(zu);
        for (float t = 0f; t <= 1f; t += Time.deltaTime / Mathf.Max(Dauer, 0.01f))
        {
            Setzen(zu ? t : 1f - t);
            yield return null;
        }
        Setzen(zu ? 1f : 0f);
        if (!zu && Normal != null && Abgeriegelt != null)
        {
            Uebernehmen(Abgeriegelt.transform, Normal.transform);
            Normal.SetActive(true);
            Abgeriegelt.SetActive(false);
        }
        laeuft = null;
    }

    /// <summary>0 = offen (normal), 1 = komplett abgeriegelt.</summary>
    void Setzen(float k)
    {
        float s = Mathf.SmoothStep(0f, 1f, k);
        foreach (var t in rolllaeden)
        {
            Vector3 sk = startSkala[t];
            t.localScale = new Vector3(sk.x, Mathf.Max(sk.y * s, 0.001f), sk.z);
        }
        foreach (var t in fallen)
        {
            float hub = t.name.EndsWith("_Portale") ? PanzertuerHub : GitterHub;
            t.localPosition = startPos[t] + Vector3.up * hub * (1f - s);
            t.gameObject.SetActive(k > 0.001f);
        }
        foreach (var t in einblenden)
        {
            t.localScale = startSkala[t] * Mathf.Max(s, 0.001f);
            t.gameObject.SetActive(k > 0.001f);
        }
    }

    /// <summary>Zustand beweglicher Teile (Türen, Tore, Aufzüge, gestohlene Beute) von einem Modell aufs andere kopieren,
    /// damit der Tausch nicht auffällt. Gleiche Node-Namen in beiden Modellen.</summary>
    static void Uebernehmen(Transform von, Transform nach)
    {
        var ziel = new Dictionary<string, Transform>();
        foreach (Transform t in nach.GetComponentsInChildren<Transform>(true))
            if (!t.name.Contains("__") && !ziel.ContainsKey(t.name)) ziel[t.name] = t;
        foreach (Transform t in von.GetComponentsInChildren<Transform>(true))
        {
            if (t == von || t.name.Contains("__") || t.childCount == 0) continue;            // nur bewegliche Teile (Nodes mit Kindern)
            if (t.name.StartsWith("Abriegelung_", StringComparison.Ordinal)) continue;
            if (!ziel.TryGetValue(t.name, out Transform z)) continue;
            z.localPosition = t.localPosition;
            z.localRotation = t.localRotation;
            z.localScale = t.localScale;
            z.gameObject.SetActive(t.gameObject.activeSelf);                                // gestohlene Beute bleibt weg
        }
    }
}
