using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Baut ein kleines Stueck Rio als Low-Poly-Blockout:
    /// Meer, Strand, Promenade, Strasse, ein paar Haeuserblocks und ein
    /// Morro mit Favela-Huetten. Flach schattiert, einfarbige Flaechen,
    /// keine Texturen - der Look der Auto-Renderings vom Whiteboard.
    ///
    /// Menue: Tools / Rio / Blockout bauen
    /// </summary>
    public static class RioBlockoutBuilder
    {
        const string RootName = "Rio_Blockout";
        const string ProjectRoot = "Assets/_Project";
        const string MaterialDir = ProjectRoot + "/Materials/World";
        const string MeshDir = ProjectRoot + "/Generated/World";

        // ---- Abmessungen des Ausschnitts (Meter) ----
        const float HalfWidth = 110f;   // X von -110 bis +110
        const float SeaFrom = -150f;
        const float BeachFrom = -42f;
        const float BeachTo = -9f;
        const float PromenadeTo = -4.5f;
        const float RoadTo = 4.5f;
        const float SidewalkTo = 8.5f;
        const float BlocksTo = 56f;
        const float HillTo = 132f;

        const float GroundY = 1.4f;     // Strassenniveau
        const float HillPeak = 34f;

        // ------------------------------------------------------------------

        [MenuItem("Tools/Rio/Blockout bauen", priority = 0)]
        public static void Build()
        {
            var buckets = new Buckets();
            var rng = new System.Random(20261122);

            BuildSea(buckets);
            BuildBeach(buckets);
            BuildPromenade(buckets);
            BuildRoad(buckets);
            BuildCityBlocks(buckets, rng);
            BuildHill(buckets);
            BuildFavela(buckets, rng);
            BuildPalms(buckets, rng);

            var root = ReplaceRoot();
            int tris = 0, objects = 0;

            EnsureDir(MaterialDir);
            EnsureDir(MeshDir);

            foreach (var bucket in buckets.All)
            {
                if (bucket.IsEmpty) continue;

                Mesh mesh = bucket.BuildMesh();
                string meshPath = $"{MeshDir}/{bucket.Name}.asset";
                AssetDatabase.DeleteAsset(meshPath);
                AssetDatabase.CreateAsset(mesh, meshPath);

                Material mat = GetOrCreateMaterial(bucket);

                var go = new GameObject(bucket.Name);
                go.transform.SetParent(root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

                // Wasser und Palmwedel bleiben ohne Kollision: ins Meer soll man
                // laufen koennen, und Blaetter sollen nicht im Weg stehen.
                if (bucket.Name != "Sea" && bucket.Name != "PalmLeaf")
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;

                tris += bucket.TriangleCount;
                objects++;
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log($"[Rio] Blockout gebaut: {objects} Meshes, {tris} Dreiecke. " +
                      $"Ausschnitt {HalfWidth * 2f}m x {HillTo - SeaFrom}m.");
        }

        // ------------------------------------------------------------------
        // Gelaende
        // ------------------------------------------------------------------

        static void BuildSea(Buckets b)
        {
            // Wasserflaeche, leicht unter Null damit der Strand sauber eintaucht
            b.Sea.AddQuad(
                new Vector3(-HalfWidth - 60f, 0f, BeachTo - 6f),
                new Vector3(HalfWidth + 60f, 0f, BeachTo - 6f),
                new Vector3(HalfWidth + 60f, 0f, SeaFrom),
                new Vector3(-HalfWidth - 60f, 0f, SeaFrom));
        }

        static float BeachHeight(float x, float z)
        {
            float t = Mathf.InverseLerp(BeachFrom, BeachTo, z);
            t = Mathf.SmoothStep(0f, 1f, t);
            float dunes = (Mathf.PerlinNoise(x * 0.035f + 11f, z * 0.05f + 3f) - 0.5f) * 0.9f * t;
            return Mathf.Lerp(-1.1f, GroundY, t) + dunes;
        }

        static void BuildBeach(Buckets b)
        {
            b.Sand.AddHeightGrid(
                new Vector3(-HalfWidth, 0f, BeachFrom),
                HalfWidth * 2f, BeachTo - BeachFrom,
                28, 8,
                (x, z) => BeachHeight(x, z));
        }

        static void BuildPromenade(Buckets b)
        {
            // Gehweg am Strand plus niedrige Mauer zum Sand hin
            Flat(b.Sidewalk, -HalfWidth, HalfWidth, BeachTo, PromenadeTo, GroundY);
            b.Wall.AddBox(
                new Vector3(0f, GroundY - 0.25f, BeachTo + 0.2f),
                new Vector3(HalfWidth * 2f, 1.0f, 0.5f));

            // Gehweg auf der Stadtseite
            Flat(b.Sidewalk, -HalfWidth, HalfWidth, RoadTo, SidewalkTo, GroundY);
        }

        static void BuildRoad(Buckets b)
        {
            Flat(b.Asphalt, -HalfWidth, HalfWidth, PromenadeTo, RoadTo, GroundY - 0.15f);

            // Mittelstreifen
            for (float x = -HalfWidth + 3f; x < HalfWidth; x += 9f)
                Flat(b.RoadLine, x, x + 4.5f, -0.2f, 0.2f, GroundY - 0.12f);
        }

        static float HillHeight(float x, float z)
        {
            float t = Mathf.InverseLerp(BlocksTo - 4f, HillTo, z);
            t = Mathf.SmoothStep(0f, 1f, t);
            float ridge = Mathf.PerlinNoise(x * 0.012f + 5f, 0.7f) * 0.45f + 0.75f;
            float bumps = (Mathf.PerlinNoise(x * 0.06f, z * 0.06f) - 0.5f) * 5f * t;
            return GroundY + t * t * HillPeak * ridge + bumps;
        }

        static void BuildHill(Buckets b)
        {
            b.Hill.AddHeightGrid(
                new Vector3(-HalfWidth, 0f, BlocksTo - 4f),
                HalfWidth * 2f, HillTo - (BlocksTo - 4f),
                36, 24,
                (x, z) => HillHeight(x, z));
        }

        // ------------------------------------------------------------------
        // Bebauung
        // ------------------------------------------------------------------

        static void BuildCityBlocks(Buckets b, System.Random rng)
        {
            // Zwei Haeuserreihen mit Querstrassen dazwischen
            float[] rowStart = { 11f, 34f };
            float[] rowDepth = { 17f, 20f };

            foreach (var pair in Zip(rowStart, rowDepth))
            {
                float z0 = pair.Item1;
                float depth = pair.Item2;

                float x = -HalfWidth + 3f;
                while (x < HalfWidth - 12f)
                {
                    float blockWidth = Mathf.Lerp(22f, 34f, (float)rng.NextDouble());
                    blockWidth = Mathf.Min(blockWidth, HalfWidth - 3f - x);
                    if (blockWidth < 14f) break;

                    BuildBlock(b, rng, x, x + blockWidth, z0, z0 + depth);
                    x += blockWidth + 8f; // Querstrasse
                }

                // Gehweg vor der Reihe
                Flat(b.Sidewalk, -HalfWidth, HalfWidth, z0 - 2.5f, z0, GroundY);
                // Querstrassen-Asphalt als durchgehende Flaeche hinter der Reihe
                Flat(b.Asphalt, -HalfWidth, HalfWidth, z0 + depth, z0 + depth + 6f, GroundY - 0.15f);
            }
        }

        static void BuildBlock(Buckets b, System.Random rng, float x0, float x1, float z0, float z1)
        {
            float x = x0;
            while (x < x1 - 6f)
            {
                float w = Mathf.Lerp(9f, 16f, (float)rng.NextDouble());
                w = Mathf.Min(w, x1 - x);
                if (w < 6f) break;

                float d = (z1 - z0) * Mathf.Lerp(0.62f, 0.95f, (float)rng.NextDouble());
                float h = Mathf.Lerp(11f, 42f, Mathf.Pow((float)rng.NextDouble(), 1.4f));

                var wall = b.BuildingWalls[rng.Next(b.BuildingWalls.Length)];
                Vector3 baseCenter = new Vector3(x + w * 0.5f, GroundY, z0 + d * 0.5f);

                // Hauptkoerper
                wall.AddTaperedBox(baseCenter, new Vector2(w - 1.2f, d - 1.2f),
                                   new Vector2(w - 1.2f, d - 1.2f), h);

                // Dachplatte
                b.Roof.AddBox(baseCenter + new Vector3(0f, h + 0.3f, 0f),
                              new Vector3(w - 0.4f, 0.6f, d - 0.4f));

                // Hoehere Haeuser bekommen einen Aufsatz
                if (h > 26f && rng.NextDouble() < 0.6)
                {
                    float sw = (w - 1.2f) * 0.45f;
                    float sd = (d - 1.2f) * 0.45f;
                    b.Roof.AddBox(baseCenter + new Vector3(0f, h + 2.2f, 0f),
                                  new Vector3(sw, 3.2f, sd));
                }

                x += w + 1.5f;
            }
        }

        static void BuildFavela(Buckets b, System.Random rng)
        {
            // Huetten kleben am Hang, leicht verdreht, in kraeftigen Farben
            for (int i = 0; i < 160; i++)
            {
                float x = Mathf.Lerp(-HalfWidth + 6f, HalfWidth - 6f, (float)rng.NextDouble());
                float z = Mathf.Lerp(BlocksTo + 6f, HillTo - 10f, Mathf.Sqrt((float)rng.NextDouble()));
                float ground = HillHeight(x, z);

                float w = Mathf.Lerp(4.5f, 7.5f, (float)rng.NextDouble());
                float d = Mathf.Lerp(4.5f, 7f, (float)rng.NextDouble());
                float h = Mathf.Lerp(3f, 5.5f, (float)rng.NextDouble());
                float yaw = (float)rng.NextDouble() * 40f - 20f;

                var shack = b.Shacks[rng.Next(b.Shacks.Length)];
                Vector3 baseCenter = new Vector3(x, ground - 1.2f, z);

                shack.AddTaperedBox(baseCenter, new Vector2(w, d), new Vector2(w, d), h, yaw);
                b.Roof.AddBox(baseCenter + new Vector3(0f, h + 0.15f, 0f),
                              new Vector3(w + 0.8f, 0.3f, d + 0.8f), yaw);

                // Ein Teil bekommt ein aufgesetztes zweites Geschoss
                if (rng.NextDouble() < 0.35)
                {
                    float w2 = w * 0.8f, d2 = d * 0.8f, h2 = Mathf.Lerp(2.5f, 4f, (float)rng.NextDouble());
                    var shack2 = b.Shacks[rng.Next(b.Shacks.Length)];
                    Vector3 c2 = baseCenter + new Vector3(0f, h + 0.3f, 0f);
                    shack2.AddTaperedBox(c2, new Vector2(w2, d2), new Vector2(w2, d2), h2, yaw + 8f);
                    b.Roof.AddBox(c2 + new Vector3(0f, h2 + 0.15f, 0f),
                                  new Vector3(w2 + 0.6f, 0.3f, d2 + 0.6f), yaw + 8f);
                }
            }
        }

        static void BuildPalms(Buckets b, System.Random rng)
        {
            for (float x = -HalfWidth + 7f; x < HalfWidth - 5f; x += 13f)
            {
                float jitter = (float)rng.NextDouble() * 3f - 1.5f;
                float z = BeachTo - 2.2f;
                float height = Mathf.Lerp(7f, 11f, (float)rng.NextDouble());
                AddPalm(b, new Vector3(x + jitter, GroundY, z), height,
                        (float)rng.NextDouble() * 360f);
            }
        }

        static void AddPalm(Buckets b, Vector3 basePos, float height, float yaw)
        {
            // Stamm aus drei sich verjuengenden Segmenten, leicht versetzt
            int segments = 3;
            float segH = height / segments;
            Vector3 pos = basePos;
            float wide = 0.75f;

            for (int i = 0; i < segments; i++)
            {
                float next = wide * 0.72f;
                float lean = i * 0.22f;
                b.PalmTrunk.AddTaperedBox(pos, new Vector2(wide, wide), new Vector2(next, next),
                                          segH, yaw + i * 12f);
                pos += new Vector3(Mathf.Cos(yaw * Mathf.Deg2Rad) * lean, segH,
                                   Mathf.Sin(yaw * Mathf.Deg2Rad) * lean);
                wide = next;
            }

            // Wedel
            const int fronds = 8;
            for (int i = 0; i < fronds; i++)
            {
                float a = (360f / fronds) * i + yaw;
                float rad = a * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                Vector3 p0 = pos;
                Vector3 mid = pos + dir * 2.0f + Vector3.up * 0.7f;
                Vector3 tip = pos + dir * 4.1f - Vector3.up * 1.3f;

                Vector3 a0 = p0 + side * 0.35f, b0 = p0 - side * 0.35f;
                Vector3 a1 = mid + side * 0.5f, b1 = mid - side * 0.5f;

                // beidseitig sichtbar
                b.PalmLeaf.AddQuad(a0, a1, b1, b0);
                b.PalmLeaf.AddQuad(b0, b1, a1, a0);
                b.PalmLeaf.AddTriangle(a1, tip, b1);
                b.PalmLeaf.AddTriangle(b1, tip, a1);
            }
        }

        // ------------------------------------------------------------------
        // Hilfsmittel
        // ------------------------------------------------------------------

        static void Flat(MeshBucket bucket, float x0, float x1, float z0, float z1, float y)
        {
            bucket.AddQuad(
                new Vector3(x0, y, z1),
                new Vector3(x1, y, z1),
                new Vector3(x1, y, z0),
                new Vector3(x0, y, z0));
        }

        static IEnumerable<(float, float)> Zip(float[] a, float[] b)
        {
            for (int i = 0; i < Mathf.Min(a.Length, b.Length); i++)
                yield return (a[i], b[i]);
        }

        static GameObject ReplaceRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            return new GameObject(RootName);
        }

        static void EnsureDir(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(full))
            {
                Directory.CreateDirectory(full);
                AssetDatabase.Refresh();
            }
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

        // ------------------------------------------------------------------
        // Farbpalette
        // ------------------------------------------------------------------

        class Buckets
        {
            public readonly MeshBucket Sea = new MeshBucket("Sea", Hex("#1E6C86"), 0.85f);
            public readonly MeshBucket Sand = new MeshBucket("Sand", Hex("#E2CFA6"));
            public readonly MeshBucket Asphalt = new MeshBucket("Asphalt", Hex("#3B3B3E"));
            public readonly MeshBucket RoadLine = new MeshBucket("RoadLine", Hex("#D8D2BE"));
            public readonly MeshBucket Sidewalk = new MeshBucket("Sidewalk", Hex("#C7C1B2"));
            public readonly MeshBucket Wall = new MeshBucket("SeaWall", Hex("#B3AC9B"));
            public readonly MeshBucket Hill = new MeshBucket("Hill", Hex("#4E6B3A"));
            public readonly MeshBucket Roof = new MeshBucket("Roof", Hex("#6B6259"));
            public readonly MeshBucket PalmTrunk = new MeshBucket("PalmTrunk", Hex("#7A5E3E"));
            public readonly MeshBucket PalmLeaf = new MeshBucket("PalmLeaf", Hex("#3F7A3A"));

            public readonly MeshBucket[] BuildingWalls =
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
                    yield return Sea;
                    yield return Sand;
                    yield return Asphalt;
                    yield return RoadLine;
                    yield return Sidewalk;
                    yield return Wall;
                    yield return Hill;
                    yield return Roof;
                    yield return PalmTrunk;
                    yield return PalmLeaf;
                    foreach (var m in BuildingWalls) yield return m;
                    foreach (var m in Shacks) yield return m;
                }
            }
        }
    }
}
