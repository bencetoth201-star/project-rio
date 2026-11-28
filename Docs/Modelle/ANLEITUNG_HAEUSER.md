# Anleitung für Claude Code: Rio-Häuser in die Unity-Map einsetzen

Dieses Paket enthält 33 Low-Poly-Modelle (17 Häuser, Tankstelle, Bank, Kirche, Lagerhalle, 2 Hochhäuser, 3 Eckgebäude, Praça, Park, Nachtclub, Tuning-Werkstatt, Kleiderladen, Stadion, Juwelier) für das Unity-Projekt „GTA Mobile“.
Die Map ist schon gebaut und hat **Platzhalter-Objekte**, an deren Stelle die Häuser kommen sollen.

## Aufgabe
1. Platzhalter in der Szene finden und ihre Größe/Ausrichtung prüfen.
2. Die Modelle importierbar machen (glTFast) und den Ordner `Assets/RioHaeuser` ins Projekt kopieren.
3. Jeden Platzhalter durch ein passendes Haus ersetzen (Werkzeug liegt bei, siehe unten), Ergebnis prüfen.

## Inhalt
```
Assets/RioHaeuser/
  Modelle/*.glb                alle Gebäude (glTF-Binary)
  Scripts/RioHausFarben.cs     Laufzeit-Komponente: würfelt Farben der slot_-Materialien (Seed pro Objekt) – auch für Fahrzeuge
  Scripts/RioHausDaten.cs      Tabelle aller Häuser (Klasse, Grundstücksgröße) + alle Farbpaletten
  Editor/RioHausPlatzierer.cs  Editor-Fenster: Tools ▸ Rio Häuser ▸ Platzhalter ersetzen
  haeuser.json                 Metadaten: Grundstück, Dreiecke, Farb-Slots, Ankerpunkte (Position + Drehung)
Extras/OBJ/                    dieselben Modelle als .obj + .mtl (Fallback ohne glTFast, ohne Ankerpunkte)
Extras/Generator/              Python-Skripte, die alle Modelle erzeugen (nur bei Änderungen nötig)
LIESMICH.md                    ausführliche Beschreibung für Menschen
```

## Schritt 1 – glTFast installieren
In `Packages/manifest.json` unter `"dependencies"` eintragen (oder Package Manager → „Add package by name“):
```json
"com.unity.cloud.gltfast": "6.10.0"
```
(Jede aktuelle 6.x-Version passt. Bei Unity < 2021.3 stattdessen `com.atteneder.gltfast`.)

## Schritt 2 – Ordner kopieren
`Assets/RioHaeuser` komplett nach `<Projekt>/Assets/RioHaeuser` kopieren, kompilieren lassen, Konsole prüfen.
**Hinweis:** Die C#-Skripte wurden nicht in einem echten Unity-Projekt kompiliert. Fehler bitte direkt beheben (Ziel: Unity 2021.3+, C# 9).

## Schritt 3 – Konventionen der Modelle
- **Maßstab:** 1 Einheit = 1 m. Nicht skalieren.
- **Pivot:** Mitte des Grundstücks, auf dem Boden (y = 0).
- **Vorderseite (Straße/Eingang):** zeigt nach **+Z** (`transform.forward`).
- **Grundstück:** Breite (X) × Tiefe (Z) in 2-m-Schritten (Tabelle). Vorderseite bündig an der +Z-Kante.
- **Ankerpunkte:** leere Child-Objekte (z. B. `Eingang`, `Tresor`), Z zeigt nach außen bzw. zum Spieler.
  Ein Anker, der genauso hieße wie ein bewegliches Teil, heißt `<Name>_Punkt` (z. B. `Vitrine_1_Punkt`) – vorher hat so ein
  Anker das gleichnamige Teil in der GLB verschoben (Vitrinen-Hauben saßen falsch), das ist behoben.
