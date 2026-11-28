# Anleitung für Claude Code: Rio-Heists (Spezial-Paket)

Die großen Heist-Schauplätze für „GTA Mobile“ – sie sind **aus dem Häuser- und dem Hafen-Paket herausgenommen** und liegen jetzt nur hier:
- **`01_casa_imperio.glb` – Casa Império** („Megabank“, Mega-Heist-Palast im Staatsbank-/Münzanstalt-Stil), 219.500 Dreiecke, 8 Ebenen.
- **`03_casa_imperio_abgeriegelt.glb` – Casa Império abgeriegelt** (wie in der Heist-Serie: Rollläden vor allen Fenstern, Panzertüren,
  Gitter, Sandsäcke, Panzersperren, Strom aus + rotes Notlicht), 235.344 Dreiecke – gleicher Pivot wie 01, zum Austauschen.
- **`02_yacht_soberana.glb` – Megayacht SOBERANA** (150 m, Schleich-Heist nach eurer Skizze), 108.970 Dreiecke, 7 Decks.
- **Maya-Kanalisation** (`Kanalisation/10…25_kanal_*.glb`): Baukasten wie die Straßenteile – 15 Teile + fertiges Beispielnetz,
  das genau an die Maya-Wand im UG3 des Palasts passt (Fluchtweg mit Boot, Boot-Versteck mit Lift + Garage an der Straße).

Habt ihr die alten Versionen schon im Projekt (`RioHaeuser/Modelle/34_casa_imperio.glb`, `RioHafen/Modelle/18_yacht_soberana.glb`),
diese löschen und durch die Dateien aus diesem Paket ersetzen (neue Namen `01_…`/`02_…`).

## Inhalt
```
Assets/RioHeists/Modelle/*.glb            Casa Império (01), Yacht (02), Casa Império abgeriegelt (03) – glTF binär, Anker, bewegliche Teile, Hitboxen
Assets/RioHeists/Scripts/RioBeute.cs      Komponente für stehlbare Teile (Art, Wert, Greifbereich, Event „Gestohlen“, Ausrüstung)
Assets/RioHeists/Scripts/RioHeist.cs      RioHeist.Einrichten(modell): Beute-Komponenten anhängen, Werte-Tabelle, Anker-Suche
Assets/RioHeists/Scripts/RioAbriegelung.cs  Tausch normal ↔ abgeriegelt + Rollläden/Panzertüren/Gitter fahren
Assets/RioHeists/Editor/RioHeistPlatzierer.cs   Tools ▸ Rio Heists ▸ Platzhalter ersetzen / Abriegelung vorbereiten
Assets/RioHeists/Kanalisation/*.glb       Maya-Kanal-Baukasten (16 Modelle)
Assets/RioHeists/Kanalisation/Scripts/    RioKanal (Andocken/Stapeln), RioGeheimtor, RioBootslift
Assets/RioHeists/Kanalisation/Editor/RioKanalWerkzeug.cs  Tools ▸ Rio Heists ▸ Kanal an Palast andocken / Zwei Anker verbinden / Kanal-Teile einrichten
Assets/RioHeists/heists.json              Maße, Dreiecke, alle Anker (x, y, z, yaw), bewegliche Teile, Beute-Liste
Assets/RioGemeinsam/                      RioKollision (Hitboxen) – identisch mit den anderen Rio-Paketen
Extras/OBJ/  Extras/Generator/            .obj/.mtl und die Python-Generatoren (palacio.py, yacht.py, kanal.py)
```
**Hinweis:** Die C#-Skripte wurden nicht in einem echten Unity-Projekt kompiliert. Fehler bitte direkt beheben (Ziel: Unity 2021.3+).

## Einsetzen
1. glTFast installieren (`com.unity.cloud.gltfast`), `Assets/RioHeists` + `Assets/RioGemeinsam` ins Projekt kopieren.
2. Platzhalter in der Szene benennen: `Platzhalter_palast` (oder `_imperio`, `_megabank`, `_heist`) → Casa Império,
   `Platzhalter_yacht` (oder `_soberana`) → Yacht, `Platzhalter_palast_abgeriegelt` → nur die abgeriegelte Version.
   **Tools ▸ Rio Heists ▸ Platzhalter ersetzen**: setzt das Modell auf die Mitte der
   Platzhalter-Unterkante (Yacht: Unterkante = Wasserlinie), dreht wie der Platzhalter, richtet Hitboxen, Static-Flags und
   Beute-Komponenten ein. Der Häuser-Platzierer überspringt diese Platzhalter jetzt.
3. Für den Heist mit Abriegelung: Palast auswählen → **Tools ▸ Rio Heists ▸ Abriegelung vorbereiten** (siehe „Abriegelung“).
4. Maya-Kanalisation: Teile aus `Assets/RioHeists/Kanalisation` in die Szene ziehen, mit **Kanal an Palast andocken** /
   **Zwei Anker verbinden** zusammensetzen, dann **Kanal-Teile einrichten (Auswahl)** (siehe „Maya-Kanalisation“).
5. Zur Laufzeit gespawnt: `RioKollision.Einrichten(instanz); RioHeist.Einrichten(instanz);` (Read/Write der Meshes im glTFast-Importer an).
- 1 Einheit = 1 m, Y oben. **glTFast spiegelt X beim Import** → Ankerpositionen immer aus den Child-Transforms lesen, Drehrichtungen
  von Klappen/Türen im Editor prüfen.
- Node-Namen: `<Ebene>__<Material>` = Grafik, `<Ebene>_Innen__…` = Einrichtung, `…__kollision` / `…__trigger_<n>` = Hitboxen,
  Nodes ohne `__` mit Kindern = bewegliche Teile (Pivot am Scharnier), leere Nodes = Anker (yaw = Blickrichtung, lokale +Z).
- Anker, die genauso heißen würden wie ein bewegliches Teil, heißen `<Name>_Punkt` (z. B. `Hauptdeck_Beute_Bild_1_Punkt` = Standpunkt
  vor dem Bild). Vorher hat so ein Anker das Teil in der GLB verschoben (Bilder, Skulpturen, Vitrinenglas saßen falsch) – behoben.

