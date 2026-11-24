# Übergabe — Stand und nächste Schritte

Für die nächste Sitzung. Projekt: **PROJECT RIO**, Open-World-Krimi-Sandbox
für Mobile, Rio de Janeiro, Unity 6000.6.10f1, URP, Android, Solo zuerst.

Das Spielkonzept liegt als `GTA Mobile Rio - Spielkonzept (kombiniert).md`
auf dem Desktop im Ordner `GTA6 MOBILE`.

---

## In einem Satz

Die Welt entsteht komplett prozedural aus Daten; nichts davon liegt im Repo.
Der aktuelle Arbeitsschwerpunkt ist der **2D-Stadtplan** — erst wenn der im
Grundriss stimmt, wird wieder in 3D gebaut.

## Wie man anfängt

```powershell
cd "C:\dev\PROJECT RIO"
```

Unity öffnen, dann im Menü:

1. **Tools → Rio → Plan exportieren (2D)** — rechnet Gelände und Stadtplan,
   schreibt `Screenshots/rio_plan.txt`. Dauert 7 Sekunden, baut nichts in 3D.
2. `python -I Tools/stadtplan_zeichnen.py` — macht daraus
   `Screenshots/RIO_STADTPLAN.png`.
3. **Tools → Rio → Welt bauen (20 km2)** — baut die 3D-Welt. Dauert ~15 s.
4. **Tools → Rio → Block-Figur bauen** — setzt die Spielfigur.

Beide Generatoren brechen ab, wenn der Play Mode läuft.

**Wichtige Falle:** Unity liest Dateien, die von außen geschrieben werden,
bei unfokussiertem Editor **nicht** von selbst neu ein. Vor dem
Neukompilieren braucht es

```
unity command script eval --code 'UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate); return "ok";'
```

sonst läuft stillschweigend der alte Stand weiter. Das hat schon einmal eine
halbe Stunde gekostet.

---

## Aufbau

| Datei | Rolle |
|---|---|
| `RioWorldLayout.cs` | Die Karte als Daten — Küstenlinien, Berge, See, Strand, Orte. Alle Koordinaten in **Pixeln der Whiteboard-Skizze**, `MetersPerPixel = 8`. Wer etwas verschiebt, vergleicht mit der Skizze. |
| `RioDistricts.cs` | 17 benannte Viertel als Ellipsen mit eigener Ausrichtung, plus die Liste, welche Viertel verbunden sind. |
| `RioWorldTerrain.cs` | Höhenfeld, 25-m-Raster. Küstenrampe, Berge, Seebecken, Strandbogen, Glättung des Flachlands. |
| `RioModelCatalog.cs` | **Automatisch erzeugt** aus `haeuser.json` und `strassen.json`. 28 Gebäude + 33 Straßenkacheln mit Lot, Höhe und Anschlussmaske. |
| `RioHousePlanner.cs` | Der eigentliche Stadtplaner: Belegungsraster, Avenidas, Viertel, Blöcke, Favelas. |
| `RioHouseBaker.cs` | Macht aus den Platzierungen die Streamer-Daten und die Fern-Klötze. |
| `RioWorldBuilder.cs` | Baut Gelände, Wasser, Brücke, Wahrzeichen und ruft den Planer. |
| `RioPlanExporter.cs` | Schreibt den Plan als Text, ohne 3D. |
| `RioHouseStreamer.cs` | Lädt echte Modelle um den Spieler, Klötze in der Ferne. |

### Zwei Dinge, die man wissen muss

**glTFast spiegelt beim Import die X-Achse.** Ein Anschluss, der in der
glTF-Datei nach Osten zeigt, liegt in Unity im Westen. Die Masken im Katalog
stehen bereits in Unity-Richtungen — nicht nochmal umrechnen.