- **Ebenen (Bank, Nachtclub, Tuning-Werkstatt, Kleiderladen, Stadion, Juwelier):** Child-Objekte `UG__…`, `EG__…`, `OG__…`, `Dach__…`, `Aussen__…` – beim Betreten obere Ebenen ausblenden.
- **Nachtclub „Clube Subsolo“ (29):** außen drei heruntergekommene Häuser, innen eine 8,5 m hohe Halle. `EG__` = Tanzfläche (LED-Kacheln),
  DJ-Bühne mit LED-Wand, Hauptbar, Eingangsbar, Go-go-Podeste, Lounge, Garderobe, Toiletten. `OG__` = Galerie auf +4,5 m mit VIP-Lounge, VIP-Bar
  und 5 Privatkabinen mit Stange (Anker `Kabine_1…5`). `Dach__` = schwarze Hallendecke + Dach mit Wassertanks (beim Betreten ausblenden).
  `Licht__beam_*` = halbtransparente Lichtkegel der Moving Heads: deaktivieren oder durch echte Unity-Spotlights ersetzen.
  Discokugel = Material `mirror` → Metallic ~0,9 und Reflection Probe in der Halle; für Lichtpunkte ein Cookie-Spotlight auf die Kugel richten.
  Eingang = unscheinbare Stahltür mit roter Lampe und Absperrkordel (Anker `Eingang`), Notausgang hinten.
- **Tuning-Werkstatt „Garagem 021“ (30):** Halle 24 × 20 m mit Satteldach + Vorhof. 3 Rolltore = eigene Nodes `Tor_1/2/3`,
  Pivot an der Oberkante der Öffnung → **zum Öffnen localScale.y von 1 auf ~0,05 animieren** (Tor rollt nach oben).
  Zonen-Anker für Autos (Z-Achse = Fahrzeugfront, Auto fährt Richtung −Z hinein): `Einfahrt` (Tuning-Menü öffnen),
  `Lackieren` (Lackierkabine → Material `slot_paint_*` des Autos), `Felgen` (Felgenwand → `slot_rim_*`/`slot_caliper_*`),
  `Hebebuehne_1` (oben, +1,7 m) / `Hebebuehne_2` (unten) – Tieferlegen/Fahrwerk, `Pruefstand` (Rollenprüfstand: Motor/Turbo/Nitro),
  `Auto_Abholen_1/2` (Vorhof). Dazu `Buero` (Kasse) und `Lounge` (OG über dem Büro). Decke der Lackierkabine = `Dach__dach_kabine`.
  Nachts leuchten Neonschild (`neon_orange`, `led_cyan`), Bildschirme (`screen`) und die Röhrenlampen (`dach_licht`).
- **Kleiderladen „Mega Moda Popular“ (31):** schmales Grundstück 8 × 16 m, billig und vollgestopft (Stil SAARA im Centro).
  Ladenfront offen (Rolltor hochgerollt), Ware steht unter der Markise bis auf den Gehweg (Puppen, Ständer, Chinelo-Kisten, Lautsprecher).
  `EG__` = Laden mit Gang von ~0,9 m, `OG__` = Lager + obere Wände, `Dach__` = Decke mit hängender Ware, Ventilator, Röhrenlampen.
  Anker: `Eingang`, `Ladeninnen` (Kauf-Menü), `Kasse`, `Provador` (Umkleide = Outfit wechseln). Ca. 30.000 Dreiecke wegen der vielen Kleidungsstücke →
  Innenraum (`EG__`, `Dach__`) erst einblenden, wenn der Spieler nahe ist; von weitem reichen `Aussen__` + `OG__`.
- **Stadion „Estádio do Rio“ (32):** Vorbild Maracanã, **Maßstab 1:1** – Grundstück 300 × 268 m, Dachring ~264 × 230 m, Höhe 41 m,
  Spielfeld 105 × 68 m (Längsachse = X, Mittelpunkt = Pivot). Unterrang (`EG__`), Oberrang + VIP-Logen (`OG__`), Membrandach mit
  Flutlichtring und 2 Videowänden (`Dach__`), Säulen, Fassade, Vorplatz, 2 Zugangsrampen auf +14 m (`Aussen__`).
  Platzhalter mit `_stadion` im Namen; das Grundstück muss mindestens 300 × 268 m groß sein. Anker: `Spielfeld_Mitte`, `Tor_West/Ost`,
  `Spielertunnel`, `Tribuene_Haupt`, `VIP_Loge`, `Eingang_Haupt/Ost/Nord/West`, `Rampe_Ost/West_Unten`.
  Hitboxen: die mitgelieferten `__kollision`-Nodes (Ränge, Rampen, Umgang, Spielfeld) – kein MeshCollider auf die Grafik.
  LOD empfohlen (von weitem nur `Aussen__` + `Dach__`).
