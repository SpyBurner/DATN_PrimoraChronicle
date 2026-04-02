using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

public class SceneSetupEditor : EditorWindow
{
    private const string ScenesFolder = "Assets/_Game/Scenes";

    private static readonly string[] BaseSceneNames = { "Boot", "Account", "Lobby", "Gameplay" };

    private static readonly (string name, int sortOrder)[] UILayers =
    {
        ("Screen Layer",  0),
        ("Overlay Layer", 2),
        ("Popup Layer",   3),
        ("System Layer",  4),
    };

    [MenuItem("Primora/Scene Setup/Setup All Scenes", false, 0)]
    public static void SetupAllScenes()
    {
        if (!EditorUtility.DisplayDialog("Setup All Scenes",
            "This will create/overwrite Boot, Account, Lobby, and Gameplay scenes.\nContinue?",
            "Yes", "Cancel"))
            return;

        CreateProjectContext();
        EnsureFolder(ScenesFolder);

        foreach (var sceneName in BaseSceneNames)
        {
            var path = $"{ScenesFolder}/{sceneName}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;

            SetupBaseScene(scene, sceneName);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[SceneSetup] Created scene: {path}");
        }

        AddScenesToBuildSettings();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", "All base scenes have been created and added to Build Settings.", "Ok");
    }

    [MenuItem("Primora/Scene Setup/Setup Boot Scene Only", false, 1)]
    public static void SetupBootSceneOnly()
    {
        CreateProjectContext();
        EnsureFolder(ScenesFolder);
        var path = $"{ScenesFolder}/Boot.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Boot";
        SetupBaseScene(scene, "Boot");
        EditorSceneManager.SaveScene(scene, path);
        AddScenesToBuildSettings();
        AssetDatabase.Refresh();
        Debug.Log($"[SceneSetup] Boot scene created at {path}");
    }

    [MenuItem("Primora/Scene Setup/Setup Current Scene (Base)", false, 2)]
    public static void SetupCurrentScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name))
        {
            EditorUtility.DisplayDialog("Error", "Save the current scene first.", "Ok");
            return;
        }
        SetupBaseScene(scene, scene.name);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SceneSetup] Base hierarchy added to '{scene.name}'.");
    }

    // ───────────────────── Core Setup ─────────────────────

    private static void SetupBaseScene(Scene scene, string sceneName)
    {
        // 1. Main Camera
        CreateMainCamera();

        // 2. EventSystem
        CreateEventSystem();

        // 3. Global Volume
        CreateGlobalVolume();

        // 4. UIRoot with layer canvases
        CreateUIRoot();

        // 5. SceneContext + Installer (Boot gets CoreInstaller)
        CreateSceneContext(sceneName);

        // 6. Scene-specific setup
        switch (sceneName)
        {
            case "Boot":
                CreateBootstrapController();
                break;
            case "Account":
                CreateAccountScene();
                break;
        }
    }

    // ───────────────────── Creators ─────────────────────

    private static void CreateMainCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 1, -10);

        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 0f);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        cam.fieldOfView = 60f;
        cam.depth = -1;

        go.AddComponent<AudioListener>();

        // URP UniversalAdditionalCameraData is added automatically by URP if present
    }

    private static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static void CreateGlobalVolume()
    {
        var go = new GameObject("Global Volume");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 0;
        vol.weight = 1f;
    }

    private static void CreateUIRoot()
    {
        var root = new GameObject("UIRoot");

        foreach (var (layerName, sortOrder) in UILayers)
        {
            var layer = new GameObject(layerName);
            layer.transform.SetParent(root.transform, false);

            // Canvas
            var canvas = layer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;

            // CanvasScaler
            var scaler = layer.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            // GraphicRaycaster
            layer.AddComponent<GraphicRaycaster>();

            // CanvasGroup
            layer.AddComponent<CanvasGroup>();
        }
    }

    private static void CreateSceneContext(string sceneName)
    {
        var go = new GameObject("SceneContext");
        go.AddComponent<SceneContext>();
    }

    private static void CreateBootstrapController()
    {
        var go = new GameObject("BootstrapController");
        go.AddComponent<BootstrapController>();
    }

    private static void CreateAccountScene()
    {
        var controller = new GameObject("AccountSceneController");
        controller.AddComponent<AccountSceneController>();
    }

    // ───────────────────── ProjectContext ─────────────────────

    private const string ProjectContextFolder = "Assets/Resources";
    private const string ProjectContextPath = "Assets/Resources/ProjectContext.prefab";

    [MenuItem("Primora/Scene Setup/Create ProjectContext", false, 10)]
    public static void CreateProjectContextMenuItem()
    {
        CreateProjectContext();
        EditorUtility.DisplayDialog("Done", "ProjectContext prefab created/updated at Resources/ProjectContext.", "Ok");
    }

    private static void CreateProjectContext()
    {
        EnsureFolder(ProjectContextFolder);

        // Load existing or create new
        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectContextPath);
        if (existingPrefab != null)
        {
            Debug.Log("[SceneSetup] ProjectContext prefab already exists. Skipping creation.");
            return;
        }

        // Build the ProjectContext GameObject
        var go = new GameObject("ProjectContext");
        var projectCtx = go.AddComponent<ProjectContext>();

        // Add CoreInstaller (internal, resolved by type name)
        var installerType = System.Type.GetType("CoreInstaller, Core");
        if (installerType != null)
        {
            var installer = go.AddComponent(installerType) as MonoInstaller;
            var so = new SerializedObject(projectCtx);
            var monoInstallersProp = so.FindProperty("_monoInstallers");
            monoInstallersProp.arraySize = 1;
            monoInstallersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[SceneSetup] Could not find CoreInstaller type. Add it manually to the ProjectContext prefab.");
        }

        // Save as prefab
        PrefabUtility.SaveAsPrefabAsset(go, ProjectContextPath);
        Object.DestroyImmediate(go);

        AssetDatabase.Refresh();
        Debug.Log($"[SceneSetup] ProjectContext prefab created at {ProjectContextPath}");
    }

    // ───────────────────── Build Settings ─────────────────────

    private static void AddScenesToBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (var name in BaseSceneNames)
        {
            var path = $"{ScenesFolder}/{name}.unity";
            if (File.Exists(Path.GetFullPath(path)))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("[SceneSetup] Build Settings updated with scene order: " + string.Join(", ", BaseSceneNames));
    }

    // ───────────────────── Helpers ─────────────────────

    private static void EnsureFolder(string assetPath)
    {
        var parts = assetPath.Split('/');
        var current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