**Das Belegungsraster prüft gedrehte Rechtecke exakt.** Es läuft über die
2-m-Zellen, deren Mittelpunkt wirklich im Rechteck liegt. Die achsparallele
Hülle zu nehmen war ein Fehler: bei 26 Grad Drehung ist die Hülle einer
12-m-Kachel 16 m breit, dadurch gingen zwei Drittel aller Häuser verloren.

---

## Stand des Plans

15.012 Straßenkacheln, 922 Avenida-Abschnitte, 18.094 Bauten, 1.089 Blöcke,
7.641 Favela-Hütten. Landfläche rund 22 km² auf 8,2 × 5,6 km.

Jedes Viertel hat ein eigenes **gedrehtes** Raster, eine Ringstraße außen,
und eine eigene Modellpalette. Die Avenidas verbinden die Viertel und haben
Vorrang vor dem Stadtraster.

### Was als Nächstes dran ist, in dieser Reihenfolge

1. **Gelände unter der Stadt terrassieren.** Die Straßenkacheln sind flache
   Platten, gesetzt auf die Geländehöhe in ihrer Mitte. Auf jeder Steigung
   schwebt eine Kachel auf der einen Seite und versinkt auf der anderen —
   das ist die Ursache für schwebende Objekte und durchstoßendes Gras.
   Richtig wäre: je Block eine konstante Höhe, das Gelände wird darauf
   gezwungen, Übergänge über Stützmauern und Böschungen (Modelle 28–33).
2. **Noch zu viel leeres Land** zwischen den Vierteln. Entweder Viertel
   weiter vergrößern oder bewusst Grünflächen und Nebenstraßen setzen.
3. **Zwei Orla-Viertel liegen über Strand** und bekommen keine Straßen.
4. **Avenidas durch die Viertel führen**, nicht nur bis zum Mittelpunkt.
5. **Favela-Wege**: Becos, Escadarias und Hangstraßen gibt es als Modelle,
   werden aber noch nicht benutzt. Hütten stehen ohne Erschließung da.
6. **Darstellung**: 13.000 Häuser ergeben über 200.000 Renderer, weil jedes
   Modell ~16 Materialslots hat. Daran erstickt die Bildrate, nicht an den
   Dreiecken. Der Fix ist GPU-Instancing statt einzelner Objekte. Platzierung
   und Darstellung sind dafür schon getrennt.

---

## Was noch fehlt an Modellen

Nicht blockierend, aber es würde helfen:

- **Schnellstraße mit Leitplanke** plus Auf- und Abfahrt. Die Verbindungen
  zwischen den Vierteln sind in Rio Expressways, keine Alleen.
- **Brückensegment** — die Brücke nach Niterói ist noch prozedural gebaut
  und passt stilistisch nicht zu den Kacheln.
- **Kaikante** für den Hafen.
- **Beco-Kurve und -Kreuzung** für die Favela-Erschließung.

## Was da ist

- 28 Gebäude: Barraco bis Mansão Joatinga, dazu Torre Centro (139 m),
  Edifício 70er (118 m), Igreja, Lagerhalle, drei Eckgebäude, Praça, Parque.
  Bank und Tankstelle haben Ankerpunkte (Tresor, Kasse, Zapfsäule), die zur
  Heist-Leiter aus dem Konzept passen.
- 33 Straßenkacheln: Rua und Avenida in allen Varianten, Bögen mit 15 und 30
  Grad, Kreisverkehr, Copacabana-Promenade, Beco, Escadaria, Hangstraße,
  Viadukt, Tunnel, Stützmauern und Böschung.
- Farbsystem: Materialien `slot_*` werden pro Haus nach einem Seed umgefärbt.
  Die Favela-Reihe allein hat 2,1 Millionen Kombinationen.

## Nicht im Repo

`Assets/_Project/Generated/` — erzeugte Meshes und Hausdaten, rund 47 MB.
Entstehen bei festem Seed in Sekunden neu. Nach einem Clone einmal beide
Generatoren laufen lassen.
