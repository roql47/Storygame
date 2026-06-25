// One-shot project setup run in batchmode via -executeMethod ProjectBootstrap.Run
// Creates a URP 2D pipeline asset, assigns it, sets mobile resolution, and a starter scene.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ProjectBootstrap
{
    public static void Run()
    {
        try
        {
            SetupUrp2D();
            SetupPlayerSettings();
            SetupStarterScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProjectBootstrap] Completed successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ProjectBootstrap] Failed: " + e);
        }
    }

    private static void SetupUrp2D()
    {
        const string dir = "Assets/Settings";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        const string rendererPath = dir + "/Renderer2D.asset";
        const string urpPath = dir + "/URP_2D.asset";

        var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
        }

        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
        if (urp == null)
        {
            urp = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(urp, urpPath);
        }

        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;
        Debug.Log("[ProjectBootstrap] URP 2D pipeline assigned.");
    }

    private static void SetupPlayerSettings()
    {
        PlayerSettings.defaultScreenWidth = 1080;
        PlayerSettings.defaultScreenHeight = 1920;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.productName = "Mygame";
        PlayerSettings.companyName = "Mygame";
        Debug.Log("[ProjectBootstrap] Player settings set to 1080x1920 portrait.");
    }

    private static void SetupStarterScene()
    {
        const string scenesDir = "Assets/Scenes";
        if (!AssetDatabase.IsValidFolder(scenesDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        const string scenePath = scenesDir + "/Main.unity";
        if (System.IO.File.Exists(scenePath))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.05f, 0.09f, 1f);
        camGo.AddComponent<UniversalAdditionalCameraData>();
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;

        EditorSceneManager.SaveScene(scene, scenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        Debug.Log("[ProjectBootstrap] Starter scene created at " + scenePath);
    }
}
