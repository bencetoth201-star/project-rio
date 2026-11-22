using System.IO;
using Rio.Player;
using Rio.WorldGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Rio.PlayerGen
{
    /// <summary>
    /// Baut die spielbare Block-Figur: klotzige Teile im selben flach
    /// schattierten Stil wie die Welt, Pivots an Schulter und Huefte,
    /// dazu CharacterController, Steuerung und die Kamera dahinter.
    ///
    /// Menue: Tools / Rio / Block-Figur bauen
    /// </summary>
    public static class BlockCharacterBuilder
    {
        const string RootName = "Player";
        const string MaterialDir = "Assets/_Project/Materials/Player";
        const string MeshDir = "Assets/_Project/Generated/Player";

        // Masse in Metern. Die Figur ist rund 1,80 m hoch.
        const float HipY = 0.86f;
        const float ShoulderY = 1.36f;

        // Auf der Strandpromenade: von dort schaut man ueber Strasse und
        // Haeuser bis zum Morro - und mit einer halben Drehung aufs Meer.
        static readonly Vector3 Spawn = new Vector3(0f, 4f, -6.5f);

        [MenuItem("Tools/Rio/Block-Figur bauen", priority = 1)]
        public static void Build()
        {
            EnsureDir(MaterialDir);
            EnsureDir(MeshDir);

            Material skin = Mat("Skin", "#C98D5E");
            Material shirt = Mat("Shirt", "#2FA39B");
            Material shorts = Mat("Shorts", "#2C3A4C");
            Material shoes = Mat("Shoes", "#EDE8DC");
            Material hair = Mat("Hair", "#241C18");

            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject(RootName);
            root.transform.position = Spawn;

            // ---- Rumpf: dreht nicht mit, haengt nur am Koerper ----
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);

            Part(body.transform, "Shorts", shorts, new Vector3(0f, HipY + 0.08f, 0f), b =>
                b.AddBox(Vector3.zero, new Vector3(0.46f, 0.30f, 0.30f)));

            Part(body.transform, "Torso", shirt, new Vector3(0f, 1.18f, 0f), b =>
                b.AddBox(Vector3.zero, new Vector3(0.48f, 0.46f, 0.28f)));

            Part(body.transform, "Neck", skin, new Vector3(0f, 1.44f, 0f), b =>
                b.AddBox(Vector3.zero, new Vector3(0.16f, 0.09f, 0.16f)));

            Part(body.transform, "Head", skin, new Vector3(0f, 1.63f, 0f), b =>
            {
                b.AddBox(Vector3.zero, new Vector3(0.32f, 0.32f, 0.30f));
                // Nase, damit man die Blickrichtung sieht
                b.AddBox(new Vector3(0f, -0.02f, 0.165f), new Vector3(0.07f, 0.07f, 0.05f));
            });

            Part(body.transform, "Hair", hair, new Vector3(0f, 1.78f, 0f), b =>
            {
                b.AddBox(Vector3.zero, new Vector3(0.34f, 0.08f, 0.32f));
                b.AddBox(new Vector3(0f, -0.07f, -0.14f), new Vector3(0.34f, 0.12f, 0.06f));
            });

            // ---- Gliedmassen: eigener Pivot am Gelenk ----
            Transform armL = Limb(root.transform, "ArmL", new Vector3(-0.305f, ShoulderY, 0f));
            Transform armR = Limb(root.transform, "ArmR", new Vector3(0.305f, ShoulderY, 0f));
            foreach (Transform arm in new[] { armL, armR })
            {
                Part(arm, arm.name + "_Skin", skin, new Vector3(0f, -0.27f, 0f), b =>
                    b.AddBox(Vector3.zero, new Vector3(0.145f, 0.52f, 0.17f)));
                Part(arm, arm.name + "_Hand", skin, new Vector3(0f, -0.59f, 0f), b =>
                    b.AddBox(Vector3.zero, new Vector3(0.155f, 0.13f, 0.185f)));
            }

            Transform legL = Limb(root.transform, "LegL", new Vector3(-0.13f, HipY, 0f));
            Transform legR = Limb(root.transform, "LegR", new Vector3(0.13f, HipY, 0f));
            foreach (Transform leg in new[] { legL, legR })
            {
                Part(leg, leg.name + "_Skin", skin, new Vector3(0f, -0.36f, 0f), b =>
                    b.AddBox(Vector3.zero, new Vector3(0.19f, 0.72f, 0.22f)));
                Part(leg, leg.name + "_Shoe", shoes, new Vector3(0f, -0.785f, 0.035f), b =>
                    b.AddBox(Vector3.zero, new Vector3(0.21f, 0.13f, 0.30f)));
            }

            // ---- Komponenten ----
            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.78f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.89f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;

            var rig = body.AddComponent<BlockCharacterRig>();
            rig.body = body.transform;
            rig.leftArm = armL;
            rig.rightArm = armR;
            rig.leftLeg = legL;
            rig.rightLeg = legR;

            var move = root.AddComponent<BlockCharacterController>();

            // ---- Kamera ----
            Camera cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                var follow = cam.GetComponent<ThirdPersonCamera>();
                if (follow == null) follow = cam.gameObject.AddComponent<ThirdPersonCamera>();
                follow.target = root.transform;
                move.cameraTransform = cam.transform;
                cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);

                // Startblick: hinter der Figur, leicht von oben, Richtung Stadt.
                // ThirdPersonCamera uebernimmt diesen Winkel beim Start.
                Quaternion look = Quaternion.Euler(14f, 0f, 0f);
                cam.transform.SetPositionAndRotation(
                    root.transform.position + follow.targetOffset + look * Vector3.back * follow.distance,
                    look);
            }
            else
            {
                Debug.LogWarning("[Rio] Keine Kamera in der Szene gefunden - " +
                                 "die Figur laeuft, aber niemand schaut zu.");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log($"[Rio] Block-Figur gebaut bei {Spawn}. " +
                      "WASD laufen, Shift rennen, Leertaste springen, Maus schauen, Escape Mauszeiger frei.");
        }

        // ------------------------------------------------------------------

        static Transform Limb(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        static void Part(Transform parent, string name, Material material,
                         Vector3 localPosition, System.Action<MeshBucket> fill)
        {
            var bucket = new MeshBucket(name, Color.white);
            fill(bucket);

            Mesh mesh = bucket.BuildMesh();
            string path = $"{MeshDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static Material Mat(string name, string hex)
        {
            string path = $"{MaterialDir}/M_Player{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = "M_Player" + name };
                AssetDatabase.CreateAsset(mat, path);
            }

            ColorUtility.TryParseHtmlString(hex, out Color color);
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.06f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.06f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
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
    }
}
