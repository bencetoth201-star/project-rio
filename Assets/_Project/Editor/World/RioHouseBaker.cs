using System.Collections.Generic;
using System.IO;
using Rio.World;
using UnityEditor;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Macht aus den Platzierungen zwei Dinge:
    /// die Datentabelle fuer den Streamer, und die groben Kloetze fuer die Ferne.
    ///
    /// Beides entsteht aus derselben Liste. Deshalb steht in der Ferne genau
    /// dort ein Klotz, wo in der Naehe das echte Haus auftaucht - sonst wuerde
    /// die Stadt beim Naeherkommen springen.
    /// </summary>
    public static class RioHouseBaker
    {
        const string WorldAssetPath = "Assets/_Project/Generated/World/RioHouseWorld.asset";
        const string LodMeshPath = "Assets/_Project/Generated/World/RioHouseLod.asset";
        const string ModelDir = "Assets/RioHaeuser/Modelle";

        public const float ChunkSize = 300f;

        public static RioHouseWorld Bake(RioHousePlanner planner, GameObject worldRoot,
                                         float minX, float maxX, float minZ, float maxZ,
                                         Material[] wallMats, Material roofMat,
                                         out int lodObjects)
        {
            int chunksX = Mathf.CeilToInt((maxX - minX) / ChunkSize);
            int chunksZ = Mathf.CeilToInt((maxZ - minZ) / ChunkSize);
            var worldMin = new Vector2(minX, minZ);

            // --- Platzierungen nach Chunk sortieren ---
            var byChunk = new List<RioHouseWorld.Placement>[chunksX * chunksZ];
            foreach (var p in planner.Placements)
            {
                int cx = Mathf.Clamp(Mathf.FloorToInt((p.position.x - minX) / ChunkSize), 0, chunksX - 1);
                int cz = Mathf.Clamp(Mathf.FloorToInt((p.position.z - minZ) / ChunkSize), 0, chunksZ - 1);
                int idx = cz * chunksX + cx;
                (byChunk[idx] ??= new List<RioHouseWorld.Placement>()).Add(p);
            }

            var flat = new List<RioHouseWorld.Placement>(planner.Count);
            var starts = new int[chunksX * chunksZ];
            var counts = new int[chunksX * chunksZ];

            for (int i = 0; i < byChunk.Length; i++)
            {
                starts[i] = flat.Count;
                if (byChunk[i] != null) { flat.AddRange(byChunk[i]); counts[i] = byChunk[i].Count; }
            }

            // --- Asset schreiben ---
            var asset = ScriptableObject.CreateInstance<RioHouseWorld>();
            asset.chunkSize = ChunkSize;
            asset.worldMin = worldMin;
            asset.chunksX = chunksX;
            asset.chunksZ = chunksZ;
            asset.chunkStart = starts;
            asset.chunkCount = counts;
            asset.placements = flat.ToArray();

            int n = RioHausDaten.Haeuser.Length;
            asset.models = new GameObject[n];
            asset.modelLot = new Vector2[n];
            asset.modelHeight = new float[n];
            for (int i = 0; i < n; i++)
            {
                string path = $"{ModelDir}/{RioHausDaten.Haeuser[i].slug}.glb";
                asset.models[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.models[i] == null)
                    Debug.LogWarning($"[Rio] Modell fehlt: {path}");
                asset.modelLot[i] = RioHousePlanner.Lot(i);
                asset.modelHeight[i] = RioHousePlanner.Height(i);
            }

            EnsureDir(Path.GetDirectoryName(WorldAssetPath).Replace('\\', '/'));
            AssetDatabase.DeleteAsset(WorldAssetPath);
            AssetDatabase.CreateAsset(asset, WorldAssetPath);

            // --- Grobe Kloetze fuer die Ferne, ein Objekt je Chunk ---
            lodObjects = BuildFarLod(worldRoot, asset, byChunk, chunksX, wallMats, roofMat);

            AssetDatabase.SaveAssets();
            return asset;
        }

        static int BuildFarLod(GameObject worldRoot, RioHouseWorld asset,
                               List<RioHouseWorld.Placement>[] byChunk, int chunksX,
                               Material[] wallMats, Material roofMat)
        {
            var lodRoot = new GameObject("FarLod");
            lodRoot.transform.SetParent(worldRoot.transform, false);

            AssetDatabase.DeleteAsset(LodMeshPath);
            Mesh libraryMain = null;
            int objects = 0;

            for (int idx = 0; idx < byChunk.Length; idx++)
            {
                var list = byChunk[idx];
                if (list == null || list.Count == 0) continue;

                int cx = idx % chunksX;
                int cz = idx / chunksX;

                var walls = new MeshBucket[wallMats.Length];
                for (int i = 0; i < walls.Length; i++)
                    walls[i] = new MeshBucket($"W{i}", Color.white);
                var roof = new MeshBucket("R", Color.white);

                foreach (var p in list)
                {
                    Vector2 lot = asset.modelLot[p.model];
                    float h = asset.modelHeight[p.model];
                    var center = new Vector3(p.position.x, p.position.y + h * 0.5f, p.position.z);

                    // Etwas schmaler als das Grundstueck: so wirkt die Ferne
                    // nicht zugebaut und die Kante stimmt beim Umschalten.
                    var size = new Vector3(lot.x * 0.86f, h, lot.y * 0.86f);

                    walls[p.model % walls.Length].AddBox(center, size, p.yaw);
                    roof.AddBox(new Vector3(center.x, p.position.y + h + 0.3f, center.z),
                                new Vector3(size.x + 0.6f, 0.6f, size.z + 0.6f), p.yaw);
                }

                var chunkGo = new GameObject($"LOD_{cx}_{cz}");
                chunkGo.transform.SetParent(lodRoot.transform, false);
                objects++;

                for (int i = 0; i <= walls.Length; i++)
                {
                    var bucket = i < walls.Length ? walls[i] : roof;
                    if (bucket.IsEmpty) continue;

                    Mesh mesh = bucket.BuildMesh();
                    mesh.name = $"LOD_{cx}_{cz}_{bucket.Name}";
                    if (libraryMain == null) { libraryMain = mesh; AssetDatabase.CreateAsset(mesh, LodMeshPath); }
                    else AssetDatabase.AddObjectToAsset(mesh, libraryMain);

                    var part = new GameObject(bucket.Name);
                    part.transform.SetParent(chunkGo.transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = mesh;
                    part.AddComponent<MeshRenderer>().sharedMaterial =
                        i < walls.Length ? wallMats[i] : roofMat;
                    GameObjectUtility.SetStaticEditorFlags(part, StaticEditorFlags.BatchingStatic);
                }
            }

            return objects;
        }

        static void EnsureDir(string assetPath)
        {
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!Directory.Exists(full)) { Directory.CreateDirectory(full); AssetDatabase.Refresh(); }
        }
    }
}
