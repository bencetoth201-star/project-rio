# Anleitung für Claude Code: Rio-Straßennetz (55 Straßen- und Geländeteile)

Modularer Straßen- und Geländebaukasten im Low-Poly-Stil der Rio-Häuser (gleiche Farben, gleiches 2-m-Raster).

## Inhalt
```
Assets/RioStrassen/Modelle/*.glb   alle Teile + 00_beispiel_viertel.glb (Demo, nur zum Anschauen)
Assets/RioStrassen/strassen.json   Größe, Dreiecke und Anker (mit Drehung) jedes Teils
Extras/OBJ/                        dieselben Teile als .obj/.mtl (ohne Anker)
Extras/Generator/                  Python-Generator (roads.py, roads2.py, roads3.py)
```
Import über **glTFast** (`com.unity.cloud.gltfast`). Ordner `Assets/RioStrassen` ins Projekt kopieren.

## Konventionen
- 1 Einheit = 1 m. Fahrbahn-Oberfläche **y = 0**, Gehweg **y = +0,15**.
- **Querschnitte:** Rua 12 m (2,5 · 7 · 2,5) · Avenida 24 m (3 · 7 · 4 Mittelstreifen · 7 · 3) · Beco 3 m ·
  Schnellstraße 24 m (je Richtung: Leitwand · 2,1 Seitenstreifen · 2 × 3,5 Spuren · 1,5 Standstreifen · 1 Bankett) · Brücke 12 m (eine Richtung, 2 Spuren).
- **Rechteck-Kacheln** (Gerade, Kreuzungen, T, Übergang …): Pivot = Kachelmitte, Maße in 2-m-Schritten, Anschlüsse mittig auf den Kanten.
- **Bögen (21–25):** Pivot = **Start-Anschluss** (`…_S`) im Ursprung, Fahrtrichtung −Z. Der Bogen dreht nach **links**;
  rückwärts befahren ist er ein Rechtsbogen – deshalb gibt es nur eine Variante pro Winkel.
  Der End-Anschluss `…_Ende` hat Position **und Drehung** (yaw = 180° + Winkel). Radien: Rua 40 m, Avenida 60 m, Copacabana 120 m.
  Kombinationen: 6 × 15° = 3 × 30° = 90°. Bögen lassen sich mit Geraden und anderen Bögen beliebig verketten.
- **Copacabana-Bogen** dreht nach **rechts**, Strand innen (folgt der Bucht) – passt direkt an die gerade Copacabana-Kachel
  (gleicher Querschnitt, Avenida-Achse 6 m links der Kachelmitte der Geraden).
- **Anschlüsse** = leere Child-Objekte `Anschluss_<Typ>_<Seite>` (Typ = Rua | Avenida | Beco | Schnellstrasse | Bruecke). Ihre **lokale Z-Achse zeigt aus dem Teil hinaus**.
  Zwei Teile passen, wenn Typ gleich ist, die Anker-Positionen aufeinanderliegen und die Z-Achsen entgegengesetzt zeigen.
- **Achsen:** glTFast spiegelt beim Import die X-Achse. Ein `_E`-Anschluss liegt in Unity bei −X, ein Linksbogen wirkt wie gebaut.
  → **Immer die Anker-Transforms in Unity verwenden**, nicht Buchstaben oder die Tabellenwerte unten (die sind glTF-Koordinaten).
- **Höhen:** Steigung +3 m, Steigung sanft +1,5 m, Hangstraße +4 m, Escadaria +4 m (nach −Z ansteigend), Viadukt +6 m, Schnellstraßen-Viadukt +9 m.

## Tunnel (20 + 34)
- `20_tunnel` = Einfahrt (Portal, Nordhälfte ist schon Röhre). `34_tunnel_mitte` = 12 × 24 m gerade Röhre, gleicher Querschnitt
  (Wände x ±5,4…6,0, Decke y 6,4…6,8, oben 0,6 m Fels-Deckschicht bis y 7,4). Beliebig oft hintereinander setzen.
- Morro-Durchstich: Portal → n × Mittelstück → Portal um 180° gedreht. 600 m Berg ≈ 2 × 24 m Portal + 23 Mittelstücke.
- Die Röhre ist außen geschlossen, damit sie im Terrain verschwindet; das Terrain einfach darüber modellieren (Unterkante Deckschicht 6,8 m).

