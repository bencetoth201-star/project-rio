# Anleitung für Claude Code: Rio-Hafen (Containerhafen-Baukasten)

17 Teile: 300-m-Containerschiff (begehbar), Schlepper, Kaikanten, Molenkopf mit Leuchtturm, Hafenfläche,
Containerbrücke (STS), Portalkran (RTG), Containerstapler, Container 20/40 Fuß, Containerstapel, Hafenbüro, Hafentor, Zaun, Flutlichtmast.

## Inhalt
```
Assets/RioHafen/Modelle/*.glb   17 Teile (glTF binär, mit Ankern und animierbaren Unter-Nodes)
Assets/RioHafen/hafen.json      Name, Kategorie, Maße, Dreiecke, Anker, Farb-Slots
Extras/OBJ/                     .obj/.mtl (ohne Anker)
Extras/Generator/               Python-Generator (hafen.py)
```
Import über **glTFast**, Ordner `Assets/RioHafen` ins Projekt kopieren.

## Höhen + Koordinaten (wichtig)
- 1 Einheit = 1 m, Y oben. **Wasserspiegel = y 0, Kai-Oberkante = y +3.** Den ganzen Hafen so setzen,
  dass y 0 der Modelle auf der Wasserhöhe deiner Szene liegt (Hafen-Root-Objekt auf y = Wasserhöhe).
- **Kaikanten** (`03`–`05`): Pivot = Kachelmitte auf Wasserhöhe, Wasserseite **+Z** (Kante bei z = +6), Land nach −Z.
  24-m- und 12-m-Kacheln entlang X aneinanderreihen (`Anschluss_Kai_W/O`). Die Ecke ist 36 × 36 m (Wasser bei +Z und +X).
  Kranschienen: wasserseitig z = +3,5, landseitig z = −27 (Spurweite 30,5 m). Unterwasserteil bis y −6.
- **Alles, was auf dem Kai steht** (Kräne, Container, Gebäude, Zaun, Masten): Pivot unten Mitte → auf y = +3 setzen.
- **Containerbrücke** auf den Anker `Kran_Mitte` einer Kaikante setzen (dann stehen die Räder auf beiden Schienen,
  der Ausleger ragt 55 m über das Wasser). Kann entlang X auf den Schienen fahren.
- **Schiffe**: Pivot Mitte auf der Wasserlinie, Bug +X. Das Containerschiff auf den Anker `Liegeplatz` einer Kaikante
  setzen (Schiff parallel zur Kante, Backbordseite zum Kai). Der Rumpf reicht bis y −13 unter Wasser.
- glTFast spiegelt X beim Import → Ankerpositionen immer aus den Child-Transforms lesen.

## Containerschiff „RIOMAR ATLÂNTICO“ (300 × 40 m)
- Nodes: `Aussen__…` (Rumpf, Deck, Lukendeckel, Laschbrücken, Reling), `Ladung__cont_*` (~1.100 Container als Stapel),
  `Aufbau__…` (Wohndeck mit 8 Decks, Außentreppen, Schornstein, Rettungsboote, Gangway),
  `OG__…` (Brücke innen: Fahrpult, Bildschirme, Steuerrad, Stühle, Kartentisch, Brückennocken), `Dach__…` (Brückendach, Radarmast).
- **Begehbar:** Hauptdeck y = +10 (Seitengänge an der Reling, Achterdeck), Back y = +12,5, alle Aufbau-Decks (Balkone mit Reling,
  Zick-Zack-Außentreppe hinten), Brücke y = +31. **Gangway** auf der Kai-Seite: `Gangway_Unten` (Kai, y 3) → `Gangway_Oben` (Deck).
- Hitboxen: mitgelieferte `__kollision`-Nodes (Deck, Aufbau, Gangway, Containerstapel) – **Tools ▸ Rio ▸ Hitboxen einrichten**.
  Zum Betreten der Brücke `Dach__*` ausblenden. Rumpffarbe = Slot `slot_hull_1`.
- Die `Ladung` ist ein eigener Node: ausblenden = leeres Schiff mit grünen Lukendeckeln (z. B. beim Entladen).

## Megayacht SOBERANA
Die 150-m-Megayacht ist jetzt im eigenen **Heist-Paket** (`Assets/RioHeists`, `02_yacht_soberana.glb`, Anleitung
`ANLEITUNG_CLAUDE_CODE_HEISTS.md`). Platzierung am Kai wie dort beschrieben: Anker `Liegeplatz` einer Kaikante, dann −8 m in Z.

## Animierbare Teile
- Containerbrücke: `Laufkatze` (Pivot auf dem Ausleger, entlang lokal **Z** verschieben, Position z = −35 … +65, Start +40) und `Spreader`
  (Pivot am Spreader, entlang **Y** verschieben, Position y = +5 … +40, Start +26). Seile hängen an der Laufkatze → Spreader an die Laufkatze parenten.
- Portalkran: `Laufkatze` entlang lokal **X**.
- Hafentor: `Schranke_1/2` um die lokale **Z**-Achse um 85° drehen (Pivot am Schrankenfuß).
- Container: Slot `slot_container_1` (10 Farben) mit `RioHausFarben` würfeln. Anker `Oben` = Stapelpunkt (+2,59 m), `Tuer` = Tür.

## Nachts
Leuchtend: `lamp` (Flutlicht, Kranleuchten, Leuchtturm), `led_red` (Flugwarnlichter), `glass`, `screen`, `dach_licht` (Topplicht),
`pool`.

