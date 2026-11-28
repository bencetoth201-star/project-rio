using System.Collections;
using UnityEngine;

/// <summary>
/// Bootslift im Boot-Versteck: fährt die Plattform "Kanal_Bootslift" vom Kanal (Anker "Bootslift_Unten") durch den Liftschacht
/// bis in die Versteck-Garage (Anker "Bootslift_Ziel") und zurück. Vorher gehen die Bodenklappen der Garage auf.
/// Felder leer lassen = automatisch per Name suchen (Bootsversteck und Garage müssen über die Liftschächte verbunden sein).
/// Boot mitnehmen: lift.Boot = meinBoot.transform; (wird während der Fahrt an die Plattform gehängt, Rigidbody kinematisch).
/// </summary>
public class RioBootslift : MonoBehaviour
{
    public Transform Plattform;      // Kanal_Bootslift
    public Transform Unten;          // Bootslift_Unten (im Bootsversteck)
    public Transform Ziel;           // Bootslift_Ziel (in der Versteck-Garage)
    public Transform KlappeL, KlappeR, Tor;   // Garage_Bodenklappe_L/R, Garage_Tor
    public float Geschwindigkeit = 1.5f;      // m/s
    public float KlappenWinkel = 95f;         // falls die Klappen nach unten aufgehen: Vorzeichen umdrehen
    public Transform Boot;

    public bool Oben { get; private set; }
    public bool Faehrt { get; private set; }
    Quaternion klappeL0, klappeR0;

    void Start()
    {
        Transform r = transform;
        if (Plattform == null) Plattform = RioKanal.Finde(r, "Kanal_Bootslift");
        if (Unten == null) Unten = RioKanal.Finde(r, "Bootslift_Unten");
        Vector3 p = Plattform != null ? Plattform.position : r.position;
        if (Ziel == null) Ziel = RioKanal.Naechster("Bootslift_Ziel", p);
        if (KlappeL == null) KlappeL = RioKanal.Naechster("Garage_Bodenklappe_L", p);
        if (KlappeR == null) KlappeR = RioKanal.Naechster("Garage_Bodenklappe_R", p);
        if (Tor == null) Tor = RioKanal.Naechster("Garage_Tor", p);
        if (KlappeL != null) klappeL0 = KlappeL.localRotation;
        if (KlappeR != null) klappeR0 = KlappeR.localRotation;
    }

    public void Hoch() { if (!Faehrt && !Oben) StartCoroutine(Fahren(true)); }
    public void Runter() { if (!Faehrt && Oben) StartCoroutine(Fahren(false)); }

    IEnumerator Fahren(bool hoch)
    {
        if (Plattform == null || Unten == null || Ziel == null) yield break;
        Faehrt = true;
        Rigidbody rb = Boot != null ? Boot.GetComponent<Rigidbody>() : null;
        if (Boot != null) { Boot.SetParent(Plattform, true); if (rb != null) rb.isKinematic = true; }
        if (hoch) yield return Klappen(true);
        float y0 = Plattform.position.y;
        float y1 = hoch ? Ziel.position.y : Unten.position.y;
        Vector3 xz = Plattform.position;
        while (Mathf.Abs(Plattform.position.y - y1) > 0.001f)
        {
            float y = Mathf.MoveTowards(Plattform.position.y, y1, Geschwindigkeit * Time.deltaTime);
            Plattform.position = new Vector3(xz.x, y, xz.z);
            yield return null;
        }
        if (!hoch) yield return Klappen(false);
        if (Boot != null) { Boot.SetParent(null, true); if (rb != null) rb.isKinematic = hoch; }
        Oben = hoch;
        Faehrt = false;
    }

    IEnumerator Klappen(bool auf)
    {
        for (float t = 0f; t <= 1f; t += Time.deltaTime / 1.5f)
        {
            float w = Mathf.SmoothStep(0f, KlappenWinkel, auf ? t : 1f - t);
            if (KlappeL != null) KlappeL.localRotation = klappeL0 * Quaternion.Euler(0f, 0f, -w);
            if (KlappeR != null) KlappeR.localRotation = klappeR0 * Quaternion.Euler(0f, 0f, w);
            yield return null;
        }
    }
}