## Beute (alle Modelle)
- Jeder Node mit **`_Beute_`** im Namen ist ein stehlbares Teil: `<Ebene>_Beute_<Art>_<n>`. Er hat **keine feste Hitbox**, sondern einen
  Greifbereich `…__trigger_1` (Box 25 cm größer als das Teil). Dazu zählen auch `…_Tresor_Schluessel` und `…_Tauchausruestung_<n>`.
- `RioHeist.Einrichten(modell)` hängt `RioBeute` an (Art aus dem Namen, Wert aus `RioHeist.Werte`, `Gross` = zwei Hände/langsam).
  Spieler-Skript: `if (Input.GetKeyDown(KeyCode.E) && RioBeute.Naechste(transform.position) is RioBeute b) b.Stehlen();`
  Geld gutschreiben über `RioBeute.Gestohlen += b => geld += b.Wert;`. Der Spieler braucht das Tag `Player`.
- Werte (R$, fiktiv, frei änderbar): Safira 25 Mio. · Edelstein („Estrela do Rio“, „Coração do Brasil“) 15 Mio. · Krone 8 Mio. ·
  Gold-Palette 2,5 Mio. · Jademaske (Maya-Schatz in der Kanal-Halle) 1,5 Mio. · Kunst-Paket 1,2 Mio. · Bild 800.000 · Bargeld 500.000 ·
  Skulptur 400.000 · Schmuck 300.000 · Uhr 120.000 · Tasche 40.000 · Wein 30.000 · Kerzenleuchter 20.000 · Laptop 15.000 ·
  Champagner 8.000 · Zigarren 6.000 · Handy 5.000.
- **Ausrüstung** (`…_Ausruestung_<Art>_<n>`, nur in der Versteck-Garage): Bauplan, Nachtsicht ×2, Bombe ×2. Gleiche Technik wie Beute
  (Greifbereich, `RioBeute`), aber `Ausruestung == true` und Wert 0 → ins Inventar statt Geld gutschreiben.

## Megayacht SOBERANA – Ablauf des Heists (nach der Skizze)
Grundregel: **Schleich-Mission.** Niemand darf euch sehen. Wer gesehen wird, löst Alarm aus: Security überall, alle Wachen jagen euch,
Heckklappe, Garagen- und Jetski-Tor gehen zu, die Tauchschleuse wird gesperrt → Flucht unmöglich = Mission gescheitert.

1. **Einstieg (eine von zwei Varianten):**
   - **Kleines Boot ans Unterdeck:** Schlauchboot `Heist_Schlauchboot` (liegt am Heck, abkoppeln und als Boot nutzen) → `Boot_Einstieg_Heck`
     → Heckklappe `Unterdeck_Heckklappe` (90° nach außen = Badeplattform, `Boot_Ausstieg_Badeplattform`) → Beach Club. Wache `Wache_Beachclub`.
   - **Jetski:** von der offenen Wasserseite (Steuerbord, +Z) zur neuen **Jetski-Seitengarage** (Unterdeck, x 32–38): `Jetski_Einstieg_Wasser`
     → Klappe `Unterdeck_Jetski_Tor` (Scharnier unten, um 90° nach außen klappen = Plattform auf 1,2 m, `Jetski_Einstieg_Plattform`)
     → `Jetski_Garage` (2 Jetskis `Unterdeck_Jetski_3/4`, Torschalter `Jetski_Tor_Schalter`). `Wache_Jetski` steht mit dem Rücken zur Garage.
2. **Hochkämpfen – die Treppen sind kaputt:** Glaslift und Wendeltreppe sind außer Betrieb (Schild „FORA DE SERVIÇO“, Absperrband),
   mehrere Treppen sind eingestürzt (Lücke, Trümmer, Absperrband). Der einzige Weg nach oben ist ein Zickzack Deck für Deck:
   - Unterdeck → Hauptdeck: **Beach-Club-Treppen** (Heck, innen)
   - Hauptdeck nach vorne → Oberdeck: **Crew-Treppe** (`Crew_Treppe_Hauptdeck`, Tür vom Gästeflur)
   - Oberdeck nach achtern → Eignerdeck: **Außentreppen achtern** (BB + SB). Innen sperrt das Kino den Weg → über die Seitentüren
     (x 0) und das Seitendeck nach hinten.
   - Eignerdeck nach vorne → Brückendeck: **Crew-Treppe** (Tür aus der Eignersuite)
   - eingestürzt: Wendeltreppe Unterdeck → Brückendeck, Crew-Treppe Unter → Haupt und Ober → Eigner, Außentreppen Haupt → Ober und
     Eigner → Brücke. Brückendeck → Sonnendeck ist intakt. **Runter** geht es schneller: durch das offene Treppenauge springen (Fallschaden).
3. **Lüftungsschächte (Wachen ausschalten):** Auf Haupt-, Ober-, Eigner- und Brückendeck läuft an Steuerbord unter der Decke ein
   begehbarer Schacht (1 m breit, 0,9 m hoch → Kriech-Modus, Kapselhöhe ≤ 0,8 m) über die ganze Länge; die Trennwände enden darunter.
   Einstieg außen am Seitendeck: Leiter + Gitter `<Deck>_Lueftung_Einstieg` (Scharnier oben, hochklappen), Anker `Lueftung_<Deck>_Einstieg`.
   Pro Deck 5 Klappen `<Deck>_Lueftungsklappe_1…5` (Scharnier an der Hinterkante, nach unten klappen) genau über einer Wache
   (`Wache_<Deck>_1…5`, Bodenpunkt `Lueftung_<Deck>_Klappe_<n>`) → von oben ausschalten, Leiche ziehen, weiterkriechen.
