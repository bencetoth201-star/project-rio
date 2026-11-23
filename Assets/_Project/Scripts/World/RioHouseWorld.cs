using System;
using UnityEngine;

namespace Rio.World
{
    /// <summary>
    /// Alle Hausplatzierungen der Welt, nach Chunks sortiert.
    /// Wird im Editor vom Planer erzeugt und zur Laufzeit vom Streamer gelesen.
    ///
    /// Bewusst eine flache Liste plus Start/Anzahl je Chunk statt verschachtelter
    /// Listen: das ist ein zusammenhaengender Speicherblock, laedt schnell und
    /// erzeugt keinen Muell beim Durchlaufen.
    /// </summary>
    public class RioHouseWorld : ScriptableObject
    {
        [Serializable]
        public struct Placement
        {
            public Vector3 position;   // Mitte des Grundstuecks, auf dem Boden
            public float yaw;          // Drehung um Y, Vorderseite zeigt nach +Z
            public int model;          // Index in Models
            public int colorSeed;      // Seed fuer RioHausFarben
        }

        [Header("Modelle")]
        public GameObject[] models;
        public Vector2[] modelLot;     // Grundstueck B x T je Modell
        public float[] modelHeight;    // Hoehe je Modell, fuer Kollider und Fern-LOD
        public bool[] modelIsRoad;     // Strassenkacheln bekommen keinen Quader-Kollider

        [Header("Chunk-Raster")]
        public float chunkSize = 300f;
        public Vector2 worldMin;
        public int chunksX;
        public int chunksZ;

        [Header("Daten")]
        public int[] chunkStart;       // Index in Placements, Laenge chunksX*chunksZ
        public int[] chunkCount;
        public Placement[] placements;

        public int ChunkIndex(int cx, int cz) => cz * chunksX + cx;

        public bool InRange(int cx, int cz) =>
            cx >= 0 && cz >= 0 && cx < chunksX && cz < chunksZ;

        public Vector2 ChunkCenter(int cx, int cz) =>
            worldMin + new Vector2((cx + 0.5f) * chunkSize, (cz + 0.5f) * chunkSize);

        public void WorldToChunk(Vector3 p, out int cx, out int cz)
        {
            cx = Mathf.FloorToInt((p.x - worldMin.x) / chunkSize);
            cz = Mathf.FloorToInt((p.z - worldMin.y) / chunkSize);
        }
    }
}
