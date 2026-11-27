# Anleitung für Claude Code: Fahrzeug „Valente GT“

Fiktives Sport-Coupé (eigenes Design, keine echte Marke) im Low-Poly-Stil der Rio-Häuser.

## Inhalt
```
Assets/RioFahrzeuge/Modelle/valente_gt.glb   Modell (glTF-Binary, Import über glTFast)
Assets/RioFahrzeuge/fahrzeuge.json           Maße, Dreiecke, Farb-Slots, Anker
Assets/RioHaeuser/Scripts/RioHausDaten.cs    AKTUALISIERTE Version (enthält jetzt die Paletten paint/rim/caliper)
                                            -> ersetzt die gleichnamige Datei aus dem Häuser-Paket
Extras/OBJ/, Extras/Generator/               Fallback-Format und Python-Generator
```
Voraussetzung: das Häuser-Paket (`Assets/RioHaeuser`) ist bereits im Projekt, weil `RioHausFarben` von dort verwendet wird.

## Konventionen
- 1 Einheit = 1 m. Länge 4.96 m, Breite inkl. Spiegel 2.24 m, Höhe 1.39 m. 3492 Dreiecke.
- Pivot: Boden (y = 0), Mitte zwischen den Achsen. **Front = +Z**. Fahrerseite = linke Seite (Brasilien: Linkslenker).
- **Räder** sind eigene Objekte mit Drehpunkt in der Radmitte: `Rad_VL`, `Rad_VR`, `Rad_HL`, `Rad_HR`
  (V = vorne, H = hinten, L = links, R = rechts). Für die Fahrt um die lokale **X-Achse** drehen, Vorderräder zum Lenken um **Y**.
  Radradius 0,36 m, Radstand 2,60 m, Spur 1,68 m.
- Bremssättel gehören zur Karosserie (drehen nicht mit).
- **Farb-Slots** (Tuning-Garage): `slot_paint_1` (Lack, 16 Farben), `slot_rim_1` (Felgen, 6), `slot_caliper_1` (Bremssättel, 6) → 576 Kombinationen.
  Komponente `RioHausFarben` ans Fahrzeug hängen (Seed = Variante) oder die Materialfarben direkt aus dem Tuning-Menü setzen
  (`_BaseColor` in URP, `_Color` Built-in, `baseColorFactor` bei glTFast-Shadern).
- **Lichter (Emission bei Nacht):** `car_light` (Scheinwerfer), `lamp` (Tagfahrlicht), `car_tail` (Rücklicht, beim Bremsen heller).
- Glas `car_glass` ist leicht durchsichtig (Innenraum mit Sitzen, Lenkrad, Armaturenbrett sichtbar).

## Anker (leere Child-Objekte)
- `Fahrertuer` (1.35, 0, -0.15)
- `Beifahrertuer` (-1.35, 0, -0.15)
- `Sitz_Fahrer` (0.38, 0.48, -0.4)
- `Sitz_Beifahrer` (-0.38, 0.48, -0.4)
- `Kofferraum` (0, 0.5, -2.9)
- `Motorhaube` (0, 0.8, 1.6)
- `Auspuff` (0, 0.31, -2.5)
- `Scheinwerfer` (0, 0.6, 2.5)
(glTF-Koordinaten. glTFast spiegelt beim Import die X-Achse – Positionen deshalb aus Unity lesen.)

## Einbau-Empfehlung
1. glb importieren (glTFast), Prefab daraus machen.
2. Rigidbody (~1.500 kg) + BoxCollider für die Karosserie, 4 × WheelCollider an den Positionen der `Rad_*`-Objekte (Radius 0,36).
3. Pro Frame die Rad-Objekte mit der WheelCollider-Pose synchronisieren (`GetWorldPose`).
4. Fahrer-Animation: Ein-/Ausstieg an `Fahrertuer`, Sitzposition `Sitz_Fahrer`.