## Serpentinen + sanfte Steigung (35–37)
- **Serpentine 90° links/rechts:** Rua-Querschnitt, Radius 16 m (Achse), steigt **+3 m** über die Kurve. Pivot = unterer Start-Anschluss
  (Fahrtrichtung −Z), `Anschluss_Rua_Ende` liegt bei (∓16; 3; −16) mit yaw 270° bzw. 90°. Außen Leitplanke + Richtungstafeln.
  Rückwärts befahren geht es bergab – mit links + rechts sind alle vier Fälle abgedeckt. Zwei Serpentinen gegenläufig = S-Kurve +6 m,
  zwei gleiche = Spitzkehre (180°) +6 m.
- **Steigung sanft:** 12 × 24 m, +1,5 m (6 %), steigt nach −Z wie `06_rua_steigung`.

## Brücke nach Niterói (38–44)
- **Segment** 12 × 24 m: Deck mit New-Jersey-Leitwänden + Geländer, 2 Spuren (Fahrbahn exakt wie Rua: x ±3,5), Hohlkastenträger,
  **ohne Pfeiler**. Oberfläche y = 0, **Trägerunterkante y = −2,6**. Anker `Pfeiler_S/N` = Auflagerpunkt an den Kanten.
- **Segment +2 m:** dasselbe mit Steigung (+2 m auf 24 m), um Höhe zu gewinnen.
- **Zwei Segmente nebeneinander (x −6 / +6) = 24-m-Brücke mit 2 × 2 Spuren**, Spuren deckungsgleich mit der Schnellstraße.
- **Brückenkopf** 12 × 48 m: Rampe vom Boden (Süd, y 0) auf **+5 m** (Nord) mit Natursteinwänden + Widerlager;
  das nächste Segment liegt mit seinem Träger auf dem Widerlager (ragt 0,8 m unter das Segment). Für das andere Ufer 180° drehen.
- **Pfeiler zum Stapeln** (Pivot unten Mitte, Anker `Unten`/`Oben`): Fundament 1 m → beliebig viele Schäfte 2 m / 8 m → Kopf 1,4 m.
  **Deckhöhe über Grund/Wasser = 5 + 2 · (Schäfte 2 m) + 8 · (Schäfte 8 m).** Pfeiler mittig unter eine Segment-Fuge setzen
  (`Pfeiler_N` des einen = `Pfeiler_S` des nächsten), der Kopf trägt beide Träger.
  Beispiel: Brückenkopf (+5) → Segment +2 m (+7) → Segment +2 m (+9) → gerade Segmente. Pfeiler an der Fuge bei +5: nur Fundament + Kopf;
  bei +7: 1 Schaft 2 m; bei +9: 2 Schäfte 2 m; für +45 m: 5 Schäfte 8 m.

## Schnellstraße (45–50)
- **Gerade** 24 × 24, **Bögen 15° / 30°** (Radius 100 m, Pivot = Start wie bei den anderen Bögen, Linksbogen).
- **Abfahrt → Rua** (36 × 48 m, Pivot Kachelmitte): Schnellstraßen-Achse bei x = −6. Die Ausfädelspur verlässt die
  Nordrichtung (Fahrtrichtung −Z) nach rechts und endet an der Nordkante als **Rua-Anschluss bei x = +12**. Von dort mit
  `11_avenida_einmuendung` oder `27_uebergang_avenida_rua` an die Avenida. Mit Schilderbrücke „SAÍDA 3 / Centro“.
- **Auffahrt ← Rua**: dasselbe in Z gespiegelt – Rua-Anschluss an der Südkante, Einfädeln in die Nordrichtung.
  Für die Gegenrichtung beide Teile um 180° drehen.
- **Viadukt** 24 × 24 m auf **+9 m** mit Hammerkopf-Pfeiler in Kachelmitte (darunter je Seite 7 m frei für Straßen).
  Hochkommen: zwei Brückenköpfe nebeneinander (+5) + je 2 × Brückensegment +2 m (+9) → Viadukt.
- Rechtsverkehr: Nordrichtung = +X-Hälfte, Südrichtung = −X-Hälfte (in glTF-Koordinaten). Linke Randlinie gelb (wie in Brasilien).