4. **Kapitän töten, Schlüssel holen (ganz oben):** Der Kapitän sitzt am Steuerstand der Brücke (`Kapitaen`, Brückendeck, Wache `Wache_Bruecke`).
   Der Tresorschlüssel `Brueckendeck_Tresor_Schluessel` liegt im Modell auf dem Beistelltisch neben seinem Sessel – im Spiel an den Kapitän
   hängen und beim Tod fallen lassen.
5. **Schlüssel genommen → sofort Polizei:** Countdown starten. Polizeiboote `Polizei_Boot_1…4`, Hubschrauber `Polizei_Heli`, Streifen am Kai
   `Polizei_Kai_1…3` (Kaihöhe y 3), Polizei stürmt über `Polizei_Zugang_Gangway` und `Polizei_Zugang_Heck` an Bord und arbeitet sich nach oben.
6. **Schnell runter:** Crew-Treppe Brücke → Eignerdeck → Eignerbüro, Geheimregal `Eignerdeck_Regal` zur Seite schieben → Privataufzug
   `Tankdeck_Privataufzug` (in Y von 12,2 auf −2,6 fahren) → direkt in den Tresor-Vorraum (Wache `Wache_Tresor`). Der Schlüssel öffnet Regal-Lift und Tresor.
7. **Motor zerstören, bevor der Tresor aufgeht:** Maschinenraum (Tankdeck achtern, `Wache_Maschinenraum`): beide Einspritzpumpen
   `Tankdeck_Motor_BB_Einspritzpumpe` und `Tankdeck_Motor_SB_Einspritzpumpe` zerstören (Stehpunkte `Sabotage_Motor_BB/SB`, Rauch/Feuer bei
   `Motor_Rauch_BB/SB`). Erst dann fällt der Bordstrom aus → Zeitschloss (`Zeitschloss`) und Laser (`Tankdeck_Laser`) aus, die Rundtür
   `Tankdeck_Tresortuer` geht mit dem Schlüssel auf – und die Yacht kann nicht mehr ablegen.
8. **Klauen:** im Tresor 6 Goldpaletten `Tankdeck_Beute_Gold_1…6`, 2 Bargeldstapel, 3 Kunst-Pakete, der Saphir `Tankdeck_Beute_Safira`
   (Glashaube `Tankdeck_Safira_Glas`); dazu Beute auf jedem Deck (Tabelle unten).
9. **Flucht mit Taucherausrüstung:** Tauchschleuse an Steuerbord (Tankdeck, x 11–17, Schild „MERGULHO“). Weg: Tresor → Vorraum →
   an Steuerbord am Tresor vorbei → Tür im Schott x 10,5 → Weinkeller → wasserdichte Tür `Tankdeck_Tauchschleuse_Tuer` (Scharnier bei x 15, um Y).
   Innen 4 Tauchausrüstungen `Tankdeck_Tauchausruestung_1…4` (aufnehmen = anziehen), Pult `Tauchschleuse_Fluten`: Tür zu,
   Wasser `Tankdeck_Tauchschleuse_Wasser` in Y ~3,3 m hochfahren (Trigger = Tauch-Modus), Außenluke `Tankdeck_Tauchluke`
   (unter der Wasserlinie, Scharnier oben, nach außen klappen) → raus ins offene Hafenwasser (`Tauchschleuse_Ausstieg`,
   Richtung `Flucht_Taucher_Richtung`, dort z. B. ein Fluchtboot spawnen). Unter Wasser sehen die Polizeiboote euch nicht.
10. **Nebenmission „Reiche am Pool“:** Am Infinity-Pool (Hauptdeck achtern) liegen reiche Gäste auf den Liegen (`Pool_Gast_1…6`) und im Pool
    (`Pool_Gast_7/8`). Am Punkt `Pool_Bedrohen` Waffe ziehen → Gäste ergeben sich und rücken ihre Wertsachen raus: 6 Teile auf den
    Beistelltischen (`Hauptdeck_Beute_*_Pool1`, `RioBeute.Pool == true`). Das Pool-Deck ist offen (Wache `Wache_Achterdeck`, Kameras).

### Wachen, Patrouillen, Verstecke, Kameras (Anker)
- **Wachen** `Wache_*` (33 Posten, yaw = Blickrichtung), davon je 5 unter den Lüftungsklappen.
- **Patrouillen** `Patrouille_<Deck>_<Route>_<n>` (64 Punkte): Punkte einer Route der Reihe nach ablaufen, nach dem letzten zurück
  zum ersten (Ring). yaw zeigt zum nächsten Punkt.
- **Verstecke** `Versteck_*` (18 Stück, geduckt): hinter Bars, Tendern, Flügel, Kinosesseln, Vitrinen, Hubschrauber usw.
- **Kameras** `Kamera_<Deck>_<n>` (35 Stück, yaw = Blickrichtung) – Sichtkegel wie Wachen; ausschalten über `Alarm_Hauptschalter`
  bzw. die Sicherheitszentrale (Brückendeck).
- Sichtprüfung: Raycast von Wache/Kamera zum Spieler gegen die `__kollision`-Collider (Glas zählt als durchsichtig → Glas-Ebene ignorieren).

### Beute an Bord (86 Teile + Schlüssel + 4 Tauchausrüstungen)
| Deck | Beute | Teile |
|---|---|---|
| Tankdeck | Bargeld × 2, Gold × 6, Handy × 1, Kunst × 3, Safira × 1, Tauchausrüstung × 4, Wein × 2 | 19 |
| Unterdeck | Bargeld × 1, Champagner × 1, Handy × 2, Laptop × 1, Schmuck × 1, Uhr × 2 | 8 |
| Hauptdeck | Bargeld × 1, Bild × 3, Champagner × 2, Handy × 1, Kerzenleuchter × 2, Schmuck × 2, Skulptur × 2, Tasche × 2, Uhr × 2, Zigarren × 1 | 18 |
| Oberdeck | Bild × 3, Champagner × 2, Handy × 1, Kerzenleuchter × 1, Laptop × 1, Schmuck × 1, Tasche × 1, Uhr × 2, Zigarren × 1 | 13 |
| Eignerdeck | Bargeld × 1, Bild × 11, Champagner × 1, Laptop × 1, Schmuck × 5, Skulptur × 3, Tasche × 1, Uhr × 1 | 24 |
| Brueckendeck | Bargeld × 1, Laptop × 1, Schlüssel × 1, Uhr × 1, Zigarren × 1 | 5 |
| Sonnendeck | Champagner × 1, Handy × 1, Tasche × 1, Uhr × 1 | 4 |