- **Juwelier „Esmeralda Joias“ (33):** 12 × 22 m, 3 Ebenen. `EG__` Verkaufsraum mit Sicherheitsschleuse (Metalldetektor),
  Schaufenstern mit Büsten, beleuchteten Wandvitrinen, 3 Insel-Vitrinen, Verkaufstheke, VIP-Ecke, Wachposten, Personalbereich mit
  Spinden/WC/Hintertür. `OG__` EG-Decke mit Kristallleuchter, Goldschmiede-Werkstatt, Überwachungsraum (12 Monitore), Chefbüro.
  `UG__` Tresorraum mit Schließfachwand, Goldbarren, Edelsteintisch + Lager mit Video-Rekorder. Wendeltreppe UG → EG → OG (hinten rechts).
  **Für Überfälle:** eigene Nodes `Vitrine_1…6` (Glashauben, Pivot Mitte – zum Zerschlagen ausblenden/ersetzen),
  `Tuer_Aussen`/`Tuer_Innen` (Schiebetüren der Schleuse – entlang X schieben), `Tresortuer` (Pivot = Scharnier, um Y drehen).
  Anker: `Eingang`, `Schleuse`, `Wachmann`, `Theke`, `Vitrine_1…6_Punkt` (Standpunkt vor der Vitrine), `Kamera_1…4` (Blickrichtung = yaw), `Alarm`, `Alarmglocke`,
  `Tresor`, `Tresor_Tuer`, `Server_Ueberwachung`, `Ueberwachung`, `Werkstatt`, `Buero`, `Hintereingang`.
- **Casa Império (Juwelierpalast / Mega-Heist) und die Megayacht** sind jetzt im eigenen **Heist-Paket** (`Assets/RioHeists`,
  Anleitung `ANLEITUNG_CLAUDE_CODE_HEISTS.md`). Platzhalter mit `_palast`, `_imperio`, `_megabank`, `_heist` oder `_yacht` überspringt
  der Häuser-Platzierer – die setzt **Tools ▸ Rio Heists ▸ Platzhalter ersetzen**.
- **Tankstelle:** Materialien, die mit `dach` beginnen, beim Betreten ausblenden.
- **Bank-Keller** liegt bei y = −4 m → Terrain unter der Bank braucht ein Loch (Terrain Holes).
- **Farben:** Materialien `slot_<art>_<n>` färbt `RioHausFarben` um; alle anderen Materialien bleiben fest.
- **Nacht-Emission:** `glass`, `glass_open`, `glass_big`, `glass_bank`, `glass_shop`, `slot_glass_1`, `lamp`, `dach_licht`, `sign_light`, `led_*`, `club_neon`, `beam_*`, `neon_orange`, `screen`, `pool`.
- **glTFast spiegelt beim Import die X-Achse** (Rechts-/Linkshänder). Aussehen bleibt gleich; Positionen von Ankern daher immer aus Unity lesen.

## Schritt 4 – Platzhalter ersetzen
**Variante A (Werkzeug):** Platzhalter-Namen beginnen mit `Platzhalter` → *Tools ▸ Rio Häuser ▸ Platzhalter ersetzen*.
- Misst jeden Platzhalter (MeshFilter-Bounds × Scale, sonst BoxCollider, sonst Scale), setzt das größte passende Haus mittig auf die Unterkante.
- Ausrichtung: Vorderseite = `forward` des Platzhalters.
- Steuerung über den Namen: `_sehrarm`, `_arm`, `_einfach`, `_mittel`, `_gehoben`, `_reich`, `_luxus` oder ein Hausname (`_boteco`, `_villa_zona_sul` …).
- **Nur per Name** (nie zufällig): `_tankstelle`, `_bank`, `_kirche`/`_igreja`, `_lagerhalle`, `_praca`, `_park`, `_club`/`_disco`, `_tuning`/`_garagem`, `_kleider`/`_moda`/`_loja`, `_stadion`, `_juwelier`/`_joias`,
  `_centro`/`_hochhaus` (Türme), `_ecke` (Eckgebäude), `_gewerbe`, `_frei` (Praça/Park).
- **Eckgebäude:** Vorderseite +Z **und** zweite Straßenseite **+X** liegen bündig an den Grundstückskanten; die abgeschrägte Ecke mit Eingang zeigt nach (+X, +Z).
  Platzhalter so drehen, dass forward zur Hauptstraße und die rechte Seite (glTF +X; nach glTFast-Import −X!) zur Nebenstraße zeigt – im Zweifel nach dem Einsetzen visuell prüfen und um 90° drehen.
