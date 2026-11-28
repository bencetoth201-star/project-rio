using Rio.World;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Baut die ganze Insel als Grundgeruest: rund 20 km2 Land, nach der
    /// Kartenskizze vom Whiteboard. Gelaende, Kuesten, Berge, See, Strand,
    /// Bruecke, Hauptstrassen, Stadtteile, Favelas und die Orte aus der
    /// Legende.
    ///
    /// Bewusst grob: alles flach schattierte Kloetze, keine Details. Es geht
    /// um Lage, Groesse und Wege - nicht um Optik.
    ///
    /// Menue: Tools / Rio / Welt bauen (20 km2)
    /// </summary>
    public static class RioWorldBuilder
    {
        const string RootName = "Rio_World";
        const string MeshLibPath = "Assets/_Project/Generated/World/RioWorld.asset";
        const string MaterialDir = "Assets/_Project/Materials/World";

        // Weltgrenzen in Metern. Deckt beide Inseln plus Meeresrand ab.
        const float MinX = -4100f, MaxX = 4100f;
        const float MinZ = -2800f, MaxZ = 2800f;
        /// <summary>
        /// Maschenweite des Hoehenfelds. Gleich der Kachelgroesse der Stadt:
        /// mit 25 m konnte das Gelaende einer Terrassenstufe alle 12 m nicht
        /// folgen, die Fahrbahn stand dadurch bis zu 3 m ueber dem Boden.
        /// </summary>
        const float TerrainCell = 12f;

        const float DistrictCell = 110f;
        const float FavelaCell = 62f;

        [MenuItem("Tools/Rio/Welt bauen (20 km2)", priority = 10)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[Rio] Welt bauen geht nur ausserhalb vom Play Mode.");
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            EditorUtility.DisplayProgressBar("Rio", "Gelaende rechnen...", 0.1f);

            var terrain = new RioWorldTerrain(MinX, MaxX, MinZ, MaxZ, TerrainCell);
            terrain.SmoothLowland(24);   // Ebene fuer die Stadt, Morros bleiben
            terrain.FlattenRoads();

            var b = new Buckets();
            var rng = new System.Random(20261122);

            // Die Reihenfolge ist der Kern der Sache. Vorher wurde hier das
            // Gelaendenetz gebaut und erst danach die Stadt geplant - das
            // Gelaende stand also fest, bevor die erste Strassenkachel gesetzt
            // war, und die Kacheln wurden auf einen Untergrund gelegt, der
            // nichts von ihnen wusste. Daher die schwebenden Objekte und die
            // Luecken an jeder Steigung.
            //
            // Jetzt plant die Stadt zuerst, legt ihre Hoehen fest, und das
            // Gelaende wird danach gebaut - auf diese Hoehen.

            // Die Wahrzeichen suchen sich ihren Platz vor der Stadt: ihre
            // Flaechen fallen aus der Stadtmaske, sonst legt der Strassenplan
            // ein Raster quer durch Flughafen und Stadion.
            EditorUtility.DisplayProgressBar("Rio", "Wahrzeichen planen...", 0.2f);
            RioWahrzeichen.Planen(terrain);

            EditorUtility.DisplayProgressBar("Rio", "Stadtflaechen suchen...", 0.3f);
            var planner = new RioHousePlanner(terrain);
            planner.BuildCityMask();
            planner.InitOccupancy();
            RioWahrzeichen.Belegen(planner);

            EditorUtility.DisplayProgressBar("Rio", "Strassennetz planen...", 0.38f);
            var plan = new RioStrassenplan(terrain, planner);
            plan.Viertel();
            plan.NaehteVerbinden();
            plan.HoehenAusGelaende();
            plan.AvenidasVerbinden(planner.PlacementList);
            plan.Schnellstrassen(planner.PlacementList);
            // Die Bruecke nach Niteroi kommt spaeter als eigenes Modell.
            // plan.Bruecke und plan.BrueckeAnbinden bleiben im Code, werden
            // aber nicht mehr aufgerufen.
            plan.AufEinNetzReduzieren();

            // Erst jetzt die Hoehen festlegen - vorher ist das Netz nicht
            // fertig, und ohne das fertige Netz weiss niemand, welche Kachel
            // welche Nachbarn bekommt.
            plan.HoehenAusgleichen();
            plan.GelaendeMelden();

            plan.Kacheln(planner.PlacementList, rng);

            // Was jetzt noch nirgends anschliesst, ist ein Stummel im Feld
            int splitter = RioStrassennetz.SplitterEntfernen(planner.PlacementList, 6);

            EditorUtility.DisplayProgressBar("Rio", "Wahrzeichen und Villen...", 0.5f);
            planner.PlaceVillas(rng);
            planner.PlaceSpecials(rng);

            EditorUtility.DisplayProgressBar("Rio", "Viertel bebauen...", 0.6f);
            planner.PlaceCity(rng);

            EditorUtility.DisplayProgressBar("Rio", "Favelas...", 0.7f);
            planner.PlaceFavelas(rng);

            EditorUtility.DisplayProgressBar("Rio", "Gelaende vernetzen...", 0.78f);
            BuildTerrainMesh(terrain, b);
            BuildWaterPlanes(b);

            EditorUtility.DisplayProgressBar("Rio", "Wahrzeichen...", 0.85f);
            BuildPlaces(terrain, b, rng);

            EditorUtility.DisplayProgressBar("Rio", "Gelaende schreiben...", 0.92f);
            int tris = Emit(b, terrain);

            EditorUtility.DisplayProgressBar("Rio", "Haeuser backen...", 0.96f);
            var root = GameObject.Find(RootName);
            var wallMats = new Material[b.Walls.Length];
            for (int i = 0; i < wallMats.Length; i++) wallMats[i] = GetOrCreateMaterial(b.Walls[i]);
            Material roofMat = GetOrCreateMaterial(b.Roof);

            Material asphaltMat = GetOrCreateMaterial(b.Asphalt);
            var houseWorld = RioHouseBaker.Bake(planner, root, MinX, MaxX, MinZ, MaxZ,
                                                wallMats, roofMat, asphaltMat, out int lodObjects);
            SetupStreamer(root, houseWorld);

            EditorUtility.DisplayProgressBar("Rio", "Wahrzeichen setzen...", 0.98f);
            int wahrzeichen = RioWahrzeichen.Setzen(root, terrain);
            Debug.Log($"[Rio] Wahrzeichen: {RioWahrzeichen.Liste.Count} Orte, {wahrzeichen} Modelle gesetzt.");
            Debug.Log($"[Rio] Teilnetze: {plan.NetzBericht}");

            PlacePlayer(terrain, plan);

            EditorUtility.ClearProgressBar();
            sw.Stop();

            float landKm2 = EstimateLandArea(terrain) / 1_000_000f;
            Debug.Log($"[Rio] Welt gebaut in {sw.ElapsedMilliseconds / 1000f:F1}s: " +
                      $"Gelaende {tris:N0} Dreiecke. " +
                      $"{plan} | {planner.Blocks:N0} Blocks mit " +
                      $"{planner.Houses:N0} Haeusern, {planner.Freiflaechen:N0} Plaetzen/Parks, " +
                      $"{planner.Shacks:N0} Favela-Huetten. " +
                      $"Viertel: Centro {planner.KindCount[0]}, Orla {planner.KindCount[1]}, " +
                      $"Bairro {planner.KindCount[2]}, Nobre {planner.KindCount[3]}, " +
                      $"Industrie {planner.KindCount[4]}, Favela {planner.KindCount[5]} Blocks. " +
                      $"Gesamt {planner.Count:N0} Objekte in {lodObjects} Chunks. " +
                      $"Landflaeche rund {landKm2:F1} km2.");
        }

        // ------------------------------------------------------------------
        // Gelaende
        // ------------------------------------------------------------------

        static void BuildTerrainMesh(RioWorldTerrain t, Buckets b)
        {
            for (int ix = 0; ix < t.CountX - 1; ix++)
            for (int iz = 0; iz < t.CountZ - 1; iz++)
            {
                Vector3 v00 = t.VertexAt(ix, iz);
                Vector3 v10 = t.VertexAt(ix + 1, iz);
                Vector3 v11 = t.VertexAt(ix + 1, iz + 1);
                Vector3 v01 = t.VertexAt(ix, iz + 1);

                // Unter Gebaeuden mit Untergeschoss bleibt das Gelaende offen
                if (RioWahrzeichen.ImLoch(new Vector2((v00.x + v11.x) * 0.5f, (v00.z + v11.z) * 0.5f)))
                    continue;

                float avg = (v00.y + v10.y + v11.y + v01.y) * 0.25f;
                float span = Mathf.Max(Mathf.Max(v00.y, v10.y), Mathf.Max(v11.y, v01.y)) -
                             Mathf.Min(Mathf.Min(v00.y, v10.y), Mathf.Min(v11.y, v01.y));
                float slope = span / t.Cell;

                MeshBucket bucket;
                if (avg < -0.3f) bucket = b.SeaFloor;
                else if (avg < 2.6f) bucket = b.Sand;
                else if (slope > 0.55f || avg > 125f) bucket = b.Rock;
                else bucket = b.Grass;

                bucket.AddQuad(v01, v11, v10, v00);
            }
        }

        static void BuildWaterPlanes(Buckets b)
        {
            Flat(b.Sea, MinX - 400f, MaxX + 400f, MinZ - 400f, MaxZ + 400f, RioWorldLayout.SeaLevel);

            // Der See im Suedwesten
            Vector2 c = RioWorldLayout.LakeCenter;
            float r = RioWorldLayout.LakeRadius * 0.94f;
            const int seg = 28;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2f;
                float a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
                b.Lake.AddTriangle(
                    new Vector3(c.x, RioWorldLayout.LakeLevel, c.y),
                    new Vector3(c.x + Mathf.Cos(a1) * r, RioWorldLayout.LakeLevel, c.y + Mathf.Sin(a1) * r),
                    new Vector3(c.x + Mathf.Cos(a0) * r, RioWorldLayout.LakeLevel, c.y + Mathf.Sin(a0) * r));
            }
        }

        // ------------------------------------------------------------------
        // Strassen
        // ------------------------------------------------------------------

        static void BuildRoads(RioWorldTerrain t, Buckets b)
        {
            foreach (var road in RioWorldLayout.Roads)
                Ribbon(t, b.Asphalt, b.RoadLine, RioWorldLayout.ToWorld(road), 9f, 0.28f);
        }

        static void Ribbon(RioWorldTerrain t, MeshBucket surface, MeshBucket line,
                           Vector2[] pts, float halfWidth, float lift)
        {
            var path = Resample(pts, 16f);
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i], c = path[i + 1];
                Vector2 dir = (c - a).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x) * halfWidth;

                float ha = t.SampleHeight(a) + lift;
                float hc = t.SampleHeight(c) + lift;

                Vector3 a0 = new Vector3(a.x - side.x, ha, a.y - side.y);
                Vector3 a1 = new Vector3(a.x + side.x, ha, a.y + side.y);
                Vector3 c0 = new Vector3(c.x - side.x, hc, c.y - side.y);
                Vector3 c1 = new Vector3(c.x + side.x, hc, c.y + side.y);

                surface.AddQuad(a1, c1, c0, a0);

                if (i % 3 == 0 && line != null)
                {
                    Vector2 m = side.normalized * 0.45f;
                    Vector3 l0 = new Vector3(a.x - m.x, ha + 0.03f, a.y - m.y);
                    Vector3 l1 = new Vector3(a.x + m.x, ha + 0.03f, a.y + m.y);
                    Vector3 l2 = new Vector3(c.x + m.x, hc + 0.03f, c.y + m.y);
                    Vector3 l3 = new Vector3(c.x - m.x, hc + 0.03f, c.y - m.y);
                    line.AddQuad(l1, l2, l3, l0);
                }
            }
        }

        static List<Vector2> Resample(Vector2[] pts, float step)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < pts.Length - 1; i++)
            {
                float len = Vector2.Distance(pts[i], pts[i + 1]);
                int n = Mathf.Max(1, Mathf.CeilToInt(len / step));
                for (int s = 0; s < n; s++)
                    result.Add(Vector2.Lerp(pts[i], pts[i + 1], s / (float)n));
            }
            result.Add(pts[pts.Length - 1]);
            return result;
        }

        static void BuildBridge(RioWorldTerrain t, Buckets b)
        {
            var pts = RioWorldLayout.ToWorld(RioWorldLayout.BridgePx);
            var path = Resample(pts, 20f);

            float startH = t.SampleHeight(path[0]);
            float endH = t.SampleHeight(path[path.Count - 1]);
            const float peak = 34f;
            const float half = 11f;

            var deckH = new float[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                float u = i / (float)(path.Count - 1);
                float arc = Mathf.Sin(u * Mathf.PI);
                deckH[i] = Mathf.Lerp(startH, endH, u) + arc * peak;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector2 a = path[i], c = path[i + 1];
                Vector2 dir = (c - a).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x) * half;

                Vector3 a0 = new Vector3(a.x - side.x, deckH[i], a.y - side.y);
                Vector3 a1 = new Vector3(a.x + side.x, deckH[i], a.y + side.y);
                Vector3 c0 = new Vector3(c.x - side.x, deckH[i + 1], c.y - side.y);
                Vector3 c1 = new Vector3(c.x + side.x, deckH[i + 1], c.y + side.y);

                b.Concrete.AddQuad(a1, c1, c0, a0);                          // Fahrbahn
                b.Concrete.AddQuad(a1, a1 + Vector3.down * 2.2f, c1 + Vector3.down * 2.2f, c1);
                b.Concrete.AddQuad(c0, c0 + Vector3.down * 2.2f, a0 + Vector3.down * 2.2f, a0);

                // Pylone
                if (i % 6 == 3)
                {
                    Vector2 m = (a + c) * 0.5f;
                    float ground = t.SampleHeight(m);
                    float h = deckH[i] - ground;
                    if (h > 3f)
                        b.Concrete.AddTaperedBox(new Vector3(m.x, ground, m.y),
                                                 new Vector2(9f, 9f), new Vector2(5f, 5f), h);
                }
            }
        }

        // ------------------------------------------------------------------
        // Wahrzeichen
        // ------------------------------------------------------------------

        static void BuildPlaces(RioWorldTerrain t, Buckets b, System.Random rng)
        {
            foreach (var place in RioWorldLayout.Places)
            {
                // Echtes Modell statt Klotz
                if (RioWahrzeichen.Ersetzt(place.Label)) continue;

                // Banken stellt der Hausplaner als echtes Modell hin
                // (PlaceSpecials) - der Klotz stand bisher zusaetzlich darin.
                if (place.Kind == RioWorldLayout.Poi.BankGross ||
                    place.Kind == RioWorldLayout.Poi.BankKlein) continue;

                // Die kleinen Flugplaetze bekommen spaeter ein eigenes Modell
                if (place.Kind == RioWorldLayout.Poi.FlughafenKlein) continue;

                Vector2 p = place.Position;
                float g = t.SampleHeight(p);

                switch (place.Kind)
                {
                    case RioWorldLayout.Poi.Hafen: BuildHafen(b, p, g); break;
                    case RioWorldLayout.Poi.FlughafenGross: BuildAirport(t, b, p, g, 1500f, 48f, true); break;
                    case RioWorldLayout.Poi.FlughafenKlein: BuildAirport(t, b, p, g, 780f, 30f, false); break;
                    case RioWorldLayout.Poi.Stadion: BuildStadium(b, p, g); break;
                    case RioWorldLayout.Poi.Militaerbasis: BuildMilitary(b, p, g); break;
                    case RioWorldLayout.Poi.JesusStatue: BuildJesus(b, p, g); break;
                    case RioWorldLayout.Poi.BankGross: BuildBank(b, p, g, 62f, 34f, 26f); break;
                    case RioWorldLayout.Poi.BankKlein: BuildBank(b, p, g, 34f, 22f, 15f); break;
                    case RioWorldLayout.Poi.Krankenhaus: BuildHospital(b, p, g); break;
                    case RioWorldLayout.Poi.Casino: BuildFlat(b, p, g, 54f, 40f, 22f, b.Gold); break;
                    case RioWorldLayout.Poi.Nightclub: BuildFlat(b, p, g, 40f, 32f, 14f, b.Neon); break;
                    case RioWorldLayout.Poi.JeweleryGross: BuildFlat(b, p, g, 38f, 30f, 26f, b.Jewel); break;
                    case RioWorldLayout.Poi.JeweleryKlein: BuildFlat(b, p, g, 26f, 22f, 16f, b.Jewel); break;
                    case RioWorldLayout.Poi.Clothstore: BuildFlat(b, p, g, 44f, 30f, 10f, b.Walls[1]); break;
                }
            }
        }

        static void Pad(Buckets b, Vector2 p, float g, float sx, float sz)
        {
            Flat(b.Concrete, p.x - sx * 0.5f, p.x + sx * 0.5f, p.y - sz * 0.5f, p.y + sz * 0.5f, g + 0.25f);
        }

        static void BuildFlat(Buckets b, Vector2 p, float g, float w, float d, float h, MeshBucket body)
        {
            Pad(b, p, g, w + 22f, d + 22f);
            body.AddTaperedBox(new Vector3(p.x, g, p.y), new Vector2(w, d), new Vector2(w, d), h);
            b.Roof.AddBox(new Vector3(p.x, g + h + 0.5f, p.y), new Vector3(w + 1.5f, 1f, d + 1.5f));
        }

        static void BuildBank(Buckets b, Vector2 p, float g, float w, float d, float h)
        {
            Pad(b, p, g, w + 30f, d + 30f);
            b.Stone.AddTaperedBox(new Vector3(p.x, g, p.y), new Vector2(w, d), new Vector2(w, d), h);
            // Saeulenfront
            int pillars = Mathf.Max(4, Mathf.RoundToInt(w / 9f));
            for (int i = 0; i < pillars; i++)
            {
                float fx = Mathf.Lerp(-w * 0.44f, w * 0.44f, i / (float)(pillars - 1));
                b.Stone.AddTaperedBox(new Vector3(p.x + fx, g, p.y + d * 0.5f + 3f),
                                      new Vector2(3.4f, 3.4f), new Vector2(3.4f, 3.4f), h * 0.78f);
            }
            b.Stone.AddBox(new Vector3(p.x, g + h * 0.78f + 1.2f, p.y + d * 0.5f + 3f),
                           new Vector3(w * 0.95f, 2.4f, 9f));
            b.Roof.AddBox(new Vector3(p.x, g + h + 0.8f, p.y), new Vector3(w + 2f, 1.6f, d + 2f));
        }

        static void BuildHospital(Buckets b, Vector2 p, float g)
        {
            Pad(b, p, g, 80f, 70f);
            b.White.AddTaperedBox(new Vector3(p.x, g, p.y), new Vector2(50f, 38f), new Vector2(50f, 38f), 30f);
            b.Roof.AddBox(new Vector3(p.x, g + 30.6f, p.y), new Vector3(52f, 1.2f, 40f));
            // rotes Kreuz auf dem Dach
            float y = g + 31.4f;
            Flat(b.Danger, p.x - 3.5f, p.x + 3.5f, p.y - 12f, p.y + 12f, y);
            Flat(b.Danger, p.x - 12f, p.x + 12f, p.y - 3.5f, p.y + 3.5f, y);
        }

        static void BuildHafen(Buckets b, Vector2 p, float g)
        {
            Pad(b, p, g, 320f, 220f);
            for (int i = 0; i < 3; i++)
            {
                float cx = p.x - 90f + i * 90f;
                b.Metal.AddTaperedBox(new Vector3(cx - 13f, g, p.y), new Vector2(5f, 5f), new Vector2(4f, 4f), 42f);
                b.Metal.AddTaperedBox(new Vector3(cx + 13f, g, p.y), new Vector2(5f, 5f), new Vector2(4f, 4f), 42f);
                b.Metal.AddBox(new Vector3(cx, g + 44f, p.y + 14f), new Vector3(7f, 4f, 78f));
            }
            // Containerstapel
            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                float cx = p.x + ((float)rng.NextDouble() - 0.5f) * 280f;
                float cz = p.y + 70f + ((float)rng.NextDouble() - 0.5f) * 90f;
                int stack = 1 + rng.Next(3);
                for (int s = 0; s < stack; s++)
                {
                    var bucket = b.Shacks[rng.Next(b.Shacks.Length)];
                    bucket.AddBox(new Vector3(cx, g + 1.6f + s * 3.0f, cz), new Vector3(12f, 2.9f, 5f));
                }
            }
        }

        static void BuildAirport(RioWorldTerrain t, Buckets b, Vector2 p, float g,
                                 float length, float width, bool big)
        {
            // In Segmente zerlegt: ein 1500 m langes Dreieck macht der
            // Physik-Engine Probleme (Warnung ab 500 Einheiten Kantenlaenge).
            const float seg = 60f;
            for (float sx = -length * 0.5f; sx < length * 0.5f; sx += seg)
            {
                float x1 = Mathf.Min(sx + seg, length * 0.5f);
                Flat(b.Asphalt, p.x + sx, p.x + x1,
                     p.y - width * 0.5f, p.y + width * 0.5f, g + 0.3f);
            }

            for (float mx = -length * 0.5f + 40f; mx < length * 0.5f - 40f; mx += 70f)
                Flat(b.RoadLine, p.x + mx, p.x + mx + 34f, p.y - 1.2f, p.y + 1.2f, g + 0.35f);

            float tw = big ? 180f : 70f;
            float td = big ? 60f : 28f;
            float th = big ? 24f : 12f;
            b.White.AddTaperedBox(new Vector3(p.x, g, p.y + width * 0.5f + td * 0.6f),
                                  new Vector2(tw, td), new Vector2(tw, td), th);
            b.Roof.AddBox(new Vector3(p.x, g + th + 0.5f, p.y + width * 0.5f + td * 0.6f),
                          new Vector3(tw + 2f, 1f, td + 2f));
            if (big)
                b.Metal.AddTaperedBox(new Vector3(p.x + tw * 0.4f, g, p.y + width * 0.5f + td * 1.4f),
                                      new Vector2(10f, 10f), new Vector2(7f, 7f), 42f);
        }

        static void BuildStadium(Buckets b, Vector2 p, float g)
        {
            Pad(b, p, g, 240f, 200f);
            const int seg = 26;
            float ra = 96f, rb = 78f;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cx = p.x + Mathf.Cos(a) * ra;
                float cz = p.y + Mathf.Sin(a) * rb;
                float yaw = -a * Mathf.Rad2Deg;
                b.Concrete.AddTaperedBox(new Vector3(cx, g, cz), new Vector2(26f, 30f),
                                         new Vector2(26f, 22f), 26f, yaw);
            }
            // Rasen
            Flat(b.Pitch, p.x - 52f, p.x + 52f, p.y - 34f, p.y + 34f, g + 0.4f);
        }

        static void BuildMilitary(Buckets b, Vector2 p, float g)
        {
            Pad(b, p, g, 300f, 230f);
            for (int i = 0; i < 3; i++)
            {
                float cz = p.y - 60f + i * 60f;
                b.Metal.AddTaperedBox(new Vector3(p.x - 60f, g, cz), new Vector2(70f, 34f),
                                      new Vector2(56f, 24f), 16f);
            }
            // Wachturm
            b.Concrete.AddTaperedBox(new Vector3(p.x + 70f, g, p.y), new Vector2(10f, 10f),
                                     new Vector2(7f, 7f), 34f);
            b.Metal.AddBox(new Vector3(p.x + 70f, g + 36f, p.y), new Vector3(16f, 4f, 16f));
            // Zaun
            float hw = 140f, hd = 105f;
            for (float fx = -hw; fx <= hw; fx += 14f)
            {
                b.Metal.AddBox(new Vector3(p.x + fx, g + 2.5f, p.y - hd), new Vector3(13f, 5f, 0.5f));
                b.Metal.AddBox(new Vector3(p.x + fx, g + 2.5f, p.y + hd), new Vector3(13f, 5f, 0.5f));
            }
            for (float fz = -hd; fz <= hd; fz += 14f)
            {
                b.Metal.AddBox(new Vector3(p.x - hw, g + 2.5f, p.y + fz), new Vector3(0.5f, 5f, 13f));
                b.Metal.AddBox(new Vector3(p.x + hw, g + 2.5f, p.y + fz), new Vector3(0.5f, 5f, 13f));
            }
        }

        static void BuildJesus(Buckets b, Vector2 p, float g)
        {
            // Auf dem Gipfel des Hausbergs, nicht am Hang
            Vector2 summit = RioWorldLayout.Mountains[0].Center;
            float sg = g;
            // Sockel
            b.Stone.AddTaperedBox(new Vector3(summit.x, sg, summit.y), new Vector2(26f, 26f),
                                  new Vector2(20f, 20f), 14f);
            float baseY = sg + 14f;
            // Koerper
            b.White.AddTaperedBox(new Vector3(summit.x, baseY, summit.y), new Vector2(9f, 6f),
                                  new Vector2(6f, 4.5f), 26f);
            // Arme
            b.White.AddBox(new Vector3(summit.x, baseY + 22f, summit.y), new Vector3(42f, 3.4f, 4f));
            // Kopf
            b.White.AddBox(new Vector3(summit.x, baseY + 28.5f, summit.y), new Vector3(4.5f, 5f, 4.5f));
        }

        // ------------------------------------------------------------------
        // Ausgabe
        // ------------------------------------------------------------------

        static int Emit(Buckets b, RioWorldTerrain t)
        {
            EnsureDir(Path.GetDirectoryName(MeshLibPath).Replace('\\', '/'));
            EnsureDir(MaterialDir);

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            // Das kleine Testgelaende laege mitten in der Bucht - weg damit.
            var small = GameObject.Find("Rio_Blockout");
            if (small != null) Object.DestroyImmediate(small);

            var root = new GameObject(RootName);

            AssetDatabase.DeleteAsset(MeshLibPath);

            Mesh main = null;
            int tris = 0;

            foreach (var bucket in b.All)
            {
                if (bucket.IsEmpty) continue;
                Mesh mesh = bucket.BuildMesh();

                if (main == null) { main = mesh; AssetDatabase.CreateAsset(mesh, MeshLibPath); }
                else AssetDatabase.AddObjectToAsset(mesh, main);

                var go = new GameObject(bucket.Name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = GetOrCreateMaterial(bucket);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

                if (bucket.Name != "Sea" && bucket.Name != "Lake")
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;

                tris += bucket.TriangleCount;
            }

            BuildBounds(root);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            return tris;
        }

        static void BuildBounds(GameObject root)
        {
            var bounds = new GameObject("WorldBounds");
            bounds.transform.SetParent(root.transform, false);

            const float height = 400f, thick = 4f;
            float cx = (MinX + MaxX) * 0.5f, cz = (MinZ + MaxZ) * 0.5f;
            float sx = MaxX - MinX, sz = MaxZ - MinZ;
            float cy = -60f + height * 0.5f;

            Wall(bounds, "West", new Vector3(MinX - thick * 0.5f, cy, cz), new Vector3(thick, height, sz));
            Wall(bounds, "Ost", new Vector3(MaxX + thick * 0.5f, cy, cz), new Vector3(thick, height, sz));
            Wall(bounds, "Sued", new Vector3(cx, cy, MinZ - thick * 0.5f), new Vector3(sx, height, thick));
            Wall(bounds, "Nord", new Vector3(cx, cy, MaxZ + thick * 0.5f), new Vector3(sx, height, thick));
        }

        static void Wall(GameObject parent, string name, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Bound_" + name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;
        }

        /// <summary>
        /// Haengt den Streamer an die Weltwurzel und verdrahtet ihn mit den
        /// Daten und den Fern-Kloetzen.
        /// </summary>
        static void SetupStreamer(GameObject root, RioHouseWorld houseWorld)
        {
            var streamer = root.GetComponent<RioHouseStreamer>();
            if (streamer == null) streamer = root.AddComponent<RioHouseStreamer>();

            streamer.world = houseWorld;
            streamer.farLodRoot = root.transform.Find("FarLod");

            // Beim Bauen auf die aktuellen Werte setzen, sonst bleibt in der
            // Szene stehen, was beim ersten Mal serialisiert wurde.
            streamer.nearRadius = 600f;
            streamer.hysteresis = 180f;
            streamer.spawnsPerFrame = 60;
            streamer.cacheChunks = 40;
            streamer.combineChunks = true;

            var player = GameObject.Find("Player");
            streamer.player = player != null ? player.transform : null;
        }

        /// <summary>
        /// Setzt die Figur auf festen Boden in der Naehe des Strandbogens und
        /// stellt die Kamera dahinter. Ohne das stuende sie im offenen Meer.
        /// </summary>
        static void PlacePlayer(RioWorldTerrain t, RioStrassenplan plan)
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            // Mitten in Centro, auf einer Strasse. Vorher stand die
            // Figur am Strand - kilometerweit von allem weg, was gebaut
            // wurde, und man sah beim Start nur Sand.
            if (plan != null && plan.Startpunkt(out Vector3 inDerStadt))
            {
                Setzen(player, inDerStadt);
                Debug.Log($"[Rio] Figur gesetzt in Centro auf {inDerStadt}");
                return;
            }

            Vector2 start = RioWorldLayout.BeachCenter;
            Vector2 spot = start;
            bool found = false;

            for (float r = 60f; r <= 900f && !found; r += 25f)
            for (int a = 0; a < 24 && !found; a++)
            {
                float rad = a / 24f * Mathf.PI * 2f;
                Vector2 q = start + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
                if (!t.IsLand(q) || t.DistanceToCoast(q) < 45f) continue;
                float h = t.SampleHeight(q);
                if (h < 3f || h > 22f) continue;
                if (t.SlopeAt(q) > 0.10f) continue;
                spot = q; found = true;
            }

            float ground = t.SampleHeight(spot);
            Setzen(player, new Vector3(spot.x, ground + 2f, spot.y));

            Debug.Log($"[Rio] Figur gesetzt auf {spot} (Hoehe {ground:F1} m)" +
                      (found ? "" : " - kein flacher Platz gefunden, Notfallposition"));
        }

        static void Setzen(GameObject player, Vector3 pos)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = pos;
            if (cc != null) cc.enabled = true;

            var cam = Camera.main;
            if (cam == null) return;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 3000f);
            Quaternion look = Quaternion.Euler(16f, 0f, 0f);
            cam.transform.SetPositionAndRotation(
                pos + new Vector3(0f, 1.45f, 0f) + look * Vector3.back * 7f, look);
        }

        static float EstimateLandArea(RioWorldTerrain t)
        {
            float area = 0f;
            float step = 40f;
            for (float x = MinX; x < MaxX; x += step)
            for (float z = MinZ; z < MaxZ; z += step)
                if (t.IsLand(new Vector2(x, z))) area += step * step;
            return area;
        }

        // ------------------------------------------------------------------

        static void Flat(MeshBucket bucket, float x0, float x1, float z0, float z1, float y)
        {
            bucket.AddQuad(new Vector3(x0, y, z1), new Vector3(x1, y, z1),
                           new Vector3(x1, y, z0), new Vector3(x0, y, z0));
        }

        static void EnsureDir(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(full)) { Directory.CreateDirectory(full); AssetDatabase.Refresh(); }
        }

        static Material GetOrCreateMaterial(MeshBucket bucket)
        {
            string path = $"{MaterialDir}/M_{bucket.Name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader) { name = "M_" + bucket.Name };
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", bucket.Color);
            mat.SetColor("_Color", bucket.Color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", bucket.Smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", bucket.Smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        class Buckets
        {
            public readonly MeshBucket Sea = new MeshBucket("Sea", Hex("#1E6C86"), 0.85f);
            public readonly MeshBucket Lake = new MeshBucket("Lake", Hex("#2E7E92"), 0.8f);
            public readonly MeshBucket SeaFloor = new MeshBucket("SeaFloor", Hex("#9C8E6B"));
            public readonly MeshBucket Sand = new MeshBucket("Sand", Hex("#E2CFA6"));
            public readonly MeshBucket Grass = new MeshBucket("Grass", Hex("#5A7A42"));
            public readonly MeshBucket Rock = new MeshBucket("Rock", Hex("#6E6354"));
            public readonly MeshBucket Asphalt = new MeshBucket("Asphalt", Hex("#3B3B3E"));
            public readonly MeshBucket RoadLine = new MeshBucket("RoadLine", Hex("#D8D2BE"));
            public readonly MeshBucket Concrete = new MeshBucket("Concrete", Hex("#A9A294"));
            public readonly MeshBucket Courtyard = new MeshBucket("Courtyard", Hex("#9E9887"));
            public readonly MeshBucket Stone = new MeshBucket("Stone", Hex("#CFC6B2"));
            public readonly MeshBucket White = new MeshBucket("White", Hex("#F0EFE8"));
            public readonly MeshBucket Metal = new MeshBucket("Metal", Hex("#7E8389"), 0.4f);
            public readonly MeshBucket Gold = new MeshBucket("Gold", Hex("#C9A24A"), 0.5f);
            public readonly MeshBucket Neon = new MeshBucket("Neon", Hex("#8E4AC9"));
            public readonly MeshBucket Jewel = new MeshBucket("Jewel", Hex("#4FC3D9"), 0.6f);
            public readonly MeshBucket Danger = new MeshBucket("Danger", Hex("#B5402F"));
            public readonly MeshBucket Pitch = new MeshBucket("Pitch", Hex("#3F7A3A"));
            public readonly MeshBucket Roof = new MeshBucket("Roof", Hex("#6B6259"));

            public readonly MeshBucket[] Walls =
            {
                new MeshBucket("BuildingCream", Hex("#EDE3D0")),
                new MeshBucket("BuildingWhite", Hex("#F1F0EA")),
                new MeshBucket("BuildingOchre", Hex("#D9B98C")),
                new MeshBucket("BuildingBlue",  Hex("#AEC3CE")),
            };

            public readonly MeshBucket[] Shacks =
            {
                new MeshBucket("ShackTerracotta", Hex("#C2603F")),
                new MeshBucket("ShackTeal",       Hex("#3E8E8A")),
                new MeshBucket("ShackYellow",     Hex("#E0B437")),
                new MeshBucket("ShackPink",       Hex("#C96A84")),
            };

            public IEnumerable<MeshBucket> All
            {
                get
                {
                    yield return Sea; yield return Lake; yield return SeaFloor;
                    yield return Sand; yield return Grass; yield return Rock;
                    yield return Asphalt; yield return RoadLine; yield return Concrete;
                    yield return Courtyard;
                    yield return Stone; yield return White; yield return Metal;
                    yield return Gold; yield return Neon; yield return Jewel;
                    yield return Danger; yield return Pitch; yield return Roof;
                    foreach (var m in Walls) yield return m;
                    foreach (var m in Shacks) yield return m;
                }
            }
        }
    }
}
