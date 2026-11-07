using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FishingSim.EditorTools
{
    public static class ModelTools
    {
        const string ConvertedDir = "Assets/_Project/Materials/Converted";
        static int convertedCounter;

        public static GameObject Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static List<Vector3> WorldVertices(GameObject go)
        {
            var list = new List<Vector3>();
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                var matrix = filter.transform.localToWorldMatrix;
                foreach (var v in filter.sharedMesh.vertices) list.Add(matrix.MultiplyPoint3x4(v));
            }
            foreach (var skinned in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skinned.sharedMesh == null) continue;
                var matrix = skinned.transform.localToWorldMatrix;
                foreach (var v in skinned.sharedMesh.vertices) list.Add(matrix.MultiplyPoint3x4(v));
            }
            return list;
        }

        // Returns a root whose +Z axis runs along the model's longest axis, base at the origin, tip at z = length.
        // The thick end of the mesh (handle, fish head) is told apart from the thin end by averaging vertex spread.
        public static GameObject CreateAligned(GameObject prefab, string name, float length, bool thickEndIsBase)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            var vertices = WorldVertices(model);
            var root = new GameObject(name);
            if (vertices.Count == 0)
            {
                model.transform.SetParent(root.transform, false);
                return root;
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            foreach (var v in vertices) bounds.Encapsulate(v);

            Vector3 size = bounds.size;
            Vector3 axis = size.x >= size.y && size.x >= size.z ? Vector3.right : (size.y >= size.z ? Vector3.up : Vector3.forward);
            Vector3 center = bounds.center;

            float tMin = float.MaxValue, tMax = float.MinValue;
            foreach (var v in vertices)
            {
                float t = Vector3.Dot(v - center, axis);
                tMin = Mathf.Min(tMin, t);
                tMax = Mathf.Max(tMax, t);
            }

            float span = tMax - tMin;
            float lowSum = 0f, highSum = 0f;
            int lowCount = 0, highCount = 0;
            foreach (var v in vertices)
            {
                Vector3 rel = v - center;
                float t = Vector3.Dot(rel, axis);
                float radial = (rel - axis * t).magnitude;
                if (t < tMin + span * 0.2f) { lowSum += radial; lowCount++; }
                if (t > tMax - span * 0.2f) { highSum += radial; highCount++; }
            }

            float lowSpread = lowCount > 0 ? lowSum / lowCount : 0f;
            float highSpread = highCount > 0 ? highSum / highCount : 0f;
            bool lowIsThick = lowSpread >= highSpread;
            bool baseIsLow = lowIsThick == thickEndIsBase;
            if (!baseIsLow)
            {
                axis = -axis;
                float swappedMin = -tMax;
                tMax = -tMin;
                tMin = swappedMin;
            }

            Vector3 basePoint = center + axis * tMin;
            float scale = length / Mathf.Max(0.0001f, tMax - tMin);
            Quaternion rotation = Quaternion.FromToRotation(axis, Vector3.forward);

            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localRotation = rotation;
            model.transform.localPosition = -(rotation * (basePoint * scale));
            return root;
        }

        public static GameObject PlaceOnGround(GameObject prefab, Transform parent, Vector3 position, float yaw, float height)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            instance.transform.localScale = Vector3.one;

            var bounds = RendererBounds(instance);
            if (bounds.size.y > 0.0001f) instance.transform.localScale = Vector3.one * (height / bounds.size.y);

            bounds = RendererBounds(instance);
            instance.transform.position += Vector3.up * (position.y - bounds.min.y);
            return instance;
        }

        public static void FixMaterials(GameObject root, Dictionary<Material, Material> cache)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) continue;
                    if (source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline")) continue;

                    if (!cache.TryGetValue(source, out var converted))
                    {
                        converted = Convert(source);
                        cache[source] = converted;
                    }
                    materials[i] = converted;
                    changed = true;
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        static Material Convert(Material source)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.name = source.name;
            if (source.HasProperty("_Color")) material.SetColor("_BaseColor", source.GetColor("_Color"));
            if (source.HasProperty("_BaseColor")) material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            if (source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null)
                material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
            material.SetFloat("_Smoothness", 0.1f);

            Directory.CreateDirectory(ConvertedDir);
            AssetDatabase.CreateAsset(material, $"{ConvertedDir}/{source.name}_{convertedCounter++}.mat");
            return material;
        }
    }
}
