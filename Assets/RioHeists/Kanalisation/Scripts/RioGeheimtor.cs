using System.Collections;
using UnityEngine;

/// <summary>
/// Geheimtor des Boot-Verstecks: von der Kanalseite sieht es aus wie eine Sackgasse mit Glyphe. Der lose Jade-Stein
/// "Kanal_Geheimtor_Schalter" in der Ostwand (Stehpunkt: Anker "Geheimtor_Schalter") wird eingedrückt → die Steinplatte
/// "Kanal_Geheimtor" fährt 3,95 m nach oben in den Sturz. Auf das Bootsversteck-Objekt legen (Felder füllen sich selbst).
/// Spieler-Skript: if (E gedrückt && Abstand zum Anker < 1,5 m) tor.Umschalten();
/// </summary>
public class RioGeheimtor : MonoBehaviour
{
    public Transform Tor, Schalter, SchalterAnker;
    public float Hub = 3.95f, Dauer = 3f;
    public bool Offen { get; private set; }
    Vector3 tor0, schalter0;
    bool faehrt;

    void Start()
    {
        if (Tor == null) Tor = RioKanal.Finde(transform, "Kanal_Geheimtor");
        if (Schalter == null) Schalter = RioKanal.Finde(transform, "Kanal_Geheimtor_Schalter");
        if (SchalterAnker == null) SchalterAnker = RioKanal.Finde(transform, "Geheimtor_Schalter");
        if (Tor != null) tor0 = Tor.localPosition;
        if (Schalter != null) schalter0 = Schalter.localPosition;
    }

    public void Umschalten() { if (!faehrt) StartCoroutine(Fahren(!Offen)); }

    IEnumerator Fahren(bool auf)
    {
        if (Tor == null) yield break;
        faehrt = true;
        if (Schalter != null && SchalterAnker != null) Schalter.position += SchalterAnker.forward * 0.05f;   // Stein in die Wand drücken
        for (float t = 0f; t <= 1f; t += Time.deltaTime / Dauer)
        {
            float k = Mathf.SmoothStep(0f, 1f, auf ? t : 1f - t);
            Tor.localPosition = tor0 + Vector3.up * Hub * k;
            yield return null;
        }
        Tor.localPosition = tor0 + Vector3.up * (auf ? Hub : 0f);
        if (Schalter != null) Schalter.localPosition = schalter0;
        Offen = auf;
        faehrt = false;
    }
}
