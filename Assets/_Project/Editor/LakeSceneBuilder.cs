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
            // thick trigger (3 m deep, top flush with the surface) so a fast lure cannot tunnel through it
            wc.size = new Vector3(1f, 30f, 1f);
            wc.center = new Vector3(0f, -14.5f, 0f);
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

            var fishMat = MakeMat("Fish", new Color(0.95f, 0.5f, 0.15f));
            var fish = new GameObject("Fish").transform;
            var fishBody = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(fishBody.GetComponent<Collider>());
            fishBody.transform.SetParent(fish, false);
            fishBody.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            fishBody.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
            fishBody.GetComponent<Renderer>().sharedMaterial = fishMat;

            var systems = new GameObject("FishingSystems");

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