- **Hochhäuser** (Torre Centro 116 m + Antenne, Edifício 104 m): rote Flugwarnlichter `led_red` nachts leuchten lassen; für Distanz-Ansichten LOD empfohlen.
- **Praça / Park:** 36 × 36 m, Oberfläche auf Gehweghöhe (+0,15 m); Anker `Zugang_N/S/E/W` mittig an den Kanten.
- Fügt `RioHausFarben` (zufälliger Seed) hinzu, richtet die Hitboxen ein (`RioKollision`: nur die unsichtbaren `__kollision`- und
  `__trigger_<n>`-Nodes bekommen Collider) und setzt Static-Flags – bewegliche Teile (Türen, Tore, Laser, Aufzüge, Vitrinen-Hauben)
  bleiben beweglich und bekommen einen kinematischen Rigidbody. Blendet den Platzhalter aus. Rückgängig mit Strg+Z.

**Variante B (eigene Logik):** Mit `haeuser.json` / `RioHausDaten.Haeuser` Haus nach Grundstücksgröße/Klasse wählen,
`PrefabUtility.InstantiatePrefab` auf das .glb-Asset, Position = Mitte der Platzhalter-Unterkante, Rotation = Platzhalter-Rotation. Nie skalieren.

## Gebäude
| Datei | Name | Klasse | Grundstück B × T (m) | Dreiecke | Ankerpunkte |
|---|---|---|---|---|---|
| `01_barraco.glb` | Barraco | Sehr arm | 6 × 6 | 1532 | Eingang, Strom_Anschluss |
| `02_palafita.glb` | Palafita | Sehr arm | 10 × 8 | 2100 | Eingang, Strom_Anschluss |
| `03_puxadinho.glb` | Puxadinho | Arm | 6 × 8 | 1880 | Eingang, Strom_Anschluss |
| `04_favela_reihe.glb` | Favela-Reihe | Arm | 10 × 8 | 2340 | Eingang, Strom_Anschluss |
| `05_cortico.glb` | Cortiço | Arm | 10 × 10 | 3636 | Strom_Anschluss, Eingang |
| `06_casa_rio.glb` | Casa Rio | Einfach | 8 × 8 | 2088 | Eingang |
| `07_boteco.glb` | Casa do Boteco | Mittel | 8 × 10 | 2608 | Eingang |
| `08_sobrado.glb` | Sobrado | Mittel | 8 × 10 | 2070 | Eingang |
| `09_mercadinho.glb` | Mercadinho | Mittel | 10 × 10 | 2156 | Eingang |
| `10_predio.glb` | Prédio Copacabana | Mittel | 12 × 12 | 2284 | Eingang |
| `11_zona_oeste.glb` | Casa Zona Oeste | Gehoben | 16 × 14 | 3584 | Eingang |
| `12_villa_zona_sul.glb` | Villa Zona Sul | Reich | 20 × 18 | 2364 | Eingang |
| `13_casa_barra.glb` | Casa Barra | Reich | 24 × 20 | 4644 | Eingang |
| `14_cobertura_leblon.glb` | Cobertura Leblon | Reich | 18 × 18 | 2748 | Eingang |
| `15_casa_joa.glb` | Casa Modernista Joá | Reich | 32 × 20 | 2044 | Eingang |
| `16_tankstelle_257.glb` | Tankstelle 257 | Spezial | 30 × 22 | 6560 | Hintertuer, Safe_Tuer, Lager_Tuer, Safe, Kasse, Eingang, Zapfsaeule_1, Zapfsaeule_2 |
| `17_mansao_leblon.glb` | Mansão Leblon · R$ 20 Mio. | Luxus | 36 × 30 | 5044 | Eingang |
| `18_mansao_joatinga.glb` | Mansão Joatinga · R$ 50 Mio. | Luxus | 52 × 44 | 7600 | Eingang, Helipad, Gaestehaus |
| `19_banco_brasilia.glb` | Banco de Brasilia | Spezial | 28 × 24 | 14950 | Eingang, Treppe_UG, Gitter_Tuer, Tresor_Tuer, Tresor, ATM_Aussen, ATM_Innen, Schalter, Gitter_EG, Buero_Direktor |
| `20_torre_centro.glb` | Torre Centro (116 m) | Centro | 32 × 34 | 2448 | Eingang, Helipad |
| `21_edificio_70er.glb` | Edifício Anos 70 (104 m) | Centro | 40 × 22 | 1548 | Eingang |
| `22_igreja.glb` | Igreja (Kirche) | Spezial | 24 × 36 | 2920 | Eingang, Glockenturm |
| `23_lagerhalle.glb` | Lagerhalle (Hafen) | Gewerbe | 42 × 34 | 4052 | Eingang, Laderampe |
| `24_eck_padaria.glb` | Eck-Padaria | Ecke | 16 × 16 | 1176 | Eingang |
| `25_eck_sobrado.glb` | Eck-Sobrado | Ecke | 14 × 14 | 1912 | Eingang |
| `26_eck_predio.glb` | Eck-Prédio | Ecke | 18 × 18 | 1104 | Eingang |
| `27_praca.glb` | Praça mit Coreto | Freifläche | 36 × 36 | 3478 | Zugang_N, Zugang_S, Zugang_E, Zugang_W |
| `28_parque.glb` | Park mit Bolzplatz | Freifläche | 36 × 36 | 2988 | Zugang_N, Zugang_S, Zugang_E, Zugang_W |
| `29_clube_subsolo.glb` | Clube Subsolo (Nachtclub) | Spezial | 26 × 36 | 23188 | Strom_Anschluss, Kabine_1, Kabine_2, Kabine_3, Kabine_4, Kabine_5, Eingang, Tanzflaeche, DJ, Bar_Haupt, Bar_Eingang, VIP_Lounge, Notausgang |
| `30_garagem_021.glb` | Garagem 021 (Tuning) | Spezial | 26 × 30 | 19400 | Strom_Anschluss, Lackieren, Felgen, Einfahrt, Pruefstand, Hebebuehne_1, Hebebuehne_2, Buero, Lounge, Eingang, Auto_Abholen_1, Auto_Abholen_2 |
| `31_mega_moda.glb` | Mega Moda Popular (Kleiderladen) | Spezial | 8 × 16 | 29688 | Strom_Anschluss, Kasse, Provador, Ladeninnen, Eingang |
| `32_estadio_do_rio.glb` | Estádio do Rio (Stadion) | Spezial | 300 × 268 | 28530 | Spielfeld_Mitte, Tor_West, Tor_Ost, Spielertunnel, Tribuene_Haupt, VIP_Loge, Eingang_Haupt, Eingang_Ost, Eingang_Nord, Eingang_West, Rampe_Ost_Unten, Rampe_West_Unten |
| `33_esmeralda_joias.glb` | Esmeralda Joias (Juwelier) | Spezial | 12 × 24 | 26588 | Strom_Anschluss, Hintereingang, Eingang, Schleuse, Vitrine_1_Punkt, Vitrine_2_Punkt, Vitrine_3_Punkt, Vitrine_4_Punkt, Vitrine_5_Punkt, Vitrine_6_Punkt, Theke, Wachmann, Kamera_1, Kamera_2, Kamera_3, Kamera_4, Alarm, Tresor, Tresor_Tuer, Server_Ueberwachung, Alarmglocke, Werkstatt, Ueberwachung, Buero |

