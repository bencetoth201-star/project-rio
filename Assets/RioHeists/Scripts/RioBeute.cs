using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stehlbares/aufnehmbares Teil eines Heist-Modells (Node mit „_Beute“ oder „_Ausruestung“ im Namen, außerdem Schlüssel und Tauchausrüstung).
/// Wird von RioHeist.Einrichten() angehängt. Der unsichtbare Kind-Node „…__trigger_1“ ist der Greifbereich (isTrigger).
/// Spieler-Objekt braucht das Tag „Player“ und einen CharacterController oder Rigidbody.
/// Benutzung im Spieler-Skript:  if (Input.GetKeyDown(KeyCode.E) && RioBeute.Naechste(transform.position) is RioBeute b) b.Stehlen();
/// </summary>
public class RioBeute : MonoBehaviour
{
    public string Art;          // Wort nach „_Beute_“ im Namen: Gold, Bargeld, Bild, Uhr … (Schluessel / Tauchausruestung bei Spezialteilen)
    public int Wert;            // R$ (fiktiv), siehe RioHeist.Werte
    public bool Gross;          // zwei Hände / langsam tragen (Gold, Bilder, Kunst, Skulpturen)
    public bool Pool;           // gehört zur Nebenmission „Pool-Gäste“ (Yacht)
    public bool Ausruestung;    // Heist-Ausrüstung aus der Versteck-Garage (Bauplan, Nachtsicht, Bombe): Wert 0, ins Inventar statt Geld

    /// <summary>Alle Beute-Teile, in deren Greifbereich der Spieler gerade steht.</summary>
    public static readonly List<RioBeute> InReichweite = new List<RioBeute>();

    /// <summary>Wird beim Stehlen ausgelöst (z. B. Geld gutschreiben, Polizei rufen beim Schlüssel).</summary>
    public static event Action<RioBeute> Gestohlen;

    public void Stehlen()
    {
        if (!gameObject.activeSelf) return;
        InReichweite.Remove(this);
        Gestohlen?.Invoke(this);
        gameObject.SetActive(false);
    }

    /// <summary>Nächstes Teil im Greifbereich (oder null).</summary>
    public static RioBeute Naechste(Vector3 von)
    {
        RioBeute beste = null;
        float d = float.MaxValue;
        foreach (var b in InReichweite)
        {
            if (b == null || !b.gameObject.activeInHierarchy) continue;
            float e = (b.transform.position - von).sqrMagnitude;
            if (e < d) { d = e; beste = b; }
        }
        return beste;
    }

    void OnTriggerEnter(Collider c)
    {
        if (c.CompareTag("Player") && !InReichweite.Contains(this)) InReichweite.Add(this);
    }

    void OnTriggerExit(Collider c)
    {
        if (c.CompareTag("Player")) InReichweite.Remove(this);
    }

    void OnDisable()
    {
        InReichweite.Remove(this);
    }
}
