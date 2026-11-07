using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace FishingSim.EditorTools
{
    public static partial class LakeSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/FishingLake.unity";
        const string NatureDir = "Assets/ThirdParty/KenneyNature/";
        const string SurvivalDir = "Assets/ThirdParty/KenneySurvival/";
        const string RodPath = "Assets/ThirdParty/FishinRod/FishingRod_Lvl2.fbx";
        static readonly Rect PlayerArea = new Rect(-7f, -10f, 14f, 30f);

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

            asset.supportsCameraDepthTexture = true;
            asset.shadowDistance = 80f;
            EditorUtility.SetDirty(asset);
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

            var water = MakeWaterMat();
            var wood = MakeMat("Wood", new Color(0.45f, 0.3f, 0.15f));
            var rodMat = MakeMat("Rod", new Color(0.15f, 0.1f, 0.05f));
            var lureMat = MakeMat("Lure", new Color(0.9f, 0.15f, 0.1f));

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.color = new Color(1f, 0.92f, 0.78f);
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(28f, 160f, 0f);
            RenderSettings.sun = light;

            var sky = new Material(Shader.Find("FishingSim/Sky"));
            AssetDatabase.DeleteAsset(Root + "/Materials/Sky.mat");
            AssetDatabase.CreateAsset(sky, Root + "/Materials/Sky.mat");
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.7f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.68f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.4f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.94f);

            CreatePostProcessing();

            // The lake outline and bed come from LakeTerrain; the water box is bigger than the lake and is
            // hidden by the terrain wherever the ground rises above the water level.
            var env = new GameObject("Environment").transform;
            BuildTerrain(env);
            var waterGo = Box("WaterSurface", env, new Vector3(0, -0.55f, 36), new Vector3(120, 0.1f, 100), water);
            waterGo.layer = waterLayer;
            var wc = waterGo.GetComponent<BoxCollider>();
            wc.isTrigger = true;
            // thick trigger (3 m deep, top flush with the surface) so a fast lure cannot tunnel through it
            wc.size = new Vector3(1f, 30f, 1f);
            wc.center = new Vector3(0f, -14.5f, 0f);
            var matCache = new Dictionary<Material, Material>();
            BuildDock(env, wood, matCache);
            BuildScenery(new GameObject("Scenery").transform, matCache);

            var shore = new GameObject("ShorePoint").transform;
            shore.position = new Vector3(0, 0, 6);

            new GameObject("GameManager").AddComponent<GameManager>();

            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 0.05f, 6f);
            var input = player.AddComponent<FishingInput>();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0, 1.7f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var look = player.AddComponent<FirstPersonLook>();
            var lookSo = new SerializedObject(look);
            lookSo.FindProperty("input").objectReferenceValue = input;
            lookSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            lookSo.ApplyModifiedPropertiesWithoutUndo();

            Transform tip;
            var rodPrefab = ModelTools.Load(RodPath);
            if (rodPrefab != null)
            {
                const float rodLength = 1.7f;
                var rodRoot = ModelTools.CreateAligned(rodPrefab, "Rod", rodLength, true);
                ModelTools.FixMaterials(rodRoot, matCache);
                foreach (var r in rodRoot.GetComponentsInChildren<Renderer>())
                    r.shadowCastingMode = ShadowCastingMode.Off;
                rodRoot.transform.SetParent(camGo.transform, false);
                rodRoot.transform.localPosition = new Vector3(0.3f, -0.55f, 0.3f);
                rodRoot.transform.localRotation = Quaternion.Euler(-22f, -3f, 180f);
                tip = new GameObject("RodTip").transform;
                tip.SetParent(rodRoot.transform, false);
                tip.localPosition = new Vector3(0f, 0f, rodLength);
            }
            else
            {
                var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rod.name = "Rod";
                Object.DestroyImmediate(rod.GetComponent<Collider>());
                rod.transform.SetParent(camGo.transform, false);
                rod.transform.localScale = new Vector3(0.03f, 1.1f, 0.03f);
                rod.transform.localPosition = new Vector3(0.3f, -0.25f, 0.9f);
                rod.transform.localRotation = Quaternion.Euler(70f, 0f, -5f);
                rod.GetComponent<Renderer>().sharedMaterial = rodMat;
                tip = new GameObject("RodTip").transform;
                tip.SetParent(rod.transform, false);
                tip.localPosition = new Vector3(0, 1f, 0);
            }

            // Lure prefab: kinematic at rest, continuous collision so it cannot tunnel through terrain.
            var lure = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lure.name = "Lure";
            lure.transform.localScale = Vector3.one * 0.12f;
            lure.GetComponent<Renderer>().sharedMaterial = lureMat;
            var rb = lure.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            lure.AddComponent<LureController>();
            Directory.CreateDirectory(Root + "/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(lure, Root + "/Prefabs/Lure.prefab");
            Object.DestroyImmediate(lure);
            var lureInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            lureInstance.transform.position = tip.position;

            var castingConfig = LoadOrCreate<CastingConfig>(Root + "/Config/CastingConfig.asset");
            var caster = player.AddComponent<CastingController>();
            var casterSo = new SerializedObject(caster);
            casterSo.FindProperty("input").objectReferenceValue = input;
            casterSo.FindProperty("lure").objectReferenceValue = lureInstance.GetComponent<LureController>();
            casterSo.FindProperty("cameraTransform").objectReferenceValue = camGo.transform;
            casterSo.FindProperty("rodTip").objectReferenceValue = tip;
            casterSo.FindProperty("config").objectReferenceValue = castingConfig;
            casterSo.ApplyModifiedPropertiesWithoutUndo();

            var lineMat = MakeMat("Line", new Color(0.9f, 0.9f, 0.9f));
            var lineGo = new GameObject("FishingLine");
            var lr = lineGo.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.008f;
            lr.sharedMaterial = lineMat;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var lineSo = new SerializedObject(lineGo.AddComponent<FishingLineRenderer>());
            lineSo.FindProperty("rodTip").objectReferenceValue = tip;
            lineSo.FindProperty("lure").objectReferenceValue = lureInstance.transform;
            lineSo.ApplyModifiedPropertiesWithoutUndo();

            var biteConfig = LoadOrCreate<BiteConfig>(Root + "/Config/BiteConfig.asset");
            var fishConfig = LoadOrCreate<FishConfig>(Root + "/Config/FishConfig.asset");
            var fightConfig = LoadOrCreate<FightConfig>(Root + "/Config/FightConfig.asset");

            var fishLargePrefab = ModelTools.Load(SurvivalDir + "fish-large.fbx");
            Transform fish;
            if (fishLargePrefab != null)
            {
                var hookedFish = ModelTools.CreateAligned(fishLargePrefab, "Fish", 0.9f, false);
                ModelTools.FixMaterials(hookedFish, matCache);
                fish = hookedFish.transform;
            }
            else
            {
                var fishMat = MakeMat("Fish", new Color(0.95f, 0.5f, 0.15f));
                fish = new GameObject("Fish").transform;
                var fishBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(fishBody.GetComponent<Collider>());
                fishBody.transform.SetParent(fish, false);
                fishBody.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                fishBody.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
                fishBody.GetComponent<Renderer>().sharedMaterial = fishMat;
            }

            var fishSmallPrefab = ModelTools.Load(SurvivalDir + "fish.fbx");
            if (fishSmallPrefab != null && fishLargePrefab != null)
            {
                var school = new GameObject("AmbientFish");
                var schoolRandom = new System.Random(77);
                for (int i = 0; i < 14; i++)
                {
                    bool large = i % 3 == 0;
                    float length = large ? 0.9f + (float)schoolRandom.NextDouble() * 0.4f : 0.4f + (float)schoolRandom.NextDouble() * 0.3f;
                    var swimmer = ModelTools.CreateAligned(large ? fishLargePrefab : fishSmallPrefab, "AmbientFish " + i, length, false);
                    ModelTools.FixMaterials(swimmer, matCache);
                    swimmer.transform.SetParent(school.transform, false);
                }
                school.AddComponent<AmbientFish>();
            }

            var systems = new GameObject("FishingSystems");

            var dotMat = MakeSpriteMat("SplashDot", MakeSoftDot());
            var rippleMat = MakeSpriteMat("Ripple", null);
            var splash = CreateSplash(dotMat);

            var biteSystem = systems.AddComponent<BiteSystem>();
            var biteSo = new SerializedObject(biteSystem);
            biteSo.FindProperty("lure").objectReferenceValue = lureInstance.GetComponent<LureController>();
            biteSo.FindProperty("config").objectReferenceValue = biteConfig;
            biteSo.ApplyModifiedPropertiesWithoutUndo();

            var fishAI = systems.AddComponent<FishAIController>();
            var fishAiSo = new SerializedObject(fishAI);
            fishAiSo.FindProperty("config").objectReferenceValue = fishConfig;
            fishAiSo.ApplyModifiedPropertiesWithoutUndo();

            var fight = systems.AddComponent<FishFightController>();
            var fightSo = new SerializedObject(fight);
            fightSo.FindProperty("input").objectReferenceValue = input;
            fightSo.FindProperty("lure").objectReferenceValue = lureInstance.GetComponent<LureController>();
            fightSo.FindProperty("fishAI").objectReferenceValue = fishAI;
            fightSo.FindProperty("line").objectReferenceValue = lineGo.GetComponent<FishingLineRenderer>();
            fightSo.FindProperty("fish").objectReferenceValue = fish;
            fightSo.FindProperty("shorePoint").objectReferenceValue = shore;
            fightSo.FindProperty("config").objectReferenceValue = fightConfig;
            fightSo.ApplyModifiedPropertiesWithoutUndo();

            var effects = systems.AddComponent<WaterEffects>();
            var effectsSo = new SerializedObject(effects);
            effectsSo.FindProperty("lure").objectReferenceValue = lureInstance.GetComponent<LureController>();
            effectsSo.FindProperty("bite").objectReferenceValue = biteSystem;
            effectsSo.FindProperty("splash").objectReferenceValue = splash;
            effectsSo.FindProperty("rippleMaterial").objectReferenceValue = rippleMat;
            effectsSo.ApplyModifiedPropertiesWithoutUndo();

            var schoolObject = GameObject.Find("AmbientFish");
            if (schoolObject != null)
            {
                var ambientSo = new SerializedObject(schoolObject.GetComponent<AmbientFish>());
                ambientSo.FindProperty("lure").objectReferenceValue = lureInstance.GetComponent<LureController>();
                ambientSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var chGo = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            chGo.transform.SetParent(canvasGo.transform, false);
            var ch = chGo.GetComponent<Image>();
            ch.rectTransform.sizeDelta = new Vector2(6, 6);
            ch.color = new Color(1, 1, 1, 0.8f);
            ch.raycastTarget = false;

            var barBack = MakeImage("PowerBar", canvasGo.transform, new Color(0, 0, 0, 0.5f));
            barBack.rectTransform.anchorMin = barBack.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            barBack.rectTransform.anchoredPosition = new Vector2(0, 120);
            barBack.rectTransform.sizeDelta = new Vector2(420, 28);
            var barFill = MakeImage("Fill", barBack.transform, new Color(0.95f, 0.75f, 0.1f));
            Stretch(barFill.rectTransform, 4f);
            barFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            barFill.rectTransform.localScale = new Vector3(0f, 1f, 1f);

            var stateLabel = MakeText("StateLabel", canvasGo.transform, 28, TextAnchor.UpperLeft);
            stateLabel.rectTransform.anchorMin = stateLabel.rectTransform.anchorMax = new Vector2(0f, 1f);
            stateLabel.rectTransform.pivot = new Vector2(0f, 1f);
            stateLabel.rectTransform.anchoredPosition = new Vector2(24, -24);
            stateLabel.rectTransform.sizeDelta = new Vector2(500, 40);

            var hintLabel = MakeText("HintLabel", canvasGo.transform, 32, TextAnchor.MiddleCenter);
            hintLabel.rectTransform.anchorMin = hintLabel.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hintLabel.rectTransform.anchoredPosition = new Vector2(0, 70);
            hintLabel.rectTransform.sizeDelta = new Vector2(1400, 44);

            var fightPanel = new GameObject("FightPanel", typeof(RectTransform)).GetComponent<RectTransform>();
            fightPanel.SetParent(canvasGo.transform, false);
            fightPanel.anchorMin = fightPanel.anchorMax = new Vector2(0.5f, 0f);
            fightPanel.anchoredPosition = new Vector2(0, 230);
            fightPanel.sizeDelta = new Vector2(460, 90);

            var tensionBack = MakeBar("TensionBar", fightPanel, new Vector2(0, 22), new Vector2(440, 30), new Color(0.3f, 0.85f, 0.35f), "NAPIĘCIE ŻYŁKI", out var tensionFill);
            var breakMarker = MakeImage("BreakMarker", tensionBack.transform, Color.white);
            breakMarker.rectTransform.anchorMin = new Vector2(0.9f, 0f);
            breakMarker.rectTransform.anchorMax = new Vector2(0.9f, 1f);
            breakMarker.rectTransform.sizeDelta = new Vector2(3, 6);
            var staminaBack = MakeBar("StaminaBar", fightPanel, new Vector2(0, -22), new Vector2(440, 30), new Color(0.3f, 0.6f, 0.95f), "WYTRZYMAŁOŚĆ RYBY", out var staminaFill);

            var distanceLabel = MakeText("DistanceLabel", fightPanel, 28, TextAnchor.MiddleCenter);
            distanceLabel.rectTransform.anchoredPosition = new Vector2(0, 62);
            distanceLabel.rectTransform.sizeDelta = new Vector2(500, 36);

            var fishStateLabel = MakeText("FishStateLabel", fightPanel, 26, TextAnchor.MiddleCenter);
            fishStateLabel.rectTransform.anchoredPosition = new Vector2(0, -62);
            fishStateLabel.rectTransform.sizeDelta = new Vector2(500, 36);

            var biteLabel = MakeText("BiteLabel", canvasGo.transform, 64, TextAnchor.MiddleCenter);
            biteLabel.color = new Color(1f, 0.85f, 0.2f);
            biteLabel.rectTransform.anchorMin = biteLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            biteLabel.rectTransform.anchoredPosition = new Vector2(0, 120);
            biteLabel.rectTransform.sizeDelta = new Vector2(600, 90);
            biteLabel.text = "BRANIE!";
            biteLabel.gameObject.SetActive(false);

            var resultPanel = MakeImage("ResultPanel", canvasGo.transform, new Color(0, 0, 0, 0.65f));
            Stretch(resultPanel.rectTransform, 0f);
            var resultLabel = MakeText("ResultLabel", resultPanel.transform, 72, TextAnchor.MiddleCenter);
            resultLabel.rectTransform.anchorMin = resultLabel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            resultLabel.rectTransform.anchoredPosition = new Vector2(0, 80);
            resultLabel.rectTransform.sizeDelta = new Vector2(900, 220);
            var tryAgain = MakeButton("TryAgainButton", resultPanel.transform, "Spróbuj ponownie", new Vector2(0, -110), new Vector2(300, 70));

            var fpsLabel = MakeText("FpsLabel", canvasGo.transform, 22, TextAnchor.UpperRight);
            fpsLabel.rectTransform.anchorMin = fpsLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            fpsLabel.rectTransform.pivot = new Vector2(1f, 1f);
            fpsLabel.rectTransform.anchoredPosition = new Vector2(-24, -24);
            fpsLabel.rectTransform.sizeDelta = new Vector2(420, 32);
            var fpsCounter = canvasGo.AddComponent<FpsCounter>();
            var fpsSo = new SerializedObject(fpsCounter);
            fpsSo.FindProperty("label").objectReferenceValue = fpsLabel;
            fpsSo.ApplyModifiedPropertiesWithoutUndo();

            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();

            var hud = canvasGo.AddComponent<HUDController>();
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("caster").objectReferenceValue = caster;
            hudSo.FindProperty("powerRoot").objectReferenceValue = barBack.gameObject;
            hudSo.FindProperty("powerFill").objectReferenceValue = barFill;
            hudSo.FindProperty("stateLabel").objectReferenceValue = stateLabel;
            hudSo.FindProperty("hintLabel").objectReferenceValue = hintLabel;
            hudSo.FindProperty("fight").objectReferenceValue = fight;
            hudSo.FindProperty("fish").objectReferenceValue = fishAI;
            hudSo.FindProperty("bite").objectReferenceValue = biteSystem;
            hudSo.FindProperty("fightRoot").objectReferenceValue = fightPanel.gameObject;
            hudSo.FindProperty("tensionFill").objectReferenceValue = tensionFill;
            hudSo.FindProperty("staminaFill").objectReferenceValue = staminaFill;
            hudSo.FindProperty("distanceLabel").objectReferenceValue = distanceLabel;
            hudSo.FindProperty("fishStateLabel").objectReferenceValue = fishStateLabel;
            hudSo.FindProperty("biteLabel").objectReferenceValue = biteLabel;
            hudSo.FindProperty("resultRoot").objectReferenceValue = resultPanel.gameObject;
            hudSo.FindProperty("resultLabel").objectReferenceValue = resultLabel;
            hudSo.FindProperty("tryAgainButton").objectReferenceValue = tryAgain;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("FishingLake scene built: " + ScenePath);
        }

        static void BuildDock(Transform parent, Material fallback, Dictionary<Material, Material> matCache)
        {
            const float width = 2.6f;
            const float zStart = -2f;
            const float zEnd = 7f;
            const float topY = 0.05f;

            var prefab = ModelTools.Load(NatureDir + "path_wood.fbx");
            if (prefab == null)
            {
                Box("Dock", parent, new Vector3(0, -0.05f, (zStart + zEnd) / 2f), new Vector3(width, 0.2f, zEnd - zStart), fallback);
                return;
            }

            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds native = ModelTools.RendererBounds(probe);
            Object.DestroyImmediate(probe);

            bool longAlongX = native.size.x > native.size.z;
            float nativeLength = longAlongX ? native.size.x : native.size.z;
            float nativeWidth = longAlongX ? native.size.z : native.size.x;
            float scale = width / Mathf.Max(0.0001f, nativeWidth);
            float pieceLength = nativeLength * scale;
            int count = Mathf.CeilToInt((zEnd - zStart) / pieceLength);

            var dock = new GameObject("Dock").transform;
            dock.SetParent(parent, false);
            for (int i = 0; i < count; i++)
            {
                var piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, dock);
                piece.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, longAlongX ? 90f : 0f, 0f));
                piece.transform.localScale = Vector3.one * scale;
                Bounds b = ModelTools.RendererBounds(piece);
                float centerZ = zStart + (i + 0.5f) * pieceLength;
                piece.transform.position += new Vector3(-b.center.x, topY - b.max.y, centerZ - b.center.z);
                ModelTools.FixMaterials(piece, matCache);
            }

            var collider = Box("DockCollider", dock, new Vector3(0f, topY - 0.1f, (zStart + zEnd) / 2f), new Vector3(width, 0.2f, zEnd - zStart), fallback);
            Object.DestroyImmediate(collider.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(collider.GetComponent<MeshFilter>());
        }

        static Material MakeWaterMat()
        {
            string path = Root + "/Materials/Water.mat";
            AssetDatabase.DeleteAsset(path);
            var mat = new Material(Shader.Find("FishingSim/Water"));
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Material MakeSpriteMat(string name, Texture2D texture)
        {
            string path = Root + "/Materials/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);
            var mat = new Material(Shader.Find("Sprites/Default"));
            if (texture != null) mat.mainTexture = texture;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static Texture2D MakeSoftDot()
        {
            string path = Root + "/Materials/SoftDot.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            tex.Apply();
            AssetDatabase.CreateAsset(tex, path);
            return tex;
        }

        static ParticleSystem CreateSplash(Material material)
        {
            var go = new GameObject("Splash");
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(1f, 1f, 1f, 0.9f);

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return ps;
        }

        static void CreatePostProcessing()
        {
            string path = Root + "/Config/PostProcessing.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var bloom = profile.Add<Bloom>(false);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.3f);
            bloom.scatter.Override(0.6f);
            AssetDatabase.AddObjectToAsset(bloom, profile);

            var tonemapping = profile.Add<Tonemapping>(false);
            tonemapping.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(tonemapping, profile);

            var colorAdjustments = profile.Add<ColorAdjustments>(false);
            colorAdjustments.contrast.Override(10f);
            colorAdjustments.saturation.Override(12f);
            AssetDatabase.AddObjectToAsset(colorAdjustments, profile);

            var vignette = profile.Add<Vignette>(false);
            vignette.intensity.Override(0.25f);
            vignette.smoothness.Override(0.4f);
            AssetDatabase.AddObjectToAsset(vignette, profile);

            AssetDatabase.SaveAssets();

            var volumeGo = new GameObject("Post Processing");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }

        static Image MakeImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static Text MakeText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        static Image MakeBar(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor, string label, out Image fill)
        {
            var back = MakeImage(name, parent, new Color(0, 0, 0, 0.55f));
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            back.rectTransform.anchoredPosition = position;
            back.rectTransform.sizeDelta = size;

            fill = MakeImage("Fill", back.transform, fillColor);
            Stretch(fill.rectTransform, 4f);
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill.rectTransform.localScale = new Vector3(0f, 1f, 1f);

            var text = MakeText("Label", back.transform, 18, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 0f);
            text.text = label;
            return back;
        }

        static Button MakeButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var img = MakeImage(name, parent, new Color(0.2f, 0.55f, 0.9f));
            img.raycastTarget = true;
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = position;
            img.rectTransform.sizeDelta = size;
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            var text = MakeText("Label", img.transform, 32, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 0f);
            text.text = label;
            return button;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
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
