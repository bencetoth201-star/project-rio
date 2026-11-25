# PROJECT RIO

Open-World-Krimi-Sandbox für Mobile, angesiedelt in Rio de Janeiro.
Arbeitsstand: Grundgerüst. Das Spielkonzept liegt in
`GTA Mobile Rio - Spielkonzept (kombiniert).md` auf dem Desktop.

- **Unity 6000.6.10f1**, URP, Zielplattform Android
- **Modus:** erst Solo, Online später
- **Stil:** Low-Mid-Poly, flach schattiert, keine Texturen

## Nach einem frischen Clone

Die Welt und die Spielfigur liegen **nicht** im Repo. Beide entstehen
prozedural aus den Generatoren unter `Assets/_Project/Editor/`, mit festem
Seed, also jedes Mal identisch. 47 MB Mesh-Assets in der Versionsverwaltung
wären Unsinn, wenn zwei Sekunden Rechenzeit dasselbe liefern.

Deshalb nach dem Clone einmal, in dieser Reihenfolge:

1. **Tools → Rio → Welt bauen (20 km2)**
2. **Tools → Rio → Block-Figur bauen**

Die Szene selbst liegt ebenfalls nicht im Repo - sie ist das Ergebnis der
Generatoren, nicht ihre Quelle. Vor dem ersten Bau ist sie leer oder gar
nicht da. Das ist erwartet.

**Zu zweit arbeiten:** siehe `ZUSAMMENARBEIT.md`.

Beide Generatoren brechen ab, wenn der Play Mode läuft — dort gebaute
Objekte verschwinden beim Stoppen wieder.

## Was wo liegt

| Pfad | Inhalt |
|---|---|
| `Assets/_Project/Editor/World/` | Weltgenerator: Kartendaten, Gelände, Hausverteilung, Wahrzeichen |
| `Assets/_Project/Editor/Player/` | Generator für die Block-Figur |
| `Assets/_Project/Scripts/Player/` | Steuerung, Kamera, Lauf-Rig (Laufzeitcode) |
| `Assets/_Project/Scripts/World/` | Hausdaten und Streaming (Laufzeitcode) |
| `Assets/RioHaeuser/` | Die 19 Gebäudemodelle samt Farbsystem |
| `Assets/_Project/Materials/` | Flache Farbmaterialien |
| `Assets/_Project/Generated/` | Erzeugte Meshes und Hausdaten — nicht im Repo |

## Häuser: nah echt, fern Klötze

32.000 Gebäude passen nicht gleichzeitig in den Speicher — alle auf einmal
wären 75 Millionen Dreiecke. Deshalb zwei Darstellungen aus derselben
Platzierungsliste:

- **Fern:** grobe Klötze, nach Chunks von 300 m gruppiert. Stehen immer.
- **Nah:** die echten Modelle, vom `RioHouseStreamer` im Umkreis von 420 m
  erzeugt und darüber hinaus wieder gelöscht. Der zugehörige Fern-Chunk wird
  dabei ausgeblendet, sonst stünde alles doppelt.

Entladen passiert erst 80 m weiter draußen als das Laden, sonst würde ein
Chunk an der Grenze bei jedem Schritt flackern. Pro Frame entstehen höchstens
25 Häuser, damit das Betreten eines Chunks keinen Ruckler gibt. Beide Werte
stehen am `RioHouseStreamer` auf der Weltwurzel.

`RioWorldLayout.cs` ist die Karte als Daten. Alle Koordinaten stehen dort in
Pixeln der Whiteboard-Skizze; wer etwas verschieben will, vergleicht mit der
Skizze, nicht mit Weltkoordinaten.

## Steuerung

WASD laufen · Shift rennen · Leertaste springen · Maus schauen · Escape
gibt den Mauszeiger frei.

## Stand der Welt

Rund 22 km² Land auf 8,2 × 5,6 km, zwei Inseln nach der Skizze: **Rio** im
Westen mit See, Strandbogen und den Morros an der Südküste, **Niterói** im
Osten, verbunden über eine Brücke. Dazu 17 Orte aus der Legende —
Flughäfen, Banken, Krankenhäuser, Stadion, Hafen, Casino, Militärbasis,
Jesus-Statue.

Alles ist Blockout: Lage, Größe und Wege stimmen, die Optik nicht.
