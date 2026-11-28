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
| `Assets/RioHaeuser/` | 33 Gebäudemodelle samt Farbsystem, darunter Stadion, Nachtclub, Juwelier, Kleiderladen, Tuning-Werkstatt |
| `Assets/RioStrassen/` | 56 Straßen-, Brücken- und Geländeteile |
| `Assets/RioHafen/`, `Assets/RioStrand/`, `Assets/RioFahrzeuge/` | Hafen-Baukasten mit Containerschiff, 20 Strandobjekte, Valente GT |
| `Assets/RioHeists/` | Casa Império (Banco Central), Megayacht SOBERANA, Maya-Kanalisation, Beute- und Abriegelungs-Skripte |
| `Assets/RioOnline/` | Flughafen, Cristo, Krankenhaus, Militärbasis, Casino, Hochhäuser, Drughouse, Fahrzeuge, Waffen |
| `Assets/RioGemeinsam/` | Hitboxen für alle Modelle (`Tools → Rio → Hitboxen einrichten`) |
| `Docs/Modelle/` | Anleitungen zu allen Modellpaketen (Anker, bewegliche Teile, Heist-Abläufe) |
| `Assets/_Project/Materials/` | Flache Farbmaterialien |
| `Assets/_Project/Generated/` | Erzeugte Meshes und Hausdaten — nicht im Repo |

## Wahrzeichen

Die Orte aus der Legende sind keine Klötze mehr, sondern echte Modelle
(`Assets/_Project/Editor/World/RioWahrzeichen.cs`). Jedes sucht sich beim
Weltbau einen Platz nahe seinem Ort auf der Karte, nimmt seine Fläche aus
der Stadt, ebnet das Gelände ein und wird am Ende mit Hitboxen und Farben
gesetzt. Dazu kommen Hochhäuser, Drughouse, Tuning-Werkstatt, ein
Containerhafen an der Küste und Strandobjekte am Strandbogen. Die Lage
folgt der Whiteboard-Skizze; der große Flughafen liegt wie Santos Dumont
als Halbinsel an Rios Ostküste.

## Stadt und Gang-Viertel

Gebaut wird nur innerhalb der beiden grünen Stadtumrisse der Skizze
(`RioWorldLayout.StadtRioPx` / `StadtNiteroiPx`), dort aber dicht: die
Viertel in `RioDistricts.cs` sind bewusst zu groß und überlappen sich, der
Umriss schneidet sie außen ab. Außerhalb liegt nur freies Land.

**Organisch statt Raster:** Jedes Viertel hat unregelmäßige
Straßenabstände, und einzelne Straßenstücke zwischen zwei Kreuzungen fallen
weg (`RioDistricts.IstStrasse`). Die Blocks sind deshalb ungleich und
verwinkelt. Häuser stehen nicht in Reihe: An jeder Straßenkante wird eines
mit zufälligem Versatz, eigenem Abstand zur Straße und leicht schräg
versucht, danach füllen Hinterhäuser die Blockmitte
(`RioHousePlanner.BlockBebauen`).

Die beiden Gang-Viertel (`Kind.Favela`): **Jacarezinho** in Rio zwischen
Zona Oeste und Stadion (mit dem Drughouse) und **Morro do Estado** im Osten
von Niterói. Dort stehen nur arme Häuser, die Gassen sind enger und
brechen öfter ab, und jede Lücke bis den Hang hinauf füllen Favela-Hütten.
Hütten gibt es sonst nirgends.

Die beiden kleinen Flugplätze bekommen später ein eigenes Modell; bis dahin
steht dort nichts.

Die Casa Império (Banco Central) hat drei Untergeschosse und darunter die
Maya-Kanalisation - dort bleibt das Gelände offen. Die abgeriegelte Version
liegt deckungsgleich und abgeschaltet daneben (`RioAbriegelung`).

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
