using System.Collections.Generic;
using UnityEngine;

namespace Rio.WorldGen
{
    /// <summary>
    /// Sammelt flach schattierte Geometrie fuer genau ein Material.
    /// Jedes Dreieck bekommt eigene Vertices und eine eigene Normale -
    /// das ist der Low-Poly-Look mit harten Kanten, wie bei den
    /// Auto-Renderings auf dem Whiteboard.
    ///
    /// Wichtig zur Wicklung: Unity nutzt im Uhrzeigersinn gewickelte
    /// Vorderseiten. Fuer a,b,c zeigt die Normale dann in Richtung
    /// Cross(b-a, c-a). Alle Add*-Methoden halten sich daran.
    /// </summary>
    public class MeshBucket
    {
        readonly List<Vector3> _verts = new List<Vector3>();
        readonly List<Vector3> _normals = new List<Vector3>();
        readonly List<int> _tris = new List<int>();

        public string Name { get; }
        public Color Color { get; }
        public float Smoothness { get; }

        public int TriangleCount => _tris.Count / 3;
        public bool IsEmpty => _tris.Count == 0;

        public MeshBucket(string name, Color color, float smoothness = 0.08f)
        {
            Name = name;
            Color = color;
            Smoothness = smoothness;
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-12f) return; // entartetes Dreieck ueberspringen
            n.Normalize();

            int i = _verts.Count;
            _verts.Add(a); _verts.Add(b); _verts.Add(c);
            _normals.Add(n); _normals.Add(n); _normals.Add(n);
            _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(a, b, c);
            AddTriangle(a, c, d);
        }

        /// <summary>Achsenparalleler Quader, optional um Y gedreht. center ist der Mittelpunkt.</summary>
        public void AddBox(Vector3 center, Vector3 size, float yawDeg = 0f)
        {
            Vector3 h = size * 0.5f;
            Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);

            Vector3 P(int sx, int sy, int sz) =>
                center + rot * new Vector3(sx * h.x, sy * h.y, sz * h.z);

            // +X
            AddQuad(P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), P(1, -1, -1));
            // -X
            AddQuad(P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), P(-1, -1, 1));
            // +Y
            AddQuad(P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), P(-1, 1, -1));
            // -Y
            AddQuad(P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
            // +Z
            AddQuad(P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1), P(1, -1, 1));
            // -Z
            AddQuad(P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), P(-1, -1, -1));
        }

        /// <summary>
        /// Quader mit verjuengter Oberseite - fuer Haeuser mit leichtem Rueckzug,
        /// Palmenstaemme und Felsen. topScale 1 = normaler Quader.
        /// </summary>
        public void AddTaperedBox(Vector3 baseCenter, Vector2 baseSize, Vector2 topSize,
                                  float height, float yawDeg = 0f)
        {
            Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 bh = new Vector3(baseSize.x, 0f, baseSize.y) * 0.5f;
            Vector3 th = new Vector3(topSize.x, 0f, topSize.y) * 0.5f;
            Vector3 up = new Vector3(0f, height, 0f);

            Vector3 B(int sx, int sz) => baseCenter + rot * new Vector3(sx * bh.x, 0f, sz * bh.z);
            Vector3 T(int sx, int sz) => baseCenter + up + rot * new Vector3(sx * th.x, 0f, sz * th.z);

            Vector3 b00 = B(-1, -1), b10 = B(1, -1), b11 = B(1, 1), b01 = B(-1, 1);
            Vector3 t00 = T(-1, -1), t10 = T(1, -1), t11 = T(1, 1), t01 = T(-1, 1);

            AddQuad(t10, t11, b11, b10); // +X
            AddQuad(t01, t00, b00, b01); // -X
            AddQuad(t01, t11, t10, t00); // oben
            AddQuad(b00, b10, b11, b01); // unten
            AddQuad(t11, t01, b01, b11); // +Z
            AddQuad(t00, t10, b10, b00); // -Z
        }

        /// <summary>Pyramidendach ueber einer Grundflaeche.</summary>
        public void AddPyramidRoof(Vector3 baseCenter, Vector2 baseSize, float height, float yawDeg = 0f)
        {
            Quaternion rot = Quaternion.Euler(0f, yawDeg, 0f);
            Vector3 h = new Vector3(baseSize.x, 0f, baseSize.y) * 0.5f;
            Vector3 B(int sx, int sz) => baseCenter + rot * new Vector3(sx * h.x, 0f, sz * h.z);

            Vector3 p00 = B(-1, -1), p10 = B(1, -1), p11 = B(1, 1), p01 = B(-1, 1);
            Vector3 apex = baseCenter + new Vector3(0f, height, 0f);

            AddTriangle(apex, p11, p10); // +X
            AddTriangle(apex, p00, p01); // -X
            AddTriangle(apex, p01, p11); // +Z
            AddTriangle(apex, p10, p00); // -Z
        }

        /// <summary>
        /// Gitter aus Quads mit Hoehenfunktion. Erzeugt die Huegelflanke und den Strand.
        /// </summary>
        public void AddHeightGrid(Vector3 origin, float sizeX, float sizeZ, int cellsX, int cellsZ,
                                  System.Func<float, float, float> heightAt)
        {
            float stepX = sizeX / cellsX;
            float stepZ = sizeZ / cellsZ;

            for (int ix = 0; ix < cellsX; ix++)
            {
                for (int iz = 0; iz < cellsZ; iz++)
                {
                    float x0 = origin.x + ix * stepX;
                    float x1 = x0 + stepX;
                    float z0 = origin.z + iz * stepZ;
                    float z1 = z0 + stepZ;

                    Vector3 v00 = new Vector3(x0, origin.y + heightAt(x0, z0), z0);
                    Vector3 v10 = new Vector3(x1, origin.y + heightAt(x1, z0), z0);
                    Vector3 v11 = new Vector3(x1, origin.y + heightAt(x1, z1), z1);
                    Vector3 v01 = new Vector3(x0, origin.y + heightAt(x0, z1), z1);

                    // Normale soll nach oben zeigen
                    AddQuad(v01, v11, v10, v00);
                }
            }
        }

        public Mesh BuildMesh()
        {
            var mesh = new Mesh { name = Name };
            if (_verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(_verts);
            mesh.SetNormals(_normals);
            mesh.SetTriangles(_tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