### Technik der Yacht
- **Platzierung:** Pivot Mitte auf der Wasserlinie, Bug +X (glTF), Backbord −Z = Kaiseite. Am Hafen: Anker `Liegeplatz` einer Kaikante
  (Rio-Hafen-Paket) nehmen und die Yacht um −8 m in Z zum Kai schieben; die ausgeklappte `Hauptdeck_Passerelle` führt dann von
  `Passerelle_Oben` (Deck, y 5) auf den Kai (`Passerelle_Unten`, y 3). Die offene Wasserseite (Jetski-Garage, Tauchschleuse) ist Steuerbord (+Z).
- **Decks (Boden y):** `Tankdeck` −2,6 · `Unterdeck` 1,2 · `Hauptdeck` 5,0 · `Oberdeck` 8,6 · `Eignerdeck` 12,2 · `Brueckendeck` 15,8 ·
  `Sonnendeck` 19,4 · `Dach` 23,0. Struktur in `<Deck>__…`, Einrichtung in `<Deck>_Innen__…`, Rumpf in `Aussen__` (unter Wasser) und `Rumpf__`.
- **Räume:** Tankdeck: Maschinenraum mit Sabotage-Pumpen, Kontrollraum, Tresor (Gitter, Laser-Gang, Rundtür), Weinkeller, Tauchschleuse.
  Unterdeck: Beach Club, Tendergarage (2 Tender, U-Boot, 2 Jetskis), Crew-Messe, Crew-Kabinen, Spa, Security-Quartier mit Waffenkammer,
  Jetski-Seitengarage. Hauptdeck: Infinity-Pool, Salon, Speisesaal, Lobby, Gästesuiten, Kombüse, Vordeck. Oberdeck: Dinner-Deck, Sky Lounge,
  Kino, VIP-Suiten, Hubschrauberdeck vorne. Eignerdeck: Terrasse, Kunstgalerie, Eignerbüro mit Geheimregal, Ankleide mit 5 Vitrinen, Eignersuite.
  Brückendeck: Heli-Deck mit Hubschrauber, Panorama-Lounge, Sicherheitszentrale, Kapitänskabine, Funkraum, Brücke. Sonnendeck: Spa-Pool, Fitness, Bar.
- **Bewegliche Teile:** `Unterdeck_Jetski_Tor`, `Unterdeck_Heckklappe`, `Unterdeck_Garagentor_BB/SB`, `<Deck>_Lueftung_Einstieg`,
  `<Deck>_Lueftungsklappe_1…5`, `Tankdeck_Tauchluke`, `Tankdeck_Tauchschleuse_Tuer`, `Tankdeck_Tauchschleuse_Wasser`, `Tankdeck_Gitter`,
  `Tankdeck_Laser`, `Tankdeck_Tresortuer`, `Tankdeck_Safira_Glas`, `Tankdeck_Privataufzug`, `Tankdeck_Motor_BB/SB_Einspritzpumpe`,
  `Eignerdeck_Regal`, `Eignerdeck_Vitrine_1…5` (Glashauben), `Brueckendeck_Sicherheitstuer`, `Brueckendeck_Heli`, Tender, U-Boot, Jetskis,
  `Heist_Schlauchboot`, `Hauptdeck_Passerelle`, Relingstore, Seitentüren – vollständige Liste in `heists.json` → `beweglich`.
- **Hitboxen:** pro Deck `…__kollision` (Lüftungsschacht innen begehbar, eingestürzte Treppen haben eine echte Lücke); Absperrband unter 3 m
  ist nicht fest. Trigger: Laser, Pools, Flutwasser der Schleuse, Greifbereiche der Beute. **Tools ▸ Rio ▸ Hitboxen einrichten**
  (der Heist-Platzierer macht das).
- **Leistung:** 108.970 Dreiecke, Hitboxen 77.836. Von außen reichen `Aussen__`, `Rumpf__`, Deck-Strukturen und `Dach__`;
  `*_Innen` und die Beute erst aktivieren, wenn der Spieler an Bord ist. Rumpffarbe = Slot `slot_hull_1` (würfelbar).
- **Nachts leuchten:** `lamp`, `led_red/green`, `glass`, `screen`, `pool`, `uw_light`, `dach_licht`.

## Casa Império („Megabank“)
- Pivot = Mitte des Grundstücks auf dem Boden, Vorderseite +Z, Grundstück 74 × 116 m. Untergeschosse bis −21 m →
  Terrain darunter braucht ein Loch (Terrain Holes) bzw. das Modell auf eine Fläche ohne Terrain setzen.
- Ebenen beim Betreten von oben ausblenden (`Dach__`, `Portikus__`, `OG3__` … `UG3__`). Hitboxen 112.704 Dreiecke.

