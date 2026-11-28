# Übergabe — Stand und nächste Schritte

Für die nächste Sitzung. Projekt: **PROJECT RIO**, Open-World-Krimi-Sandbox
für Mobile, Rio de Janeiro, Unity 6000.6.10f1, URP, Android, Solo zuerst.

Das Spielkonzept liegt als `GTA Mobile Rio - Spielkonzept (kombiniert).md`
auf dem Desktop im Ordner `GTA6 MOBILE`.

---

## In einem Satz

Die Welt entsteht komplett prozedural aus Daten; nichts davon liegt im Repo.
Das Straßennetz wird **erst als Graph geplant** und danach in Kacheln
übersetzt — nicht umgekehrt. Das ist der Kern von allem, was hier steht.

## Wie man anfängt

```powershell
cd "C:\dev\PROJECT RIO"
```

Unity öffnen, dann im Menü:

1. **Tools → Rio → Plan exportieren (2D)** — rechnet Gelände und Stadtplan,
   schreibt `Screenshots/rio_plan.txt`. Dauert ~20 s, baut nichts in 3D.
2. `python -I Tools/stadtplan_zeichnen.py` — macht daraus
   `Screenshots/RIO_STADTPLAN.png`.
3. **Tools → Rio → Block-Figur bauen** — legt die Spielfigur an.
4. **Tools → Rio → Welt bauen (20 km2)** — baut die 3D-Welt und setzt die
   Figur nach Centro. Dauert ~20 s. Erst die Figur, dann die Welt -
   andersherum bleibt die Figur auf ihrem festen Startpunkt ausserhalb der
   Stadt.

Beide Generatoren brechen ab, wenn der Play Mode läuft.

**Wichtige Falle:** Unity liest Dateien, die von außen geschrieben werden,
bei unfokussiertem Editor **nicht** von selbst neu ein. Vor dem
Neukompilieren braucht es

```
unity command script eval --code 'UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate); return "ok";'
```

sonst läuft stillschweigend der alte Stand weiter.

---

## Aufbau

| Datei | Rolle |
|---|---|
| `RioWorldLayout.cs` | Die Karte als Daten — Küstenlinien, Berge, See, Strand, Orte. Alle Koordinaten in **Pixeln der Whiteboard-Skizze**, `MetersPerPixel = 8`. |
| `RioDistricts.cs` | 17 benannte Viertel als Ellipsen mit eigener Ausrichtung, plus die Liste, welche verbunden sind. |
| `RioWorldTerrain.cs` | Höhenfeld, **12-m-Raster**. Dazu die Stempel-Schnittstelle, über die die Stadt ihre Höhen anmeldet. |
| `RioModelCatalog.cs` | **Automatisch erzeugt** von `Tools/katalog_erzeugen.py` aus `strassen.json`. 28 Gebäude + 55 Straßen-, Brücken- und Geländeteile mit Lot, Höhe, Anschlussmaske und **allen Ankern in Unity-Koordinaten**. |
| `RioStrassenplan.cs` | Der Netzplaner: Viertelraster als Graph, Avenidas, Schnellstraßen, Brücke, Höhenausgleich, Kachelauswahl. |
| `RioStrassennetz.cs` | Die **Messung**: baut aus den Ankern den Graphen der wirklich zusammenhängenden Kacheln und zählt offene Enden und Teilnetze. |
| `RioHousePlanner.cs` | Belegungsraster und Häuser. Setzt keine Straßen mehr. |
| `RioHouseBaker.cs` | Macht aus den Platzierungen die Streamer-Daten und die Fern-Klötze. |
| `RioWorldBuilder.cs` | Ruft alles in der richtigen Reihenfolge auf und baut Gelände, Wasser und Wahrzeichen. |
| `RioHouseStreamer.cs` | Lädt echte Modelle um den Spieler, Klötze in der Ferne. |

### Vier Dinge, die man wissen muss

**Die Reihenfolge ist der Kern.** Erst plant die Stadt, dann legt sie ihre
Höhen fest, dann meldet sie die Flächen beim Gelände an, und **erst dann**
wird das Geländenetz gebaut. Vorher stand das Gelände fest, bevor die erste
Kachel gesetzt war — daher die schwebenden Objekte.

**glTFast spiegelt beim Import die X-Achse.** Ein Anschluss, der in der
glTF-Datei nach Osten zeigt, liegt in Unity im Westen. Der erzeugte Katalog
steht bereits in Unity-Koordinaten — nicht nochmal umrechnen.

**Unity dreht im Uhrzeigersinn.** `RioDistricts.ToWorld` benutzte die übliche
mathematische 2D-Drehung, die andersherum dreht. Kachelraster und
Kacheldrehung liefen dadurch gegeneinander; in einem um 26 Grad gedrehten
Viertel lagen benachbarte Anschlüsse bis zu 10 m auseinander. Wer die
Funktion anfasst, muss sie und `ToLocal` gegenseitig invers halten.

**Das Belegungsraster prüft gedrehte Rechtecke exakt** und zieht sich beim
Prüfen um `Einzug` nach innen zurück, damit zwei aneinanderstoßende Kacheln
sich nicht gegenseitig für belegt halten.

---

