# Anleitung für Claude Code: GTA Mobile – Rio-Assets (Gesamtpaket)

Dieses Paket fasst alle Rio-Pakete zusammen. Lies zuerst dieses Dokument, dann die Anleitung des Pakets, mit dem du arbeitest.

## Aufbau
```
Assets/RioGemeinsam/   RioKollision.cs (Laufzeit) + Editor/RioKollisionEditor.cs (Menü Tools ▸ Rio ▸ Hitboxen einrichten)
Assets/RioHaeuser/     33 Gebäude + Platzierer (Tools ▸ Rio Häuser ▸ Platzhalter ersetzen), RioHausFarben, haeuser.json
Assets/RioStrassen/    Straßennetz-Baukasten + strassen.json
Assets/RioStrand/      Strandobjekte + strand.json
Assets/RioHafen/       Hafen-Baukasten, Containerschiff + hafen.json
Assets/RioFahrzeuge/   Fahrzeuge + fahrzeuge.json
Docs/                  Anleitungen pro Paket (Koordinaten, Anker, bewegliche Teile, Heist-Details)
```
| Paket | Modelle | Anleitung |
|---|---|---|
| Häuser & Spezialgebäude | 33 | `Docs/ANLEITUNG_HAEUSER.md` |
| Straßennetz | 56 | `Docs/ANLEITUNG_STRASSEN.md` |
| Strand | 20 | `Docs/ANLEITUNG_STRAND.md` |
| Hafen | 17 | `Docs/ANLEITUNG_HAFEN.md` |
| Fahrzeuge | 1 | `Docs/ANLEITUNG_FAHRZEUGE.md` |

## Heists (eigenes Paket)
Casa Império („Megabank“) und die Megayacht SOBERANA sind ein **separates Paket** (`RioHeists_fuer_ClaudeCode.zip` → `Assets/RioHeists`,
Anleitung `ANLEITUNG_CLAUDE_CODE_HEISTS.md` mit Ablauf, Beute, Wachen, Fluchtwegen). Es nutzt dieselben Skripte in `Assets/RioGemeinsam`.
Der Häuser-Platzierer überspringt Platzhalter mit `_palast`, `_imperio`, `_megabank`, `_heist` oder `_yacht` – die setzt
**Tools ▸ Rio Heists ▸ Platzhalter ersetzen** aus dem Heist-Paket.

## Gemeinsame Regeln
- 1 Einheit = 1 m, Y oben. glTFast spiegelt X beim Import → Ankerpositionen immer aus den Child-Transforms lesen.
- Node-Namen: `<Ebene>__<Material>` = Grafik, `<Ebene>_Innen__…` = Einrichtung, `<Ebene>__kollision` / `__trigger_<n>` = Hitboxen,
  Nodes ohne `__` mit Kindern = bewegliche Teile (Pivot am Scharnier), leere Nodes = Anker.
- Farben würfeln: `RioHausFarben` auf das Modell (Slots `slot_*`).
- Streaming: Außenhülle immer an, `*_Innen`-Nodes erst aktivieren, wenn der Spieler im Gebäude/an Bord ist.

## Hitboxen (gilt für alle Modelle)
Jedes GLB bringt unsichtbare Zusatz-Nodes mit:
- **`<Ebene>__kollision`** – vereinfachte feste Hitbox pro Ebene: Wände, Böden, Treppen, Geländer-Handläufe, Möbel und Fahrzeuge
  als einfache Blöcke. **Nicht fest:** Kleinkram unter 30 cm (Ringe, Edelsteine, Flaschen, Schrift), flache Teppiche und
  Markierungen, dünne Stäbe (Geländerstäbe, Gitterstäbe, Fensterrahmen, Stuhlbeine), Lichtkegel, Wasserfontänen, Palmwedel.
- **`<Ebene>__trigger_<n>`** – Auslöser ohne feste Wand: Laserstrahlen, Pools, Flutwasser (convex, `isTrigger`).
- **Bewegliche Teile** (Türen, Tore, Vitrinen-Hauben, Laser, Aufzüge, Tresortüren, Boote, Hubschrauber …) sind eigene Nodes
  mit Pivot am Scharnier/Drehpunkt; ihre `…__kollision`/`…__trigger_<n>`-Kinder bewegen sich mit.

**In Unity einrichten:** Menü **Tools ▸ Rio ▸ Hitboxen einrichten (Auswahl | ganze Szene)** (Skripte in `Assets/RioGemeinsam`).
Das macht die Zusatz-Nodes unsichtbar, setzt `MeshCollider` (Trigger: convex + isTrigger), entfernt alte Voll-MeshCollider,
gibt beweglichen Teilen einen kinematischen `Rigidbody` und markiert alles andere als statisch (bewegliche Teile nie – sonst
bewegt sich die Grafik nicht mit). Der Häuser-Platzierer (`Tools ▸ Rio Häuser ▸ Platzhalter ersetzen`) erledigt das automatisch.
- **Wichtig:** keine eigenen MeshCollider auf die Grafik-Meshes legen (teuer und falsch bei Lasern/Lichtkegeln).
- **Zur Laufzeit gespawnt:** `RioKollision.Einrichten(instanz);` aufrufen. Dafür im glTFast-Importer „Read/Write“ der Meshes
  aktivieren (sonst kann Unity die Collider zur Laufzeit nicht bauen). Im Editor platzierte Modelle brauchen das nicht.
- **Heist-Logik:** Laser aus = Laser-Node deaktivieren (Trigger geht mit). Alarm = `OnTriggerEnter` am Trigger
  (der Spieler braucht `CharacterController` oder `Rigidbody`). Tür auf = beweglichen Node drehen/verschieben.
  Bewegliche Teile findet man mit `RioKollision.IstBeweglich(transform)`.
- **Fahrzeuge** (BMW M4) bekommen weiter BoxCollider + WheelCollider wie in ihrer Anleitung.
- **Kosten:** Hitboxen haben zusammen ~50 % der Grafik-Dreiecke (Containerschiff ~28.000, Stadion ~23.000, Kleiderladen ~18.000,
  normale Häuser 700–4.500, Straßenteile meist < 1.500).