**Casa Império – der Mega-Heist (Staatsbank-/Münzanstalt-Stil):** Gebäude 64 × 56 m, Kuppel bis ~49 m (Fahne ~56 m),
  3 Untergeschosse bis −21 m; Grundstück 74 × 116 m mit Vorplatz (Springbrunnen, Fahnen, Laternen, Palmen, Polizeigitter), Eisenzaun mit
  Haupttor + 2 Wachhäusern und Lkw-Hof mit Rampe und Schiebetor hinten. 8 Ebenen, Nodes von oben nach unten:
  `Dach__` (y 27: Kuppel mit Buntglasdecke + Riesen-Kronleuchter über dem Atrium, 3 Wachtürme mit Suchscheinwerfern, Helikopterlandeplatz,
  2 Glas-Tonnendächer, Dachtür, Dachluke, Lüftungsschacht), `Portikus__` (12 Kolossalsäulen, Giebel mit Uhr, Statuen und Bannern),
  `OG3__` (y 21: Sicherheitszentrale mit 48 Monitoren + Alarm-Hauptschalter, Direktorsuite mit Geheimsafe hinter Gemälde, Kaserne,
  Waffenkammer, Großraumbüro), `OG2__` (y 15,5: Sitzungssaal, Edelsteinschleiferei, Goldschmiede, Kantine, Arbeitstresor),
  `OG1__` (y 9,5: Sala Imperial mit Krone, Thron und 10 Regalia-Vitrinen hinter einem gläsernen Laser-Tunnel, Uhrenmuseum mit
  astronomischer Uhr, Auktionssaal, Archiv), `EG__` (y 1,65 – Hochparterre: Grand Salon unter Atrium und Kuppel mit „Estrela do Rio“ auf
  Sockel + Druckplattenring, Doppeltreppe, 5 Sicherheitsschleusen, Diamant- und Haute-Joaillerie-Salon mit Bar und Flügel, Uhren- und
  Braut-Boutique, Tesouraria, Goldannahme, Lkw-Aufzug), `UG1__` (y −4,5: Banknotendruckerei mit 3 Druckstraßen + Bargeldpaletten,
  Panzergarage für 4 Geldtransporter, Wertsack-Lager, Serverraum, Stromzentrale, Lüftungszentrale, Checkpoint), `UG2__` (y −10,5:
  Goldschmelze mit 3 Öfen, Kran und Gusslinie, Münzprägerei, Raffinerie-Labor, Kistenlager, Tresor-Vorraum mit Laser-Schleuse,
  Pumpenraum der Flutanlage), `UG3__` (y −20,5: „Câmara de Ouro“ – Goldtresor 44 × 34 m, 9,5 m hoch, 108 Goldstapel, Regale,
  Bargeldkäfige, Diamantkammer mit „Coração do Brasil“, 6-m-Rundtür mit Zeitschloss, Flutkanäle mit 4 Schiebern, Ring-Gang um den
  Tresor, Seitentür im Tresoraufzug-Tunnel, **Sprengstelle Tresorwand** (Ostseite) und **alte Maya-Wand** in der Außenwand = Durchbruch
  in die Maya-Kanalisation). Einrichtung liegt jeweils in `<Ebene>_Innen__…`.
  **Neu nach eurer Skizze:** EG: Technikraum hinter der Tesouraria (Tür `EG_Technik_Tuer`) mit Strombox (Hebel `EG_Strom_Hebel`) und
  Sicherheitsbox (Klappe `EG_Abriegelung_Klappe` über dem roten Knopf), Waffenkontrolle (Torbogen-Detektor + Röntgenband) am Lkw-Aufzug,
  Wachen an Tesouraria und Goldannahme. OG3: Kaserne/Waffenkammer → **Lager** (Hochregale, Kisten, Hubwagen, Tür `OG3_Lager_Tuer`).
  UG3: Tür vom Tresoraufzug-Tunnel in den Ring-Gang (`UG3_Tunnel_Tuer`, Kartenleser), Laser im Tunnel erst hinter dieser Tür.
  **Streaming:** ~220.000 Dreiecke gesamt, pro Ebene ~10.000–47.000. Von außen reichen `Aussen__`, `Portikus__`, `Dach__` und die
  Fassaden-Teile der Ebenen; `*_Innen` und die Untergeschosse erst aktivieren, wenn der Spieler im Gebäude ist.
  **Heist-Nodes (eigene Pivots):** `EG_Diamant` + `EG_Diamant_Glas`, `EG_Vitrine_1…18`, `EG_Schleuse_<1…5><A|B>` (Glastüren, in X
  schieben), `EG_Portal_<1…3><L|R>` (Bronzetüren, um Y drehen), `EG_Fahrzeugaufzug` (Y zwischen EG und UG1), `OG1_Sala_Tuer`,
  `OG1_Sala_Seitentuer_W/O` (mit Kartenleser), `OG1_Laser`, `OG1_Krone` + `OG1_Krone_Glas`, `OG1_Vitrine_1…25`, `OG2_Arbeitstresor_Tuer`,
  `OG3_Gemaelde` (aufklappen → Safe), `OG3_Waffenkammer_Tuer`, `UG1_Gitter`, `UG2_Gitter_1/2` + `UG2_Laser` (Schleuse),
  `UG2_Tresoraufzug` (Y zwischen UG2 und UG3), `UG2_Schott` (Wartungstreppe), `UG3_Tunnel_Laser`, `UG3_Tunnel_Tuer`, `UG3_Tresorwand_Sprengstelle`
  (2-m-Pfropfen in der Tresorwand – nach der Sprengung ausblenden), `UG3_Kanalwand` (Maya-Steine – nach der Sprengung ausblenden),
  `EG_Technik_Tuer`, `EG_Strom_Hebel`, `EG_Abriegelung_Klappe`, `OG3_Lager_Tuer`, `UG3_Tresortuer` (Scharnier links,
  um Y drehen), `UG3_Glastuer` + `UG3_Laser` + `UG3_Edelstein`, `UG3_Flutschieber_1…4` (in Y hochfahren = Wasser marsch),
  `UG3_Flutwasser` (liegt unsichtbar im Boden – zum Fluten in Y bis ~4 m hochfahren), `Tor_Haupt_L/R`, `Tor_Hof` (Schiebetor),
  `Tor_Logistik` (Rolltor, Y), `Tor_Hintertuer`, `Dach_Tuer`.
  **Einstiegswege für Missionen:** Haupttor + Schleusen, Lkw-Hof → Rolltor → Fahrzeugaufzug, Hintertür, Helikopter/Dachtür/Dachluke,
  Lüftungsschacht (Dach → UG1), Wartungstreppe (UG2 → UG3), Tunnel-Seitentür (UG3), Tresorwand sprengen (Ring-Gang Ost → Tresor),
  Maya-Wand sprengen (Ring-Gang Ost → Maya-Kanalisation → Boot).
  Anker: 52 Kameras (`Kamera_<Ebene>_<n>`, yaw = Blickrichtung), `Druckplatten`, `Laser_Start/Ende/Steuerung`, `Kartenleser_Sala_W/O`,
  `Thron`, `Zeitschloss`, `Tresor_Tuer`, `Flutsteuerung`, `Server`, `Hauptstrom`, `Strom_Trafo`, `Sicherheitszentrale`,
  `Alarm_Hauptschalter`, `Direktor_Safe`, `Waffenkammer`, `Notendruckerei`, `Schmelzoefen`, `Praegerei`, `Geldtransporter_*`,
  `Hubschrauber`, `Wachturm_*`, `Polizei_Linie`, `Treppenhaus_<W|O>_<Ebene>`, `Aufzug_<L|R>_<Ebene>`, neu: `Strombox`, `Sicherheitsbox`,
  `Technikraum`, `Waffenkontrolle`, `Lagereingang`, `Lkw_Lieferung`, `Lager`, `Lager_Depot`, `Tunnel_Tuer`, `Sprengladung_Tresor`,
  `Sprengladung_Kanal`, `Kanal_Anschluss`, `Wache_*` (28 Posten auf allen Ebenen, yaw = Blickrichtung).

