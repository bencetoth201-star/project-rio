# Anleitung für Claude Code: Rio-Strand (20 Strandobjekte)

Low-Poly-Strandobjekte für Copacabana/Ipanema im Stil der Rio-Häuser und -Straßen.

## Inhalt
```
Assets/RioStrand/Modelle/*.glb   20 Objekte (glTF binär, mit Ankerpunkten)
Assets/RioStrand/strand.json     Name, Kategorie, Fläche, Dreiecke, Anker, Farb-Slots
Extras/OBJ/                      dieselben Objekte als .obj/.mtl (ohne Anker)
Extras/Generator/                Python-Generator (beach.py)
```
Import über **glTFast** (`com.unity.cloud.gltfast`), Ordner `Assets/RioStrand` ins Projekt kopieren.

## Konventionen
- 1 Einheit = 1 m, **Y oben**, Pivot = Mitte der Fläche auf dem Sand (y = 0). Vorderseite +Z (zum Meer bzw. zur Kundenseite).
- Der Rettungsturm schaut mit dem Fenster nach +Z (Meer), die Rampe führt nach −Z (Promenade).
- Farb-Slots `slot_accent_*` (Sonnenschirme, Strandtücher, Hängematte, Kiosk-Schirme) mit `RioHausFarben` aus dem Häuserpaket würfeln.
- glTFast spiegelt die X-Achse beim Import → Ankerpositionen immer aus den Child-Transforms lesen.
- Nachts leuchten: `glass_shop`, `glass`, `screen`, `lamp` (Emission setzen).

## Platzierung am Strand (Vorschlag)
- Promenade (Copacabana-Wellenmuster aus dem Straßenpaket) → Kiosk, Eisladen, Duschen, Fitness, Leihräder, Mülleimer.
- Sandstreifen dahinter → Palmen, Barracas (alle 30–50 m), Rettungstürme (alle ~400 m, wie die „Postos“).
- Wassernähe → Sonnenschirme, Tücher, Sandburgen, Volleyball, Fußballtore, Surfbretter, Fischerboote (bei Posto 6).
- Zufällig streuen: Schirm-Set, Tücher, Sandburg, Palmen (mit zufälliger Drehung um Y, Farben würfeln).
- Hitboxen: **Tools ▸ Rio ▸ Hitboxen einrichten** ausführen – nutzt die mitgelieferten `__kollision`-Nodes
  (Stämme, Kiosk, Turm, Boot sind fest; Tücher, Palmwedel und Kleinkram nicht).

## Objekte (Anker in glTF-Koordinaten x, y, z; yaw)
| Datei | Name | Kategorie | Fläche B × T (m) | Dreiecke | Anker |
|---|---|---|---|---|---|
| `01_kokospalme.glb` | Kokospalme | Natur | 6 × 6 | 660 | – |
| `02_palmengruppe.glb` | Palmengruppe | Natur | 10 × 10 | 1500 | – |
| `03_haengematte.glb` | Hängematte zwischen Palmen | Natur | 12 × 6 | 600 | `Liegen` (-0.048, 0.8, 0.611; 0°) |
| `04_rettungsturm.glb` | Rettungsturm (Salva-Vidas) | Rettung | 6 × 10 | 1570 | `Rettungsschwimmer` (-0.243, 2.55, 3.07; 0°)<br>`Rampe_Unten` (-0.243, 0, -3.93; 180°) |
| `05_duschen.glb` | Strandduschen | Rettung | 4 × 2 | 668 | `Duschen` (0, 0.08, 0; 0°) |
| `06_muell_warnflagge.glb` | Mülleimer + Warnflagge | Rettung | 4 × 2 | 1440 | `Muell` (-1.09, 0, 1.15; 0°) |
| `07_strandkiosk.glb` | Strandkiosk (Quiosque) | Essen | 10 × 8 | 3018 | `Kaufen` (-0.004, 0.12, 1.8; 0°) |
| `08_eisladen.glb` | Eisladen | Essen | 6 × 4 | 1306 | `Kaufen` (0, 0.1, 2.2; 0°) |
| `09_eiswagen.glb` | Eis-Fahrrad (Picolé) | Essen | 2 × 4 | 956 | `Kaufen` (0, 0, 2; 0°) |
| `10_kokos_stand.glb` | Kokosnuss-Stand | Essen | 4 × 4 | 1536 | `Kaufen` (-0.08, 0, 1.5; 0°) |
| `11_barraca.glb` | Barraca (Strandverleih) | Essen | 4 × 4 | 1414 | `Kaufen` (0, 0, 2.55; 0°)<br>`Stuehle_Mieten` (-0.8, 0, 1.85; 0°) |
| `12_sandburg.glb` | Sandburg | Freizeit | 4 × 4 | 752 | – |
| `13_sonnenschirm_set.glb` | Sonnenschirm + 2 Stühle | Freizeit | 4 × 4 | 236 | `Sitz_1` (-0.7, 0.33, 1.1; 0°)<br>`Sitz_2` (0.4, 0.33, 1.1; 0°) |
| `14_strandtuch_set.glb` | Strandtücher-Set | Freizeit | 4 × 2 | 324 | `Liegen_1` (-0.604, 0.02, 0.05; 0°)<br>`Liegen_2` (0.696, 0.02, 0.05; 0°) |
| `15_volleyball.glb` | Beachvolleyball-Feld | Sport | 18 × 14 | 276 | `Feld_Mitte` (0, 0, 0.893; 0°) |
| `16_fussballtor.glb` | Beach-Soccer-Tor | Sport | 6 × 6 | 220 | `Tor` (0, 0, -0.11; 0°) |
| `17_fitness.glb` | Fitness-Station | Sport | 6 × 4 | 424 | `Training` (0, 0.05, 0; 0°) |
| `18_surfbretter.glb` | Surfbrett-Ständer | Sport | 4 × 2 | 760 | `Surfbrett` (-0.866, 0, 1.171; 0°) |
| `19_leihraeder.glb` | Leihrad-Station | Freizeit | 8 × 2 | 2136 | `Fahrrad_Leihen` (0, 0.08, 1.285; 0°) |
| `20_fischerboot.glb` | Fischerboot | Freizeit | 4 × 6 | 880 | `Boot` (-0.627, 0, 0.2; 0°) |
