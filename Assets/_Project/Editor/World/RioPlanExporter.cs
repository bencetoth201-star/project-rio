using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Schreibt den Stadtplan als Textdatei, ohne irgendetwas in 3D zu bauen.
    ///
    /// Der Sinn: Fehler im Aufbau - abgerissene Strassen, leere Flaechen,
    /// falsch verteilte Viertel - sieht man auf einer Karte in Sekunden. In
    /// der 3D-Szene muss man dafuer hinlaufen. Erst wenn der Plan stimmt,
    /// lohnt es sich, ihn zu bauen.
    ///
    /// Menue: Tools / Rio / Plan exportieren (2D)
    /// </summary>
    public static class RioPlanExporter
    {
        const string OutPath = "Screenshots/rio_plan.txt";

        [MenuItem("Tools/Rio/Plan exportieren (2D)", priority = 20)]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Rio] Plan exportieren geht nur ausserhalb vom Play Mode.");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            EditorUtility.DisplayProgressBar("Rio", "Gelaende rechnen...", 0.2f);

            var terrain = new RioWorldTerrain(-4100f, 4100f, -2800f, 2800f, 25f);
            terrain.SmoothLowland(7);
            terrain.FlattenRoads();

            EditorUtility.DisplayProgressBar("Rio", "Stadt planen...", 0.5f);
            var planner = new RioHousePlanner(terrain);
            var rng = new System.Random(20261122);
            planner.BuildCityMask();
            planner.BuildDistrictMap();
            planner.InitOccupancy();
            planner.PlaceRoads();
            planner.PlaceVillas(rng);
            planner.PlaceSpecials(rng);
            planner.PlaceBlocks(rng);
            planner.PlaceFavelas(rng);

            EditorUtility.DisplayProgressBar("Rio", "Plan schreiben...", 0.85f);
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(1 << 22);

            sb.Append("HEAD ")
              .Append(terrain.MinX.ToString(inv)).Append(' ')
              .Append(terrain.MinZ.ToString(inv)).Append(' ')
              .Append(RioHousePlanner.Cell.ToString(inv)).Append(' ')
              .Append(planner.GridW).Append(' ').Append(planner.GridH).Append(' ')
              .Append(planner.SuperW).Append(' ').Append(planner.SuperH).Append('\n');

            // Wo ueberhaupt Stadt sein darf
            sb.Append("MASK\n");
            for (int gz = planner.GridH - 1; gz >= 0; gz--)
            {
                for (int gx = 0; gx < planner.GridW; gx++)
                    sb.Append(planner.CityAt(gx, gz) ? '1' : '0');
                sb.Append('\n');
            }

            // Viertelstyp je Superblock
            sb.Append("KIND\n");
            for (int sz = planner.SuperH - 1; sz >= 0; sz--)
            {
                for (int sx = 0; sx < planner.SuperW; sx++)
                    sb.Append((int)planner.KindOfSuper(sx, sz));
                sb.Append('\n');
            }

            // Alles Gesetzte: Strassenkacheln, Haeuser, Wahrzeichen
            sb.Append("PLACE ").Append(planner.Count).Append('\n');
            foreach (var p in planner.Placements)
            {
                sb.Append(p.position.x.ToString("F1", inv)).Append(' ')
                  .Append(p.position.z.ToString("F1", inv)).Append(' ')
                  .Append(p.yaw.ToString("F0", inv)).Append(' ')
                  .Append(p.model).Append('\n');
            }

            // Modelltabelle, damit der Zeichner Groesse und Art kennt
            sb.Append("MODEL ").Append(RioModelCatalog.Count).Append('\n');
            for (int i = 0; i < RioModelCatalog.Count; i++)
            {
                var e = RioModelCatalog.Get(i);
                sb.Append(i).Append(' ')
                  .Append(e.lot.x.ToString("F1", inv)).Append(' ')
                  .Append(e.lot.y.ToString("F1", inv)).Append(' ')
                  .Append(e.height.ToString("F1", inv)).Append(' ')
                  .Append(e.isRoad ? 1 : 0).Append(' ')
                  .Append(Path.GetFileNameWithoutExtension(e.path)).Append('\n');
            }

            string full = Path.Combine(Directory.GetCurrentDirectory(), OutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, sb.ToString());

            EditorUtility.ClearProgressBar();
            sw.Stop();

            Debug.Log($"[Rio] Plan exportiert in {sw.ElapsedMilliseconds / 1000f:F1}s: " +
                      $"{planner.RoadTiles:N0} Strassenkacheln, {planner.Houses:N0} Haeuser, " +
                      $"{planner.Shacks:N0} Huetten, {planner.Blocks:N0} Blocks -> {OutPath} " +
                      $"({sb.Length / 1024 / 1024} MB)");
        }
    }
}