### Casa Império – Ablauf des Heists (euer Script, mind. 2 Spieler)
Fahrzeuge (Helikopter, Lkw, Boot, Fluchtauto) sind nicht im Modell – es gibt nur Anker, an denen ihr eure Fahrzeuge spawnt/parkt.
**Vorbereitung (kaufen):** Bauplan, Helikopter, Boot, Nachtsicht, 2 große Bomben, Boot-Versteck, Fluchtauto, Lkw.
- Boot-Versteck = Teil `18_kanal_bootsversteck` (unter der Erde) + Liftschächte + `21_kanal_versteck_garage` (getarnte Werkstatt „OFICINA“
  an der Straße). Dort liegen Bauplan (`Garage_Ausruestung_Bauplan`), 2 Nachtsichtgeräte (`…_Nachtsicht_1/2`) und 2 Bomben (`…_Bombe_1/2`),
  Stehpunkt `Ausruestung_Tisch`. Kaufen = Teile aktivieren, Aufnehmen = `RioBeute.Stehlen()` (Ausrüstung).
- **Boot in der Kanalisation parken:** Boot in der Garage auf den Bootslift → `RioBootslift.Runter()` → durchs Geheimtor raus → durch das Netz
  zum Teil `17_kanal_anschluss_palast` → Anker `Boot_Parkplatz` (neben dem Gang zum Palast, Poller + Fender, Einstieg `Kanal_Einstieg_Boot`).
- **Fluchtauto parken:** in der Garage, Anker `Fluchtauto` (Schnauze zum Rolltor `Garage_Tor`).

**Eindringen**
- **P1 – Helikopter aufs Dach** mit den Waffen und dem Sprengstoff: Anker `Hubschrauber` (Landeplatz), rein über `Dachtuer`/`Dachluke`
  (Wachen `Wache_Dach_1/2`, Kameras `Kamera_Dach_*`).
- **P2 – als Lkw-Lieferant (Stealth):** Lkw-Spawn `Lkw_Lieferung` → Rampe → Rolltor `Tor_Logistik` = `Lagereingang` → Lkw-Aufzug
  (`EG_Fahrzeugaufzug`, `Fahrzeugaufzug_EG`) → **Waffenkontrolle** (`Waffenkontrolle`: Torbogen-Detektor + Röntgenband, Wachen
  `Wache_Lieferung_1/2`) – mit Waffe = Alarm, also unbewaffnet rein. Durch die Tesouraria (`Wache_Tesouraria`) zur Tür `EG_Technik_Tuer`
  → Technikraum (`Technikraum`): an der **Strombox** (`Strombox`) Hebel `EG_Strom_Hebel` runter = Strom aus, an der **Sicherheitsbox**
  (`Sicherheitsbox`) Klappe `EG_Abriegelung_Klappe` hoch + roter Knopf = **Abriegelung** → `RioAbriegelung.Abriegeln()`.

**Midgame**
1. **Abriegeln wie in „Haus des Geldes“:** Der Palast wird gegen die abgeriegelte Version getauscht (gleiche Stelle): Rollläden fahren
   herunter, Panzertüren fallen vor die Bronzeportale, Stahlträger vor das Lkw-Tor, Gitter vor die 5 Schleusen, Dachtür/Dachluke/Lüftung
   zu, Sandsäcke hinter den Portalen, Panzersperren und Betonblöcke vor beiden Toren. Polizei kommt nicht rein (Polizei-KI an
   `Polizei_Linie` warten lassen, solange `IstAbgeriegelt`).
2. **Dunkel → Nachtsicht:** In der abgeriegelten Version sind alle Lampen, Bildschirme und Laser aus, nur rotes Notlicht
   (`Abriegelung_<Ebene>_Notlicht`) und 7 Alarm-Leuchten (`Alarm_Leuchte_1…7`). Wachen stehen an `Wache_*`.
3. **Tresorwand aufsprengen (viel Sprengstoff aus dem Heli):** Weg nach unten über Treppenhäuser/Aufzüge bis UG2, dann Wartungstreppe oder
   Tresoraufzug nach UG3 (Tür `UG3_Tunnel_Tuer` in den Ring-Gang). Im Ring-Gang Ost: Bombe 1 an `Sprengladung_Tresor` → Node
   `UG3_Tresorwand_Sprengstelle` ausblenden (+ Explosion/Trümmer) → Loch 3 × 3,2 m in der Tresorwand, Brückenplatte über den Flutkanal.
4. **Einpacken:** 108 Goldstapel `UG3_Beute_Gold_*`, Bargeld (UG3 + Druckerei UG1), Schmuck und Uhren aus den Vitrinen (EG, OG1),
   Edelsteine `EG_Beute_Edelstein_Estrela` + `UG3_Beute_Edelstein_Coracao`, Krone `OG1_Beute_Krone`.