## Wie man Qualität misst

Nicht nach Augenmaß. Jeder Lauf schreibt zwei Zeilen ins Log:

```
[Rio] Strassenplan: ... 0 Stufen ohne Kachel ...
[Rio] Strassennetz: ... offene Enden ... Teilnetze ...
```

* **Offene Enden** — Anschlüsse ohne Partner. Jeder davon ist eine Straße,
  die im Nichts aufhört.
* **Teilnetze** — Zusammenhangskomponenten. Zwischen zwei Teilnetzen kann man
  nicht fahren.
* **Stufen ohne Kachel** — Höhenwechsel ohne Steigungskachel. Jeder davon ist
  ein Bruch in der Fahrbahn.
* **Größter Überstand über Gelände** — wie weit die Fahrbahn über dem Boden
  schwebt oder darin versinkt.

Zum Vergleich der Stand, bevor daran gearbeitet wurde: 15.012 Kacheln in
**12.924 Teilnetzen** mit **31.031 offenen Enden**. Das Netz war Staub,
obwohl es auf der Karte wie ein Netz aussah.

---

## Stand

Rund 15.000 Straßenkacheln, 9.400 Häuser, 9.700 Favela-Hütten, 1.080 Blocks
auf 22 km². Gelände 642.000 Dreiecke.

| Maß | Wert |
|---|---|
| Offene Enden | **unter 50** |
| Teilnetze | **rund 20** |
| Stufen ohne Steigungskachel | **0** |
| Größter Überstand über Gelände | **0,94 m** |
| Größter Knick an einer Fuge | **1,1 Grad** |
| Brücke nach Niterói | durchgehend, 132 Teile auf echten Pfeilern |

### Was als Nächstes dran ist, in dieser Reihenfolge

1. **Schnellstraßenring.** Der einzige Teil, der nicht läuft. Von 7
   Abschnitten schließt 1. Der Ring wird deshalb komplett verworfen —
   `SchnellVerworfen` im Log — damit keine breite Fahrbahn im Feld endet.
   Die Mechanik steht (`Ring`, `RampenKandidaten`, `Abschnitt`), es fehlt der
   Suche die Trefferquote über 1–2 km. Ansätze: mehr Startrampen probieren
   statt nur der besten, Zwischenziele auf halber Strecke setzen, oder den
   Ring in kürzere Abschnitte mit mehr Auffahrten zerlegen.
2. **Die 9 nicht geschlossenen Avenida-Strecken** (von 23). Gleiche Ursache,
   gleiche Hebel.
3. **Stützmauern an den Terrassenkanten.** Die Höhen stehen auf einem
   1,5-m-Raster; wo zwei Terrassen aneinanderstoßen, gehört eine Mauer hin
   (Modelle 51, 28, 29) oder eine Böschung (32). Im Moment läuft das Gelände
   dort weich durch, was die 0,94 m Überstand erklärt.
4. **Favela-Wege**: Becos, Escadarias und Hangstraßen gibt es als Modelle,
   werden aber nicht benutzt. Die Hütten stehen ohne Erschließung da.
5. **Tunnel durch die Morros.** `20_tunnel` und `34_tunnel_mitte` liegen
   bereit, werden aber nicht gesetzt.
6. **Zu viel leeres Land** zwischen den Vierteln.
7. **Darstellung**: 33.000 Objekte mit je ~16 Materialslots ergeben über
   200.000 Renderer. Daran erstickt die Bildrate, nicht an den Dreiecken.
   Der Fix ist GPU-Instancing. Platzierung und Darstellung sind dafür schon
   getrennt. Dazu kommen 642.000 Dreiecke Gelände — für Mobile zu viel, das
   Raster müsste außerhalb der Stadt gröber werden.

---

## Modelle

**28 Gebäude**: Barraco bis Mansão Joatinga, dazu Torre Centro (139 m),
Edifício 70er (118 m), Igreja, Lagerhalle, drei Eckgebäude, Praça, Parque.
Bank und Tankstelle haben Ankerpunkte (Tresor, Kasse, Zapfsäule), die zur
Heist-Leiter aus dem Konzept passen.

**55 Straßen-, Brücken- und Geländeteile**: Rua und Avenida in allen
Varianten, Bögen mit 15 und 30 Grad, Kreisverkehr, Copacabana-Promenade,
Beco, Escadaria, Hangstraße, Viadukt, Tunnel mit Mittelstück, Serpentinen,
sanfte Steigung, komplette Brücke mit stapelbaren Pfeilern, Schnellstraße mit
Bögen, Auf- und Abfahrt und Viadukt, Stützmauern in 2/4/8 m mit Abschlüssen
und Böschung. Die Beschreibung aller Teile liegt in
`Assets/RioStrassen/ANLEITUNG_STRASSEN.md`.

**Farbsystem**: Materialien `slot_*` werden pro Haus nach einem Seed
umgefärbt. Die Favela-Reihe allein hat 2,1 Millionen Kombinationen.

## Nicht im Repo

`Assets/_Project/Generated/` — erzeugte Meshes und Hausdaten, rund 47 MB.
Entstehen bei festem Seed in Sekunden neu. Nach einem Clone einmal beide
Generatoren laufen lassen.
