using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FishingSim.EditorTools
{
    public static partial class LakeSceneBuilder
    {
        static GameObject BuildTerrain(Transform parent)
        {
            var grassTexture = MakeTiledTexture("GrassTexture", new Color(0.18f, 0.36f, 0.1f), new Color(0.46f, 0.64f, 0.22f), new Color(0.62f, 0.66f, 0.28f), 5);
            var sandTexture = MakeTiledTexture("SandTexture", new Color(0.62f, 0.55f, 0.38f), new Color(0.8f, 0.72f, 0.52f), new Color(0.7f, 0.62f, 0.45f), 9);

            string materialPath = Root + "/Materials/Ground.mat";
            AssetDatabase.DeleteAsset(materialPath);
            var material = new Material(Shader.Find("FishingSim/Ground"));
            material.SetTexture("_GrassMap", grassTexture);
            material.SetTexture("_SandMap", sandTexture);
            AssetDatabase.CreateAsset(material, materialPath);

            var mesh = BuildTerrainMesh();
            var go = new GameObject("Terrain", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic);
            return go;
        }

        static Mesh BuildTerrainMesh()
        {
            const float minX = -160f, maxX = 160f, minZ = -60f, maxZ = 220f, step = 1.25f;
            int countX = Mathf.RoundToInt((maxX - minX) / step) + 1;
            int countZ = Mathf.RoundToInt((maxZ - minZ) / step) + 1;

            var vertices = new Vector3[countX * countZ];
            for (int z = 0; z < countZ; z++)
            {
                for (int x = 0; x < countX; x++)
                {
                    float wx = minX + x * step;
                    float wz = minZ + z * step;
                    vertices[z * countX + x] = new Vector3(wx, LakeTerrain.GroundHeight(wx, wz), wz);
                }
            }

            var triangles = new int[(countX - 1) * (countZ - 1) * 6];
            int t = 0;
            for (int z = 0; z < countZ - 1; z++)
            {
                for (int x = 0; x < countX - 1; x++)
                {
                    int i = z * countX + x;
                    triangles[t++] = i;
                    triangles[t++] = i + countX;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + 1;
                    triangles[t++] = i + countX;
                    triangles[t++] = i + countX + 1;
                }
            }

            var mesh = new Mesh { name = "LakeTerrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Directory.CreateDirectory(Root + "/Meshes");
            string path = Root + "/Meshes/LakeTerrain.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Texture2D MakeTiledTexture(string name, Color dark, Color light, Color dry, int seed)
        {
            string path = Root + "/Materials/" + name + ".asset";
            AssetDatabase.DeleteAsset(path);

            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var random = new System.Random(seed);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float large = TileableNoise(x, y, size, 0.03f);
                    float fine = TileableNoise(x, y, size, 0.14f);
                    float n = Mathf.SmoothStep(0f, 1f, large * 0.65f + fine * 0.35f);
                    var color = Color.Lerp(dark, light, n);
                    color = Color.Lerp(color, dry, Mathf.Clamp01((large - 0.6f) * 2f) * 0.5f);
                    float speckle = ((float)random.NextDouble() - 0.5f) * 0.08f;
                    texture.SetPixel(x, y, new Color(color.r + speckle, color.g + speckle, color.b + speckle * 0.5f, 1f));
                }
            }
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        static float TileableNoise(float x, float y, float size, float scale)
        {
            float sx = x / size, sy = y / size;
            float a = Mathf.PerlinNoise(x * scale, y * scale);
            float b = Mathf.PerlinNoise((x - size) * scale, y * scale);
            float c = Mathf.PerlinNoise((x - size) * scale, (y - size) * scale);
            float d = Mathf.PerlinNoise(x * scale, (y - size) * scale);
            return a * (1f - sx) * (1f - sy) + b * sx * (1f - sy) + c * sx * sy + d * (1f - sx) * sy;
        }

        static void BuildScenery(Transform parent, Dictionary<Material, Material> matCache)
        {
            var random = new System.Random(2024);

            var treeRegions = new[]
            {
                new Rect(-95f, -20f, 61f, 135f),
                new Rect(34f, -20f, 61f, 135f),
                new Rect(-34f, 68f, 68f, 47f),
                new Rect(-48f, -30f, 96f, 22f)
            };
            var surroundings = new[] { new Rect(-100f, -30f, 200f, 150f) };

            string[] trees = { "tree_oak", "tree_default", "tree_pineTallA", "tree_pineDefaultA", "tree_simple", "tree_detailed", "tree_fat", "tree_cone", "tree_pineTallB" };
            string[] rocks = { "rock_largeA", "rock_largeB", "rock_largeC", "rock_smallA", "rock_smallB", "rock_smallC", "stone_largeA", "stone_smallA" };
            string[] bushes = { "plant_bush", "plant_bushLarge", "plant_bushDetailed", "plant_bushSmall" };
            string[] grasses = { "grass", "grass_large", "grass_leafs" };
            string[] flowers = { "flower_purpleA", "flower_redA", "flower_yellowA", "flower_purpleB", "flower_yellowB" };
            string[] props = { "log", "log_large", "stump_old", "stump_round", "mushroom_redGroup" };

            Scatter(parent, matCache, random, trees, 150, treeRegions, 7f, 12f, true, 6f, 80f);
            Scatter(parent, matCache, random, rocks, 45, surroundings, 0.5f, 2.2f, false, -0.8f, 6f);
            Scatter(parent, matCache, random, bushes, 60, surroundings, 0.9f, 1.7f, false, 0.8f, 8f);
            Scatter(parent, matCache, random, props, 10, surroundings, 0.5f, 0.9f, false, 0.8f, 8f);
            Scatter(parent, matCache, random, grasses, 90, surroundings, 0.4f, 0.8f, false, 0.5f, 8f);
            Scatter(parent, matCache, random, grasses, 450, surroundings, 0.45f, 0.9f, false, 2.5f, 45f);
            Scatter(parent, matCache, random, flowers, 50, surroundings, 0.3f, 0.5f, false, 0.5f, 8f);
            Scatter(parent, matCache, random, flowers, 120, surroundings, 0.3f, 0.5f, false, 2.5f, 45f);

            foreach (var t in parent.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
        }

        static void Scatter(Transform parent, Dictionary<Material, Material> matCache, System.Random random, string[] names, int count, Rect[] regions, float minHeight, float maxHeight, bool weightByArea, float minShoreDistance, float maxShoreDistance)
        {
            var prefabs = new List<GameObject>();
            foreach (var n in names)
            {
                var prefab = ModelTools.Load(NatureDir + n + ".fbx");
                if (prefab != null) prefabs.Add(prefab);
            }
            if (prefabs.Count == 0) return;

            float totalArea = 0f;
            foreach (var r in regions) totalArea += weightByArea ? r.width * r.height : 1f;

            for (int i = 0; i < count; i++)
            {
                float pick = (float)random.NextDouble() * totalArea;
                Rect region = regions[regions.Length - 1];
                foreach (var r in regions)
                {
                    pick -= weightByArea ? r.width * r.height : 1f;
                    if (pick <= 0f) { region = r; break; }
                }

                float x = 0f, z = 0f;
                bool found = false;
                for (int attempt = 0; attempt < 400 && !found; attempt++)
                {
                    x = region.x + (float)random.NextDouble() * region.width;
                    z = region.y + (float)random.NextDouble() * region.height;
                    if (PlayerArea.Contains(new Vector2(x, z))) continue;
                    float distance = LakeTerrain.SignedDistance(x, z);
                    found = distance >= minShoreDistance && distance <= maxShoreDistance;
                }
                if (!found) continue;

                float height = minHeight + (float)random.NextDouble() * (maxHeight - minHeight);
                float yaw = (float)random.NextDouble() * 360f;
                var chosen = prefabs[random.Next(prefabs.Count)];

                var instance = ModelTools.PlaceOnGround(chosen, parent, new Vector3(x, LakeTerrain.GroundHeight(x, z), z), yaw, height);
                ModelTools.FixMaterials(instance, matCache);
            }
        }
    }
}
