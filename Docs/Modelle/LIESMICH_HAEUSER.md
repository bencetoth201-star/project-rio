# Rio-Häuser für Unity – 17 Low-Poly-Häuser + Tankstelle 257 + Bank für GTA Mobile

## In 4 Schritten in deine Map

1. **glTFast installieren** (einmalig): *Window ▸ Package Manager ▸ + ▸ Add package by name…* → `com.unity.cloud.gltfast`
   (offizielles Unity-Paket; damit werden die .glb-Dateien direkt als Modelle importiert, auch in URP).
2. Den Ordner **`Assets/RioHaeuser`** aus diesem ZIP in den `Assets`-Ordner deines Projekts kopieren.
3. Deine Platzhalter so benennen, dass der Name mit **`Platzhalter`** beginnt (z. B. `Platzhalter_01`, `Platzhalter_arm_07`).
   Die **blaue Achse (Z)** des Platzhalters zeigt zur Straße. Die Unterkante des Platzhalters ist der Boden.
4. Menü **Tools ▸ Rio Häuser ▸ Platzhalter ersetzen** → Knopf **„Platzhalter ersetzen“**.

Das Werkzeug misst jeden Platzhalter (Würfel, Plane oder Box-Collider, auch skaliert), wählt ein Haus, dessen Grundstück
hineinpasst, und setzt es mittig ein. Danach würfelt es die Farben, richtet die mitgelieferten Hitboxen ein und markiert das Haus als statisch
(Türen, Tore und andere bewegliche Teile bleiben beweglich).
Die Platzhalter werden ausgeblendet (oder gelöscht, wenn du das ankreuzt). Alles lässt sich mit **Strg+Z** rückgängig machen.

**Welches Haus?** Ohne Angabe nimmt es ein Haus, das den Platzhalter gut ausfüllt (mit etwas Zufall für Abwechslung).
Steuern lässt sich das über den Namen:
- Kategorie: `…_sehrarm`, `…_arm`, `…_einfach`, `…_mittel`, `…_gehoben`, `…_reich`, `…_luxus`
- bestimmtes Haus: `…_barraco`, `…_palafita`, `…_puxadinho`, `…_favela_reihe`, `…_cortico`, `…_casa_rio`, `…_boteco`,
  `…_sobrado`, `…_mercadinho`, `…_predio`, `…_zona_oeste`, `…_villa_zona_sul`, `…_casa_barra`, `…_cobertura_leblon`, `…_casa_joa`,
  `…_mansao_leblon`, `…_mansao_joatinga`

**Farben ändern:** Haus auswählen → im Inspector bei *Rio Haus Farben* den **Seed** ändern, oder im Werkzeug-Fenster
„Farben der Auswahl neu würfeln“. *Zufallsfarben* aus = Originalfarben.

## Grundstücke (Puzzle-Raster)

Jedes Haus steht auf einem Grundstück, dessen Maße ein Vielfaches von **2 m** sind. Der **Nullpunkt** liegt in der Mitte des
Grundstücks am Boden, die **Vorderseite** liegt bündig an der +Z-Kante. Häuser mit gleich tiefem Grundstück stehen nebeneinander
also automatisch in einer Flucht. Grundstücke lassen sich lückenlos aneinandersetzen.

| Haus | Klasse | Grundstück (B × T) |
|---|---|---|
| Barraco | Sehr arm | 6 × 6 m |
| Palafita | Sehr arm | 10 × 8 m |
| Puxadinho | Arm | 6 × 8 m |
| Favela-Reihe | Arm | 10 × 8 m |
| Cortiço | Arm | 10 × 10 m |
| Casa Rio | Einfach | 8 × 8 m |
| Casa do Boteco | Mittel | 8 × 10 m |
| Sobrado | Mittel | 8 × 10 m |
| Mercadinho | Mittel | 10 × 10 m |
| Prédio Copacabana | Mittel | 12 × 12 m |
| Casa Zona Oeste | Gehoben | 16 × 14 m |
| Villa Zona Sul | Reich | 20 × 18 m |
| Casa Barra | Reich | 24 × 20 m |
| Cobertura Leblon | Reich | 18 × 18 m |
| Casa Modernista Joá | Reich | 32 × 20 m |
| Mansão Leblon (R$ 20 Mio.) | Luxus | 36 × 30 m |
| Mansão Joatinga (R$ 50 Mio.) | Luxus | 52 × 44 m |
| Tankstelle 257 | Spezial | 30 × 22 m |
| Banco de Brasilia | Spezial | 28 × 24 m |

**Tipp für Platzhalter:** Wenn du sie auf dieselben Größen (2-m-Schritte) bringst, passt jedes Haus exakt.

## Luxus-Villen
- **Mansão Leblon – R$ 20 Mio.:** L-förmige Villa (Glas-Erdgeschoss, auskragendes Holz-Obergeschoss, Dachterrasse mit Pavillon),
  Infinity-Pool mit Whirlpool und Holzdeck, überdachter Grillbereich (Espaço Gourmet), 3er-Garage mit Gründach, Pförtnerhaus, hohe Sichtschutzhecken. ~5.000 Dreiecke.
- **Mansão Joatinga – R$ 50 Mio.:** dreistöckige Mega-Villa: Steinsockel, weiße auskragende Etage mit Glasbalkon,
  Glas-Penthouse mit **Dach-Infinity-Pool**, **Hubschrauberlandeplatz**, großer Pool mit Cabanas, **Tennisplatz**, Gästehaus,
  6er-Garage, Palmenallee mit Springbrunnen, hohe Mauer mit Kameras und zweistöckigem Wachhaus. ~7.600 Dreiecke.
  Ankerpunkte: `Eingang`, `Helipad`, `Gaestehaus`.

