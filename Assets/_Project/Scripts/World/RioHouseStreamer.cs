using System.Collections.Generic;
using UnityEngine;

namespace Rio.World
{
    /// <summary>
    /// Haelt um den Spieler herum die echten Hausmodelle vor und raeumt sie
    /// hinter ihm wieder weg. Weiter draussen bleiben die groben Kloetze
    /// stehen, damit die Skyline nicht abreisst.
    ///
    /// Zwei Radien statt einem: geladen wird ab <see cref="nearRadius"/>,
    /// entladen erst ab nearRadius + <see cref="hysteresis"/>. Ohne diesen
    /// Abstand wuerde ein Chunk an der Grenze bei jedem Schritt laden und
    /// entladen.
    ///
    /// Pro Frame wird nur eine begrenzte Zahl Haeuser erzeugt, sonst gibt es
    /// beim Betreten eines neuen Chunks einen Ruckler.
    /// </summary>
    public class RioHouseStreamer : MonoBehaviour
    {
        [Header("Daten")]
        public RioHouseWorld world;
        public Transform player;
        [Tooltip("Wurzel der groben Kloetze. Kinder heissen LOD_<cx>_<cz>.")]
        public Transform farLodRoot;

        [Header("Sichtweite")]
        [Tooltip("Ab hier stehen echte Haeuser.")]
        public float nearRadius = 420f;
        [Tooltip("Zusatzabstand fuers Entladen, gegen Flackern an der Grenze.")]
        public float hysteresis = 80f;

        [Header("Budget")]
        [Tooltip("Hoechstens so viele Haeuser pro Frame erzeugen.")]
        public int spawnsPerFrame = 25;
        [Tooltip("Sekunden zwischen zwei Pruefungen, welche Chunks dran sind.")]
        public float updateInterval = 0.25f;

        [Header("Kollision")]
        public bool addColliders = true;

        readonly Dictionary<int, GameObject> _loaded = new Dictionary<int, GameObject>();
        readonly Dictionary<int, Transform> _farLod = new Dictionary<int, Transform>();
        readonly Queue<int> _toLoad = new Queue<int>();
        readonly HashSet<int> _queued = new HashSet<int>();

        float _nextCheck;
        Transform _root;

        public int LoadedChunks => _loaded.Count;
        public int QueuedChunks => _toLoad.Count;

        void Awake()
        {
            _root = new GameObject("StreamedHouses").transform;
            _root.SetParent(transform, false);
            IndexFarLod();

            if (player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }
        }

        void IndexFarLod()
        {
            _farLod.Clear();
            if (farLodRoot == null || world == null) return;

            foreach (Transform child in farLodRoot)
            {
                // Name: LOD_<cx>_<cz>
                var parts = child.name.Split('_');
                if (parts.Length != 3) continue;
                if (!int.TryParse(parts[1], out int cx)) continue;
                if (!int.TryParse(parts[2], out int cz)) continue;
                _farLod[world.ChunkIndex(cx, cz)] = child;
            }
        }

        void Update()
        {
            if (world == null || world.placements == null || player == null) return;

            if (Time.time >= _nextCheck)
            {
                _nextCheck = Time.time + updateInterval;
                Refresh();
            }

            DrainQueue();
        }

        void Refresh()
        {
            Vector3 p = player.position;
            float loadSq = nearRadius * nearRadius;
            float unloadSq = (nearRadius + hysteresis) * (nearRadius + hysteresis);

            int reach = Mathf.CeilToInt((nearRadius + hysteresis) / world.chunkSize) + 1;
            world.WorldToChunk(p, out int pcx, out int pcz);

            // Laden, was nah genug ist
            for (int cx = pcx - reach; cx <= pcx + reach; cx++)
            for (int cz = pcz - reach; cz <= pcz + reach; cz++)
            {
                if (!world.InRange(cx, cz)) continue;
                int idx = world.ChunkIndex(cx, cz);
                if (world.chunkCount[idx] == 0) continue;
                if (_loaded.ContainsKey(idx) || _queued.Contains(idx)) continue;

                if (DistanceSq(p, cx, cz) <= loadSq)
                {
                    _toLoad.Enqueue(idx);
                    _queued.Add(idx);
                }
            }

            // Entladen, was zu weit weg ist
            if (_loaded.Count > 0)
            {
                var drop = new List<int>();
                foreach (var kv in _loaded)
                {
                    int idx = kv.Key;
                    int cx = idx % world.chunksX;
                    int cz = idx / world.chunksX;
                    if (DistanceSq(p, cx, cz) > unloadSq) drop.Add(idx);
                }
                foreach (int idx in drop) Unload(idx);
            }
        }

        /// <summary>Abstand vom Spieler zum naechsten Punkt des Chunk-Rechtecks.</summary>
        float DistanceSq(Vector3 p, int cx, int cz)
        {
            float minX = world.worldMin.x + cx * world.chunkSize;
            float minZ = world.worldMin.y + cz * world.chunkSize;
            float dx = Mathf.Max(minX - p.x, 0f, p.x - (minX + world.chunkSize));
            float dz = Mathf.Max(minZ - p.z, 0f, p.z - (minZ + world.chunkSize));
            return dx * dx + dz * dz;
        }

        void DrainQueue()
        {
            int budget = spawnsPerFrame;

            while (budget > 0 && _toLoad.Count > 0)
            {
                int idx = _toLoad.Peek();
                if (_loaded.ContainsKey(idx)) { _toLoad.Dequeue(); _queued.Remove(idx); continue; }

                budget -= LoadChunk(idx, budget);
                _toLoad.Dequeue();
                _queued.Remove(idx);
            }
        }

        int LoadChunk(int idx, int budget)
        {
            int start = world.chunkStart[idx];
            int count = world.chunkCount[idx];
            if (count == 0) return 0;

            var holder = new GameObject($"Chunk_{idx}");
            holder.transform.SetParent(_root, false);

            for (int i = 0; i < count; i++)
            {
                var pl = world.placements[start + i];
                var prefab = world.models[pl.model];
                if (prefab == null) continue;

                var go = Instantiate(prefab, pl.position, Quaternion.Euler(0f, pl.yaw, 0f), holder.transform);
                go.name = prefab.name;

                var farben = go.GetComponent<RioHausFarben>();
                if (farben == null) farben = go.AddComponent<RioHausFarben>();
                farben.seed = pl.colorSeed;

                if (addColliders)
                {
                    // Ein Quader statt MeshCollider auf jedem Teil: deutlich
                    // billiger und fuers Anstossen voellig ausreichend.
                    var lot = world.modelLot[pl.model];
                    float h = world.modelHeight[pl.model];
                    var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(lot.x, h, lot.y);
                    box.center = new Vector3(0f, h * 0.5f, 0f);
                }
            }

            _loaded[idx] = holder;
            SetFarLod(idx, false);
            return count;
        }

        void Unload(int idx)
        {
            if (_loaded.TryGetValue(idx, out var go) && go != null) Destroy(go);
            _loaded.Remove(idx);
            SetFarLod(idx, true);
        }

        void SetFarLod(int idx, bool visible)
        {
            if (_farLod.TryGetValue(idx, out var t) && t != null)
                t.gameObject.SetActive(visible);
        }

        void OnDisable()
        {
            foreach (var kv in _farLod)
                if (kv.Value != null) kv.Value.gameObject.SetActive(true);
        }
    }
}