## Teile (Anker in glTF-Koordinaten x, y, z; yaw)
| Datei | Name | Kategorie | B × T × H (m) | Dreiecke | Anker |
|---|---|---|---|---|---|
| `01_containerschiff.glb` | Containerschiff RIOMAR ATLÂNTICO (300 m) | Schiffe | 303.1 × 41.7 × 58 | 35269 | `Bruecke` (-114.6, 31.2, 0; 90°)<br>`Gangway_Unten` (-86, 3, -21.1; 90°)<br>`Gangway_Oben` (-100, 10, -19.5; 90°)<br>`Deck_Achtern` (-108, 10, -18.5; 0°)<br>`Back_Bug` (138, 12.5, 0; 90°)<br>`Festmachen_Bug` (136, 12.5, -5; 0°)<br>`Festmachen_Heck` (-146, 10, -10; 0°) |
| `02_schlepper.glb` | Schlepper | Schiffe | 32.8 × 11.5 × 15.8 | 1475 | `Steuer` (2, 5, 0; 90°)<br>`Schlepphaken` (-12, 4.4, 0; -90°) |
| `03_kaikante_24.glb` | Kaikante 24 m | Kai | 24 × 36.8 × 10 | 880 | `Kran_Mitte` (0, 3, -11.75; 0°)<br>`Liegeplatz` (0, 0, 28; 0°)<br>`Anschluss_Kai_W` (-12, 3, 0; -90°)<br>`Anschluss_Kai_O` (12, 3, 0; 90°) |
| `04_kaikante_12.glb` | Kaikante 12 m | Kai | 12 × 36.8 × 10 | 620 | `Kran_Mitte` (0, 3, -11.75; 0°)<br>`Liegeplatz` (0, 0, 28; 0°)<br>`Anschluss_Kai_W` (-6, 3, 0; -90°)<br>`Anschluss_Kai_O` (6, 3, 0; 90°) |
| `05_kaikante_ecke.glb` | Kaikante Ecke (außen) | Kai | 36.8 × 36.8 × 10 | 1428 | `Kran_Mitte` (0, 3, -11.75; 0°)<br>`Liegeplatz` (0, 0, 28; 0°)<br>`Anschluss_Kai_W` (-18, 3, 0; -90°)<br>`Anschluss_Kai_O` (18, 3, 0; 90°) |
| `06_molenkopf.glb` | Molenkopf mit Leuchtturm | Kai | 17.3 × 32.9 × 26.6 | 2654 | `Anschluss_Mole` (0, 3, -24; 180°)<br>`Leuchtturm` (0, 3, 0; 0°) |
| `07_hafenflaeche.glb` | Hafenfläche / Stellplätze | Kai | 24 × 24 × 0.3 | 792 | `Stapelplatz_1` (-5.3, 3, -5.3; 0°)<br>`Stapelplatz_2` (5.6, 3, 6.4; 0°) |
| `08_containerkran.glb` | Containerbrücke (STS-Kran) | Kräne | 22.4 × 110.2 × 76.5 | 2712 | `Kabine` (0, 41, 39.2; 0°)<br>`Rad_Wasserseite` (0, 0, 15.25; 0°)<br>`Rad_Landseite` (0, 0, -15.25; 0°) |
| `09_portalkran.glb` | Portalkran (RTG) | Kräne | 15.6 × 25.5 × 23 | 672 | `Fahrspur` (0, 0, 8.75; 0°) |
| `10_containerstapler.glb` | Containerstapler | Kräne | 13.8 × 12.6 × 9.7 | 476 | `Fahrer` (-0.7, 2.6, 1; 90°) |
| `11_container_20.glb` | Container 20 Fuß | Container | 6.2 × 2.5 × 2.6 | 468 | `Tuer` (3.54, 0, 0; 90°)<br>`Oben` (0, 2.59, 0; 0°) |
| `12_container_40.glb` | Container 40 Fuß | Container | 12.3 × 2.5 × 2.6 | 732 | `Tuer` (6.605, 0, 0; 90°)<br>`Oben` (0, 2.59, 0; 0°) |
| `13_containerstapel.glb` | Containerstapel | Container | 12.2 × 12.2 × 10.4 | 228 | `Oben_Mitte` (0, 10.4, 0; 0°) |
| `14_hafenbuero.glb` | Hafenbüro | Gebäude | 17.7 × 11 × 14 | 1152 | `Eingang` (0, 0.2, 6; 0°) |
| `15_hafentor.glb` | Hafentor mit Wachhaus | Gebäude | 25 × 8 × 8 | 1816 | `Wachmann` (0, 0.25, 0; 0°)<br>`Spur_Rein` (-6, 0, 0; 0°)<br>`Spur_Raus` (6, 0, 0; 180°)<br>`Zaun_W` (-12.3, 0, 0; -90°)<br>`Zaun_O` (12.3, 0, 0; 90°) |
| `16_zaun.glb` | Hafenzaun 24 m | Gebäude | 24.1 × 0.5 × 3.5 | 1168 | `Anschluss_W` (-12, 0, 0; -90°)<br>`Anschluss_O` (12, 0, 0; 90°) |
| `17_flutlichtmast.glb` | Flutlichtmast 35 m | Gebäude | 4.4 × 4.4 × 37.1 | 1848 | – |
