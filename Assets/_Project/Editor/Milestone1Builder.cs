using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FishingSim.EditorTools
{
    public static class LakeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/FishingLake.unity";

        [MenuItem("Fishing/Setup Rendering (URP)")]
        public static void SetupUrp()
        {
            string rendererPath = Root + "/Config/FishingRenderer.asset";
            string assetPath = Root + "/Config/FishingURP.asset";

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (asset == null)
            {
                var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rd, rendererPath);
                asset = UniversalRenderPipelineAsset.Create(rd);
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            GraphicsSettings.defaultRenderPipeline = asset;
            int original = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(original, false);
            AssetDatabase.SaveAssets();
            Debug.Log("URP asset assigned to Graphics and all Quality levels.");
        }

        [MenuItem("Fishing/Build Lake Scene")]
        public static void BuildScene()
        {
            SetupUrp();
            Directory.CreateDirectory(Root + "/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            int waterLayer = LayerMask.NameToLayer("Water");

            var grass = MakeMat("Grass", new Color(0.35f, 0.55f, 0.25f));
            var sand = MakeMat("LakeBed", new Color(0.55f, 0.5f, 0.35f));
            var water = MakeMat("Water", new Color(0.1f, 0.4f, 0.6f, 0.75f), transparent: true);
            var wood = MakeMat("Wood", new Color(0.45f, 0.3f, 0.15f));
            var rodMat = MakeMat("Rod", new Color(0.15f, 0.1f, 0.05f));
            var lureMat = MakeMat("Lure", new Color(0.9f, 0.15f, 0.1f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.sun = light;

            // Lake spans x in [-30, 30], z in [4, 64]; the player stands on a dock at z ~ 0.
            var env = new GameObject("Environment").transform;
            Box("Shore", env, new Vector3(0, -0.5f, -8), new Vector3(100, 1, 24), grass);
            Box("FarBank", env, new Vector3(0, -0.5f, 94), new Vector3(100, 1, 60), grass);
            Box("LeftBank", env, new Vector3(-65, -0.5f, 34), new Vector3(70, 1, 60), grass);
            Box("RightBank", env, new Vector3(65, -0.5f, 34), new Vector3(70, 1, 60), grass);
            Box("LakeBed", env, new Vector3(0, -3.5f, 34), new Vector3(60, 1, 60), sand);
            var waterGo = Box("WaterSurface", env, new Vector3(0, -0.55f, 34), new Vector3(60, 0.1f, 60), water);
            waterGo.layer = waterLayer;
            var wc = waterGo.GetComponent<BoxCollider>();
            wc.isTrigger = true;
            Box("Dock", env, new Vector3(0, -0.15f, 1.5f), new Vector3(3, 0.2f, 6), wood);

            var shore = new GameObject("ShorePoint").transform;
            shore.position = new Vector3(0, 0, 4);

            new GameObject("GameManager").AddComponent<GameManager>();

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0, 0.5f);
            var input = player.AddComponent<FishingInput>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.7f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            var look = player.AddComponent<FirstPersonLook>();
            var lookSo = new SerializedObject(look);
            lookSo.FindProperty("input").objectReferenceValue = input;
            lookSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            lookSo.ApplyModifiedPropertiesWithoutUndo();

            // Rod viewmodel; a default cylinder is 2 units tall, so its tip is at local y = 1.
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name = "Rod";
            Object.DestroyImmediate(rod.GetComponent<Collider>());
            rod.transform.SetParent(camGo.transform, false);
            rod.transform.localScale = new Vector3(0.03f, 1.1f, 0.03f);
            rod.transform.localPosition = new Vector3(0.3f, -0.25f, 0.9f);
            rod.transform.localRotation = Quaternion.Euler(70f, 0f, -5f);
            rod.GetComponent<Renderer>().sharedMaterial = rodMat;
            var tip = new GameObject("RodTip").transform;
            tip.SetParent(rod.transform, false);
            tip.localPosition = new Vector3(0, 1f, 0);

            // Lure prefab: kinematic at rest, continuous collision so it cannot tunnel through terrain.
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = "Lure";
            lure.transform.localScale = Vector3.one * 0.12f;
            lure.GetComponent<Renderer>().sharedMaterial = lureMat;
            var rb = lure.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(lure, Root + "/Prefabs/Lure.prefab");
            Object.DestroyImmediate(lure);
            var lureInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            lureInstance.transform.position = tip.position;

            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var chGo = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            chGo.transform.SetParent(canvasGo.transform, false);
            var ch = chGo.GetComponent<Image>();
            ch.rectTransform.sizeDelta = new Vector2(6, 6);
            ch.color = new Color(1, 1, 1, 0.8f);
            ch.raycastTarget = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("FishingLake scene built: " + ScenePath);
        }

        static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material MakeMat(string name, Color color, bool transparent = false)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Directory.CreateDirectory(Root + "/Materials");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