## Bank „Banco de Brasilia“
Dreistöckig mit komplettem Innenraum nach euren Plänen. Wird nur über einen Platzhalter mit `bank` im Namen gesetzt (z. B. `Platzhalter_bank`).
- **Untergeschoss (y −4 m):** Treppe von oben → **Gitterwand mit Gittertür** (Tastenfeld) → Vorraum mit Kameras →
  **runde Tresortür** in dicker Wand → Tresorraum mit Goldbarren-Regalen und Geldpaletten.
- **Erdgeschoss:** Eingang mit Glas-Windfang und Stufen, Schalter mit 3 Plätzen (Monitore, Glasaufsatz), Absperrbänder,
  ATM innen neben der Treppe, Logo + Schriftzug an der Rückwand, Gitterwand mit Tür vor der Treppe ins UG, Kameras.
- **Obergeschoss:** Großraumbüro (6 Plätze), Empfang, Kopierer, 3 Büros (Direktor hinten), Lounge im Mittelbau, Terrassen mit Geländer.
- **Außen:** Fassade mit braunen Gesimsen, großes Schild mit Logo am Mittelbau, Vordach, **Außen-ATM**, Palmen, alte Laterne, Pflanzkübel.
- **Ebenen als eigene Objekte:** alle Teile heißen `UG__…`, `EG__…`, `OG__…`, `Dach__…`, `Aussen__…`.
  Betritt der Spieler ein Stockwerk, blendet ihr die Objekte darüber aus (wie in der Galerie mit „Ebene“).
- **Wichtig:** Das Untergeschoss liegt unter y = 0. Unter dem Gebäude braucht euer Terrain ein Loch bzw. darf nicht durch den Keller ragen.
- Ankerpunkte: `Eingang`, `ATM_Aussen`, `ATM_Innen`, `Schalter`, `Gitter_EG`, `Treppe_UG`, `Gitter_Tuer`, `Tresor_Tuer`, `Tresor`, `Buero_Direktor`.
- Schriftzug ändern: in `Extras/Generator/bank.py` die Zeile `SIGN_TEXT = ("Banco de", "Brasilia")` anpassen und neu erzeugen.
- ~15.000 Dreiecke (3 komplette Etagen inkl. Einrichtung).

## Tankstelle 257
Supermarkt mit **begehbarem Innenraum** (nach eurem Plan) + Tankdach mit 2 Zapfsäulen, Preismast und Parkplätzen.
- Wird **nie zufällig** gesetzt – nur über einen Platzhalter mit `tankstelle` im Namen (z. B. `Platzhalter_tankstelle`).
- Innen: Kasse mit Code-Feld links vom Eingang, Regale, Kühlschränke, Lager (normale Tür) und **Safe-Raum vorne rechts mit Code-Tür + Tastenfeld**, Hintertür im Anbau.
- **Dach ausblendbar:** alle Materialien, die mit `dach` beginnen (Dach, Deckenlampen, Klimageräte). Wenn der Spieler hineingeht, diese Objekte ausblenden.
- Ankerpunkte für Gameplay/Heists: `Eingang`, `Kasse`, `Safe`, `Safe_Tuer`, `Lager_Tuer`, `Hintertuer`, `Zapfsaeule_1`, `Zapfsaeule_2`.
- Leuchtet nachts: Schaufenster (`glass_shop`), Schild (`sign_light`), Deckenlampen (`dach_licht`), Tankdach-Lampen (`lamp`), Anzeigen (`led_green`, `led_orange`).
- 6.560 Dreiecke (Innenraum inklusive).

## Ankerpunkte (leere Objekte im Haus)
- **`Eingang`** – vor der Haustür bzw. dem Tor, Z zeigt nach außen. Für NPC-Spawns, Missionsmarker, Türen.
- **`Strom_Anschluss`** – Halterung am Dach bzw. an der Wand der Favela-Häuser. Hier können Kabel von euren Strommasten
  in der Map andocken. Die Häuser selbst haben keine frei hängenden Kabel mehr.

## Farb-Slots
Materialien mit Namen `slot_<art>_<n>` werden umgefärbt. Die Paletten stehen in `Scripts/RioHausDaten.cs`
(wall = 16 Rio-Farben, nwall = 8 edle Töne, base, door, shutter, roof, trim, accent, wood, glass).
Im Spiel erzeugt `RioHausFarben` geteilte Material-Varianten (gleiche Farbe = gleiches Material). Das bleibt für den
SRP-Batcher und für Mobile günstig.

## Nacht
Die Materialien `glass`, `glass_open`, `glass_big`, `glass_bank`, `glass_shop`, `slot_glass_1`, `lamp`, `dach_licht`, `sign_light`, `led_*` und `pool` sind dafür gedacht, nachts zu leuchten
(Emission im Tag-/Nacht-System einschalten). `glass_rail` (Glasgeländer) ist halbtransparent.

## Ohne glTFast
Im Ordner `Extras/OBJ` liegen dieselben Häuser als .obj + .mtl. Unity liest sie ohne Plugin. Lege sie dann in
`Assets/RioHaeuser/Modelle`. Die Ankerpunkte fehlen bei OBJ. In URP müssen die importierten Materialien eventuell
auf URP umgestellt werden.

## Technik
Maßstab 1 Einheit = 1 m · Y oben · keine Texturen · 1.500–4.700 Dreiecke pro Haus.
`Extras/Generator` enthält die Python-Skripte, mit denen alle Häuser erzeugt werden (`python3 houses.py`).
Danach erzeugt `python3 make_unity.py` die Unity-Daten neu.