**Flucht**
1. **Alte Maya-Kanalisation aufbomben:** Bombe 2 an `Sprengladung_Kanal` (UG3, Ring-Gang Ost, gegenüber der Tresorwand) → Node
   `UG3_Kanalwand` (Maya-Steine mit Glyphe) ausblenden → Gang in die Kanalisation (Anker `Kanal_Anschluss` = Andockpunkt).
2. **Geparktes Boot befüllen** (`Boot_Parkplatz`).
3. **Boot verstecken:** durch das Netz zum Boot-Versteck, am losen Jade-Stein (`Geheimtor_Schalter`) das Geheimtor öffnen
   (`RioGeheimtor.Umschalten()`), Boot auf die Plattform (`Boot_Versteck`), `RioBootslift.Hoch()` → Boot steht in der Garage, Bodenklappen
   gehen vorher auf. Geheimtor wieder zu.
4. **Mit dem Auto die Sterne loswerden:** Fluchtauto in der Garage, Rolltor `Garage_Tor` hoch (Pivot oben, Y skalieren).

**Post-Heist:** Boot holen (Garage) – Gold und Juwelen an `Beute_Ablage` (Paletten) bzw. in den Tresor `Garage_Tresor` → verkaufen.

### Abriegelung (Version 03)
- **Vorbereiten:** Casa Império (01) platzieren, auswählen, **Tools ▸ Rio Heists ▸ Abriegelung vorbereiten**: legt 03 deckungsgleich und
  inaktiv an, richtet Hitboxen + Beute ein und erzeugt `Casa_Imperio_Abriegelung` mit `RioAbriegelung` (Felder Normal/Abgeriegelt).
- **Im Spiel:** `FindObjectOfType<RioAbriegelung>().Abriegeln();` (`Freigeben()` = zurück). Beim Tausch werden Türen, Tore, Aufzüge und
  bereits gestohlene Beute übernommen. Event `RioAbriegelung.Geaendert += (a, zu) => …` für Polizei/Musik.
- **Nodes in 03:** `Abriegelung_<EG|OG1|OG2|OG3>_<Vorne|Hinten|Links|Rechts>` (Rollläden, Pivot an der Oberkante → Y skalieren 0 → 1),
  `Abriegelung_EG_Portale` (fällt aus 6,5 m), `Abriegelung_EG_Schleusen` (fällt aus 3,2 m), `Abriegelung_EG_Lkw_Tor`,
  `Abriegelung_EG_Hintertuer`, `Abriegelung_Dach` (wachsen ein); statisch: `Abriegelung_<Ebene>_Kaesten` (Rollladenkästen),
  `…_Notlicht`, `…_Leuchten`, `Abriegelung_EG_Barrikaden`, `Abriegelung_Aussen` (werden an-/ausgeschaltet).
- Materialien in 03: `lamp_aus`, `glass_aus`, `glass_shop_aus`, `screen_aus`, `led_aus` (dunkel), Laser entfernt. Hitboxen der Rollläden
  hängen an den Pivots und skalieren mit.

## Maya-Kanalisation (Baukasten, Ordner `Kanalisation`)
**Konventionen** (wie beim Straßenbaukasten): 1 Einheit = 1 m, Y oben. **Pivot = Kachelmitte auf der Wasserlinie (y 0)**, Kanalsohle −1,6,
Laufstege +0,6, Kraggewölbe ab 3,4 m, Abdeckung bis 7,6 m, Bodenplatte ab −2,0 m. Kacheln 16 × 16 m (Halle 32 × 32).
Querschnitt: Wasser 5 m (±2,5), Laufstege 1,5 m, Mauern 0,8 m, Maya-Kraggewölbe mit 7 Stufen.
- **Waagerecht anschließen:** Anker `Anschluss_Kanal_<N|S|E|W>` mittig auf der Kachelkante (y 0, lokale +Z zeigt aus der Kachel; N = −Z,
  S = +Z, E = +X, W = −X). Zwei Teile passen, wenn ihre Anker aufeinander liegen und entgegengesetzt schauen.
- **Senkrecht stapeln:** `Unten` des oberen Teils auf `Oben` des unteren (Bootsversteck → Liftschacht 4 m/1 m → … → Garage;
  Einstieg → Schacht rund 4 m → … → Gullydeckel). Rotation gleich lassen (Leiter auf derselben Seite).
- **Am Palast:** `17_kanal_anschluss_palast` (oder das Beispielnetz) auswählen → **Tools ▸ Rio Heists ▸ Kanal an Palast andocken**: der
  Anker `Anschluss_Palast` (Gang-Ende, y 0,6) kommt auf den Palast-Anker `Kanal_Anschluss` (UG3-Außenwand Ost, Boden −20,5). Ohne Tool:
  Kachelmitte = `Kanal_Anschluss`-Position + 8 m in seiner Blickrichtung − 0,6 m in Y (Drehung = Palast-Drehung). Der Kanal liegt dann
  21,1 m unter dem Palast-Boden (Wasserlinie). Das Beispielnetz ist so gebaut, dass es den Palast nicht berührt.
- **Beliebige Teile verbinden:** zwei Anker auswählen (das zuletzt angeklickte bleibt stehen) → **Tools ▸ Rio Heists ▸ Zwei Anker verbinden**.
  Danach **Kanal-Teile einrichten (Auswahl)**: Hitboxen, Static-Flags, Beute/Ausrüstung, `RioGeheimtor` + `RioBootslift`.
- **Tiefe:** Das Beispielnetz hat die Straße bei y +21,1 (Garage, Gullydeckel) = Palast-Boden. Wer das Netz woanders hinlegt, braucht
  13 m Liftschacht (3 × 4 m + 1 × 1 m) bis zur Garage bzw. 12 m runden Schacht (3 × 4 m) bis zum Gullydeckel.