## Gelände (28–33, 51–55)
- **Stützmauern:** Kachel 12 × 4 m, Mauerfront an der **+Z-Kante** (Stadtseite), Hang/Plateau dahinter (−Z) auf Höhe 4 m bzw. 8 m,
  mit Geländer oben. Anker `Oben` (Plateau) und `Unten` (vor der Mauer).
- **Ecke außen** (4 × 4 m): Mauerfronten an +Z und +X (Hangnase). **Ecke innen** (4 × 4 m): Plateau-Füllstück mit Eckpfeiler,
  dort wo zwei gerade Mauern rechtwinklig aufeinandertreffen.
- **Stützmauer 2 m** (51): wie 4 m, für die kleinen Stufen.
- **Mauerabschluss** 12 × 4 m (4 m und 2 m, je rechts/links): Mauer läuft auf 12 m von voller Höhe auf 0 aus, Hang dahinter fällt mit.
  „rechts“ = volle Höhe bei −X (dort `Anschluss_Mauer` an die gerade Mauer), läuft nach +X aus; „links“ gespiegelt.
- **Böschung:** Grashang 12 × 10 m, +4 m über 8 m (anstelle einer Mauer, wo Platz ist).
- **Mauer mit Treppe:** führt durch die 4-m-Mauer nach oben; Anker `Anschluss_Beco_Unten/Oben` → passt an Beco/Escadaria.
- Faustregel: Wo ein flaches Viertel an den Hang stößt, eine Reihe Mauern entlang der Hangkante setzen, alle 3–5 Mauern eine Treppe,
  an freien Stellen Böschungen. Das Terrain dahinter auf Plateau-Höhe bringen.