## Performance (Mobile)
- Häuser 1.100–4.700 Dreiecke; Tankstelle ~6.600; Luxus-Villen 5.000–7.600; Bank ~15.000; Nachtclub ~23.000, Tuning-Werkstatt ~19.400, Kleiderladen ~30.000, Stadion ~28.500, Juwelier ~26.600 (Innenräume nur rendern, wenn der Spieler drin ist); Hochhäuser 1.500–2.500; Praça/Park ~3.000–3.500.
- Static Batching nutzen. `RioHausFarben` erzeugt zur Laufzeit geteilte Material-Varianten (SRP-Batcher bleibt wirksam).
- Hitboxen sind vereinfacht (zusammen ~50 % der Grafik-Dreiecke; Stadion ~23.000, Kleiderladen ~18.000, normale Häuser 700–4.500),
  Kleinkram, Teppiche, dünne Stäbe und Lichtkegel sind nicht fest.
- **Bank (19):** Tresortür `UG_Tresortuer` (Scharnier vorne links, um Y drehen) und Gittertür `UG_Gittertor` sind bewegliche Teile.
- **Hitboxen:** jedes Modell bringt unsichtbare `…__kollision`-Nodes (vereinfachte feste Hitbox) und `…__trigger_<n>`-Nodes
  (Laser, Pool, Wasser) mit. In Unity **Tools ▸ Rio ▸ Hitboxen einrichten** ausführen (Skript `RioKollision`, Details in der
  Haupt-Anleitung) – keine eigenen MeshCollider auf die Grafik-Meshes legen.