- **Terrain:** alles liegt unter der Erde → Terrain Holes nur für Garage/Gullydeckel; der Gullydeckel ist ein Interaktionspunkt
  (`Einstieg_Oben` ↔ `Einstieg_Unten` teleportieren, wenn die Straße darüber kein Loch hat).
- **Wasser:** `…__trigger_<n>` mit Material `kanal_wasser` = Schwimm-/Boot-Bereich (konvexe Stücke), Oberfläche y 0. Boote mit Auftrieb auf y 0
  halten. `water_jet` (Rinnsale, Maskenbrunnen) hat keine Hitbox.
- **Licht:** Fackeln haben Anker `Fackel_<n>` (Flamme) → dort Point Lights (orange, Reichweite ~6 m) setzen; Material `fackel` emissiv.
  Arbeitslampen im Versteck und in der Garage = Material `lamp`.
- **Bewegliche Teile:** `Kanal_Geheimtor` (+3,95 m in Y), `Kanal_Geheimtor_Schalter` (5 cm in die Wand), `Kanal_Bootslift` (Plattform, Y bis
  `Bootslift_Ziel`), `Garage_Bodenklappe_L/R` (Scharnier an der Außenkante, um Z ~95° hochklappen; Richtung im Editor prüfen – glTFast
  spiegelt X), `Garage_Tor` (Pivot oben, Y skalieren), `Garage_Seitentuer`, `Schacht_Gullydeckel`, `Kanal_Auslassgitter_L/R` (Scharnier
  außen, um Y nach außen), `Kanal_Beute_Jademaske` (Beute auf dem Pyramiden-Altar).
- **Leistung:** Teile 140–9.000 Dreiecke, Beispielnetz 76.332. Nodes: `Kanal__…` (Boden, Mauern,
  Wasser, Deko), `Decke__…` (Gewölbe), `Schacht__…`, `Garage__…`, `Dach__…`.

| Datei | Teil | Anker (ohne Fackeln) | Dreiecke |
|---|---|---|---|
| `10_kanal_beispielnetz.glb` | Maya-Kanal – Beispielnetz | `Anschluss_Palast`, `Auslass`, `Ausruestung_Tisch`, `Beute_Ablage`, `Boot_Parkplatz`, `Boot_Versteck`, `Bootslift_Unten`, `Bootslift_Ziel`, `Einstieg_Oben`, `Einstieg_Unten` … (+12) | 76.332 |
| `11_kanal_gerade.glb` | Kanal gerade 16 m | `Anschluss_Kanal_N`, `Anschluss_Kanal_S` | 3.232 |
| `12_kanal_kurve.glb` | Kanal Kurve 90° | `Anschluss_Kanal_E`, `Anschluss_Kanal_S` | 4.068 |
| `13_kanal_t.glb` | Kanal T-Stück | `Anschluss_Kanal_E`, `Anschluss_Kanal_N`, `Anschluss_Kanal_S` | 3.816 |
| `14_kanal_kreuzung.glb` | Kanal Kreuzung | `Anschluss_Kanal_E`, `Anschluss_Kanal_N`, `Anschluss_Kanal_S`, `Anschluss_Kanal_W` | 4.296 |
| `15_kanal_ende.glb` | Kanal Ende (Maske) | `Anschluss_Kanal_S`, `Maske` | 3.244 |
| `16_kanal_halle.glb` | Maya-Halle mit Pyramide 32 m | `Anschluss_Kanal_E`, `Anschluss_Kanal_N`, `Anschluss_Kanal_S`, `Anschluss_Kanal_W`, `Pyramide_Altar` | 8.972 |
| `17_kanal_anschluss_palast.glb` | Anschluss Palast + Boot-Parkplatz | `Anschluss_Kanal_N`, `Anschluss_Kanal_S`, `Anschluss_Palast`, `Boot_Parkplatz`, `Kanal_Einstieg_Boot` | 4.116 |
| `18_kanal_bootsversteck.glb` | Boot-Versteck (Geheimtor + Bootslift) | `Anschluss_Kanal_S`, `Boot_Versteck`, `Bootslift_Unten`, `Geheimtor`, `Geheimtor_Schalter`, `Oben` | 2.728 |
| `19_kanal_liftschacht_4m.glb` | Liftschacht 4 m | `Oben`, `Unten` | 584 |
| `20_kanal_liftschacht_1m.glb` | Liftschacht 1 m | `Oben`, `Unten` | 140 |
| `21_kanal_versteck_garage.glb` | Versteck-Garage (Straße) | `Ausruestung_Tisch`, `Beute_Ablage`, `Bootslift_Ziel`, `Fluchtauto`, `Garage_Eingang`, `Garage_Tresor`, `Unten` | 3.352 |
| `22_kanal_einstieg.glb` | Kanal mit Einstiegsnische | `Anschluss_Kanal_N`, `Anschluss_Kanal_S`, `Einstieg_Unten`, `Oben` | 4.224 |
| `23_kanal_schacht_rund_4m.glb` | Einstiegsschacht rund 4 m | `Oben`, `Unten` | 276 |
| `24_kanal_gullydeckel.glb` | Gullydeckel (Straße) | `Einstieg_Oben`, `Unten` | 544 |
| `25_kanal_auslass_meer.glb` | Auslass zum Meer (Gitter) | `Anschluss_Kanal_S`, `Auslass`, `Meer` | 3.708 |

## Modelle
| Datei | Name | B × T × H (m) | Dreiecke | Hitbox-Dreiecke | Anker |
|---|---|---|---|---|---|
| `01_casa_imperio.glb` | Casa Império (Mega-Heist-Palast) | 73.1 × 114 × 78.4 | 219500 | 112704 | 243 (siehe heists.json) |
| `02_yacht_soberana.glb` | Megayacht SOBERANA (150 m) | 157.3 × 28.7 × 39 | 108970 | 77836 | 309 (siehe heists.json) |
| `03_casa_imperio_abgeriegelt.glb` | Casa Império – abgeriegelt | 73.1 × 114 × 78.4 | 235344 | 120888 | 250 (siehe heists.json) |