## Teile (Anker in glTF-Koordinaten x, y, z; yaw)
| Datei | Name | Größe B × T (m) | Anker | Dreiecke |
|---|---|---|---|---|
| `01_rua_gerade.glb` | Rua – Gerade | 12 × 12 | Anschluss_Rua_N (0, 0, -6; 180°)<br>Anschluss_Rua_S (0, 0, 6; 0°) | 356 |
| `02_rua_kurve.glb` | Rua – Kurve | 12 × 12 | Anschluss_Rua_S (0, 0, 6; 0°)<br>Anschluss_Rua_E (6, 0, 0; 90°) | 1776 |
| `03_rua_t.glb` | Rua – T-Kreuzung | 12 × 12 | Anschluss_Rua_N (0, 0, -6; 180°)<br>Anschluss_Rua_S (0, 0, 6; 0°)<br>Anschluss_Rua_E (6, 0, 0; 90°) | 484 |
| `04_rua_kreuzung.glb` | Rua – Kreuzung | 12 × 12 | Anschluss_Rua_N (0, 0, -6; 180°)<br>Anschluss_Rua_S (0, 0, 6; 0°)<br>Anschluss_Rua_E (6, 0, 0; 90°)<br>Anschluss_Rua_W (-6, 0, 0; -90°) | 956 |
| `05_rua_sackgasse.glb` | Rua – Wendehammer | 18 × 20 | Anschluss_Rua_S (0, 0, 10; 0°) | 1156 |
| `06_rua_steigung.glb` | Rua – Steigung +3 m | 12 × 24 | Anschluss_Rua_N (0, 3, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 260 |
| `07_rua_bushaltestelle.glb` | Rua – Bushaltestelle | 12 × 24 | Bushaltestelle (4.5, 0.15, 0; -90°)<br>Anschluss_Rua_N (0, 0, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 1156 |
| `08_rua_kopfstein.glb` | Rua – Kopfstein + Bonde | 12 × 12 | Bonde_Schienen (0, 0, 0; 0°)<br>Anschluss_Rua_N (0, 0, -6; 180°)<br>Anschluss_Rua_S (0, 0, 6; 0°) | 772 |
| `09_avenida_gerade.glb` | Avenida – Gerade | 24 × 24 | Anschluss_Avenida_N (0, 0, -12; 180°)<br>Anschluss_Avenida_S (0, 0, 12; 0°) | 1020 |
| `10_avenida_kreuzung.glb` | Avenida – Kreuzung | 24 × 24 | Anschluss_Avenida_N (0, 0, -12; 180°)<br>Anschluss_Avenida_S (0, 0, 12; 0°)<br>Anschluss_Avenida_E (12, 0, 0; 90°)<br>Anschluss_Avenida_W (-12, 0, 0; -90°) | 1964 |
| `11_avenida_einmuendung.glb` | Avenida – Einmündung Rua | 24 × 24 | Anschluss_Avenida_N (0, 0, -12; 180°)<br>Anschluss_Avenida_S (0, 0, 12; 0°)<br>Anschluss_Rua_E (12, 0, 0; 90°) | 1216 |
| `12_avenida_kurve.glb` | Avenida – Kurve | 36 × 36 | Anschluss_Avenida_S (0, 0, 18; 0°)<br>Anschluss_Avenida_E (18, 0, 0; 90°) | 4420 |
| `13_kreisverkehr.glb` | Kreisverkehr | 36 × 36 | Anschluss_Rua_N (0, 0, -18; 180°)<br>Anschluss_Rua_S (0, 0, 18; 0°)<br>Anschluss_Rua_E (18, 0, 0; 90°)<br>Anschluss_Rua_W (-18, 0, 0; -90°) | 3744 |
| `14_copacabana.glb` | Copacabana-Promenade | 36 × 24 | Anschluss_Avenida_N (-6, 0, -12; 180°)<br>Anschluss_Avenida_S (-6, 0, 12; 0°)<br>Strand (14.5, -0.45, 0; 90°) | 2626 |
| `15_beco.glb` | Beco – Gasse | 4 × 12 | Anschluss_Beco_N (0, 0.05, -6; 180°)<br>Anschluss_Beco_S (0, 0.05, 6; 0°) | 48 |
| `16_escadaria.glb` | Escadaria – Treppe +4 m | 4 × 12 | Anschluss_Beco_S (0, 0, 6; 0°)<br>Anschluss_Beco_N (0, 4, -6; 180°) | 792 |
| `17_beco_gabelung.glb` | Beco – Gabelung | 12 × 12 | Anschluss_Beco_N (0, 0.05, -6; 180°)<br>Anschluss_Beco_S (0, 0.05, 6; 0°)<br>Anschluss_Beco_E (6, 0.05, 0; 90°) | 100 |
| `18_hangstrasse.glb` | Favela-Hangstraße +4 m | 12 × 24 | Anschluss_Rua_N (0, 4, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 604 |
| `19_viadukt.glb` | Viadukt (+6 m) | 12 × 24 | Anschluss_Rua_N (0, 6, -12; 180°)<br>Anschluss_Rua_S (0, 6, 12; 0°)<br>Unterfahrt (0, 0, 0; 90°) | 472 |
| `20_tunnel.glb` | Tunnel-Einfahrt | 12 × 24 | Tunnel_Eingang (0, 0, 0; 0°)<br>Anschluss_Rua_N (0, 0, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 490 |
| `21_rua_bogen_15.glb` | Rua – Bogen 15° | 13.2 × 11.9 | Anschluss_Rua_S (0, 0, 0; 0°)<br>Anschluss_Rua_Ende (-1.363, 0, -10.353; 195°) | 624 |
| `22_rua_bogen_30.glb` | Rua – Bogen 30° | 16.6 × 23 | Anschluss_Rua_S (0, 0, 0; 0°)<br>Anschluss_Rua_Ende (-5.359, 0, -20; 210°) | 1104 |
| `23_avenida_bogen_15.glb` | Avenida – Bogen 15° | 25.6 × 18.6 | Anschluss_Avenida_S (0, 0, 0; 0°)<br>Anschluss_Avenida_Ende (-2.044, 0, -15.529; 195°) | 1292 |
| `24_avenida_bogen_30.glb` | Avenida – Bogen 30° | 30.4 × 36 | Anschluss_Avenida_S (0, 0, 0; 0°)<br>Anschluss_Avenida_Ende (-8.038, 0, -30; 210°) | 2372 |
| `25_copacabana_bogen_15.glb` | Copacabana – Bogen 15° | 39.3 × 34.2 | Anschluss_Avenida_S (0, 0, 0; 0°)<br>Anschluss_Avenida_Ende (4.089, 0, -31.058; 165°)<br>Strand (23.83, -0.45, -12.661; 90°) | 3172 |
| `26_avenida_t.glb` | Avenida – T-Kreuzung | 24 × 24 | Anschluss_Avenida_N (0, 0, -12; 180°)<br>Anschluss_Avenida_S (0, 0, 12; 0°)<br>Anschluss_Avenida_E (12, 0, 0; 90°) | 1436 |
| `27_uebergang_avenida_rua.glb` | Übergang Avenida → Rua | 24 × 24 | Anschluss_Avenida_S (0, 0, 12; 0°)<br>Anschluss_Rua_N (0, 0, -12; 180°) | 304 |
| `28_stuetzmauer_4m.glb` | Stützmauer 4 m | 12 × 4 | Oben (0, 4, 0; 180°)<br>Unten (0, 0, 2.5; 0°) | 424 |
| `29_stuetzmauer_8m.glb` | Stützmauer 8 m (Naturstein) | 12 × 4 | Oben (0, 8, 0; 180°)<br>Unten (0, 0, 2.5; 0°) | 2368 |
| `30_mauer_ecke_aussen.glb` | Stützmauer – Ecke außen | 4 × 4 | Ecke (0, 0, 0; 0°) | 276 |
| `31_mauer_ecke_innen.glb` | Stützmauer – Ecke innen | 4 × 4 | Ecke (0, 0, 0; 0°) | 56 |
| `32_boeschung.glb` | Böschung 4 m | 12 × 10 | Oben (0, 4, -4; 180°)<br>Unten (0, 0, 5.6; 0°) | 396 |
| `33_mauer_treppe.glb` | Stützmauer mit Treppe | 12 × 8 | Anschluss_Beco_Unten (0, 0, 4.6; 0°)<br>Anschluss_Beco_Oben (0, 4, -4; 180°) | 840 |
| `34_tunnel_mitte.glb` | Tunnel – Mittelstück | 12 × 24 | Tunnel_Innen (0, 0, 0; 0°)<br>Anschluss_Rua_N (0, 0, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 888 |
| `35_rua_serpentine_links.glb` | Rua – Serpentine 90° links +3 m | 22 × 22 | Anschluss_Rua_S (0, 0, 0; 0°)<br>Anschluss_Rua_Ende (-16, 3, -16; 270°) | 3932 |
| `36_rua_serpentine_rechts.glb` | Rua – Serpentine 90° rechts +3 m | 22 × 22 | Anschluss_Rua_S (0, 0, 0; 0°)<br>Anschluss_Rua_Ende (16, 3, -16; 90°) | 3932 |
| `37_rua_steigung_sanft.glb` | Rua – Steigung sanft +1,5 m | 12 × 24 | Anschluss_Rua_N (0, 1.5, -12; 180°)<br>Anschluss_Rua_S (0, 0, 12; 0°) | 356 |
| `38_bruecke_segment.glb` | Brücke – Segment gerade | 12 × 24 | Anschluss_Bruecke_S (0, 0, 12; 0°)<br>Pfeiler_S (0, -2.6, 12; 0°)<br>Anschluss_Bruecke_N (0, 0, -12; 180°)<br>Pfeiler_N (0, -2.6, -12; 0°) | 632 |
| `39_bruecke_steigung.glb` | Brücke – Segment +2 m | 12 × 24 | Anschluss_Bruecke_S (0, 0, 12; 0°)<br>Pfeiler_S (0, -2.6, 12; 0°)<br>Anschluss_Bruecke_N (0, 2, -12; 180°)<br>Pfeiler_N (0, -0.6, -12; 0°) | 632 |
| `40_brueckenkopf.glb` | Brückenkopf – Rampe +5 m | 12 × 48 | Anschluss_Bruecke_S (0, 0, 24; 0°)<br>Anschluss_Bruecke_N (0, 5, -24; 180°) | 496 |
| `41_pfeiler_fundament.glb` | Pfeiler – Fundament 1 m | 8 × 6 | Unten (0, 0, 0; 0°)<br>Oben (0, 1, 0; 0°) | 52 |
| `42_pfeiler_schaft_2m.glb` | Pfeiler – Schaft 2 m | 4 × 4 | Unten (0, 0, 0; 0°)<br>Oben (0, 2, 0; 0°) | 56 |
| `43_pfeiler_schaft_8m.glb` | Pfeiler – Schaft 8 m | 4 × 4 | Unten (0, 0, 0; 0°)<br>Oben (0, 8, 0; 0°) | 56 |
| `44_pfeiler_kopf.glb` | Pfeiler – Kopf 1,4 m | 8 × 4 | Unten (0, 0, 0; 0°)<br>Traeger_Unterkante (0, 1.4, 0; 0°) | 52 |
| `45_schnellstrasse_gerade.glb` | Schnellstraße – Gerade | 24 × 24 | Anschluss_Schnellstrasse_N (0, 0, -12; 180°)<br>Anschluss_Schnellstrasse_S (0, 0, 12; 0°) | 568 |
| `46_schnellstrasse_bogen_15.glb` | Schnellstraße – Bogen 15° | 27 × 29 | Anschluss_Schnellstrasse_S (0, 0, 0; 0°)<br>Anschluss_Schnellstrasse_Ende (-3.407, 0, -25.882; 195°) | 1240 |
| `47_schnellstrasse_bogen_30.glb` | Schnellstraße – Bogen 30° | 35.8 × 56 | Anschluss_Schnellstrasse_S (0, 0, 0; 0°)<br>Anschluss_Schnellstrasse_Ende (-13.397, 0, -50; 210°) | 2368 |
| `48_schnellstrasse_abfahrt.glb` | Schnellstraße – Abfahrt → Rua | 36 × 48 | Anschluss_Schnellstrasse_S (-6, 0, 24; 0°)<br>Anschluss_Schnellstrasse_N (-6, 0, -24; 180°)<br>Anschluss_Rua_N (12, 0, -24; 180°) | 4864 |
| `49_schnellstrasse_auffahrt.glb` | Schnellstraße – Auffahrt ← Rua | 36 × 48 | Anschluss_Schnellstrasse_S (-6, 0, 24; 0°)<br>Anschluss_Schnellstrasse_N (-6, 0, -24; 180°)<br>Anschluss_Rua_S (12, 0, 24; 0°) | 3104 |
| `50_schnellstrasse_viadukt.glb` | Schnellstraße – Viadukt +9 m | 24 × 24 | Anschluss_Schnellstrasse_N (0, 9, -12; 180°)<br>Anschluss_Schnellstrasse_S (0, 9, 12; 0°)<br>Unterfahrt_West (-7, 0, 0; 90°)<br>Unterfahrt_Ost (7, 0, 0; -90°) | 792 |
| `51_stuetzmauer_2m.glb` | Stützmauer 2 m | 12 × 4 | Oben (0, 2, 0; 180°)<br>Unten (0, 0, 2.5; 0°) | 424 |
| `52_mauerabschluss_4m_rechts.glb` | Mauerabschluss 4 m rechts | 12 × 4 | Anschluss_Mauer (-6, 0, 2; -90°)<br>Oben (-5, 3.667, 0; 180°) | 340 |
| `53_mauerabschluss_4m_links.glb` | Mauerabschluss 4 m links | 12 × 4 | Anschluss_Mauer (6, 0, 2; 90°)<br>Oben (5, 3.667, 0; -180°) | 340 |
| `54_mauerabschluss_2m_rechts.glb` | Mauerabschluss 2 m rechts | 12 × 4 | Anschluss_Mauer (-6, 0, 2; -90°)<br>Oben (-5, 1.833, 0; 180°) | 304 |
| `55_mauerabschluss_2m_links.glb` | Mauerabschluss 2 m links | 12 × 4 | Anschluss_Mauer (6, 0, 2; 90°)<br>Oben (5, 1.833, 0; -180°) | 304 |

## Zusammensetzen
1. Teile als Prefabs verwenden (das importierte .glb-Asset ist direkt instanziierbar).
2. Snapping per Anker: neues Teil so drehen, dass sein Anker entgegengesetzt zum Ziel-Anker zeigt, dann verschieben, bis beide Positionen übereinanderliegen.
   Für Bögen: Start-Anker an das offene Ende setzen, der neue offene Ende ist dann `…_Ende`.
3. Straßen und Mauern als **static** markieren, MeshCollider (nicht convex).
4. Nacht: Materialien `lamp`, `led_*`, `traffic_amber`, `pool` können Emission bekommen.
