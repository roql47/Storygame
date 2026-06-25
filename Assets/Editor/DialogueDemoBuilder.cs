// Builds a "Witch's Spring"-style dialogue UI prefab + a playable test scene.
// Run: -executeMethod Mygame.EditorTools.DialogueDemoBuilder.Run
// Or from the menu: Mygame > Build Dialogue Demo
using System.IO;
using Ink;
using Mygame.Dialogue;
using Mygame.GameSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mygame.EditorTools
{
    public static class DialogueDemoBuilder
    {
        // ---- palette (warm storybook) ----
        static readonly Color ScreenBg   = new Color32(0x21, 0x1A, 0x2E, 0xFF);
        static readonly Color BoxBg       = new Color32(0x2B, 0x1F, 0x33, 0xF2);
        static readonly Color NameBg      = new Color32(0x6E, 0x4A, 0x6B, 0xFF);
        static readonly Color ChoiceBg    = new Color32(0x47, 0x33, 0x52, 0xF7);
        static readonly Color Cream       = new Color32(0xF3, 0xE9, 0xD8, 0xFF);
        static readonly Color Gold        = new Color32(0xF0, 0xC9, 0x87, 0xFF);
        static readonly Color PortraitSlot= new Color32(0x8A, 0x76, 0x9E, 0x40);

        const string UiPrefabDir = "Assets/UI/Prefabs";
        const string DialoguePrefabPath = UiPrefabDir + "/DialogueUI.prefab";
        const string ChoicePrefabPath = UiPrefabDir + "/ChoiceButton.prefab";
        const string InkJsonPath = "Assets/Ink/DemoStory.json";
        const string ScenePath = "Assets/Scenes/DialogueTest.unity";

        [MenuItem("Mygame/Build Dialogue Demo")]
        public static void Run()
        {
            EnsureFolder("Assets/UI", "Prefabs");
            EnsureFolder("Assets", "Ink");
            EnsureFolder("Assets", "Scenes");

            TextAsset ink = CompileDemoInk();
            GameObject choicePrefab = BuildChoiceButtonPrefab();
            GameObject dialoguePrefab = BuildDialoguePrefab(choicePrefab, ink);
            BuildScene(dialoguePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DialogueDemo] Built UI prefab + test scene successfully.");
        }

        // ---------- Ink ----------
        static TextAsset CompileDemoInk()
        {
            const string source =
@"VAR brave = false

# speaker: 마녀
# portrait: witch_smile
숲 속 깊은 곳, 오래된 샘 앞에서 너를 기다리고 있었단다.
# speaker: 마녀
이 샘물은 소원을 이루어 준다고 하지... 하지만 모든 것엔 대가가 따르는 법이야.
* [샘물에 소원을 빈다] -> wish
* [조용히 돌아선다] -> leave

=== wish ===
~ brave = true
# speaker: 마녀
후후, 용기있구나. 그래서, 어떤 소원이지?
-> ending

=== leave ===
# speaker: 마녀
겁쟁이로군. 다음 기회는... 없을지도 몰라.
-> ending

=== ending ===
# speaker: 마녀
또 만나게 될 거야. 반드시.
-> END
";
            var compiler = new Compiler(source);
            var story = compiler.Compile();
            File.WriteAllText(InkJsonPath, story.ToJson());
            AssetDatabase.ImportAsset(InkJsonPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(InkJsonPath);
        }

        // ---------- Choice button prefab ----------
        static GameObject BuildChoiceButtonPrefab()
        {
            var go = NewUI("ChoiceButton", null);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(820, 104);

            var img = go.AddComponent<Image>();
            img.sprite = Builtin("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = ChoiceBg;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.85f, 0.78f, 0.7f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 820;
            le.preferredHeight = 104;

            var label = NewText("Label", go.transform, "선택지", 36, Cream, TextAlignmentOptions.Center);
            Stretch((RectTransform)label.transform, 24, 8, 24, 8);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, ChoicePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------- Dialogue UI prefab ----------
        static GameObject BuildDialoguePrefab(GameObject choicePrefab, TextAsset ink)
        {
            // Canvas root
            var root = new GameObject("DialogueUI",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            // DialogueRoot (toggled on/off as the "panel")
            var panel = NewUI("DialogueRoot", root.transform);
            Stretch((RectTransform)panel.transform, 0, 0, 0, 0);

            // Portrait slot (placeholder bust above the box, left side)
            var portrait = NewUI("Portrait", panel.transform);
            var prt = (RectTransform)portrait.transform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 0f);
            prt.pivot = new Vector2(0f, 0f);
            prt.sizeDelta = new Vector2(560, 760);
            prt.anchoredPosition = new Vector2(40, 520);
            var pImg = portrait.AddComponent<Image>();
            pImg.color = PortraitSlot;
            var pLabel = NewText("Hint", portrait.transform, "[ 캐릭터 입화면 ]", 28,
                new Color(1, 1, 1, 0.5f), TextAlignmentOptions.Center);
            Stretch((RectTransform)pLabel.transform, 0, 0, 0, 0);

            // Dialogue box (bottom)
            var box = NewUI("DialogueBox", panel.transform);
            var brt = (RectTransform)box.transform;
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.offsetMin = new Vector2(40, 40);     // left, bottom
            brt.offsetMax = new Vector2(-40, 500);   // right, height(=top from bottom)
            var boxImg = box.AddComponent<Image>();
            boxImg.sprite = Builtin("UI/Skin/Background.psd");
            boxImg.type = Image.Type.Sliced;
            boxImg.color = BoxBg;

            // Name box (sits on the top-left edge of the dialogue box)
            var nameBox = NewUI("NameBox", box.transform);
            var nrt = (RectTransform)nameBox.transform;
            nrt.anchorMin = new Vector2(0f, 1f);
            nrt.anchorMax = new Vector2(0f, 1f);
            nrt.pivot = new Vector2(0f, 0.5f);
            nrt.sizeDelta = new Vector2(320, 90);
            nrt.anchoredPosition = new Vector2(44, 0);
            var nImg = nameBox.AddComponent<Image>();
            nImg.sprite = Builtin("UI/Skin/Background.psd");
            nImg.type = Image.Type.Sliced;
            nImg.color = NameBg;
            var speaker = NewText("SpeakerText", nameBox.transform, "마녀", 40, Gold, TextAlignmentOptions.Center);
            Stretch((RectTransform)speaker.transform, 12, 4, 12, 4);
            var speakerTmp = speaker.GetComponent<TMP_Text>();
            speakerTmp.fontStyle = FontStyles.Bold;

            // Body text
            var body = NewText("BodyText", box.transform,
                "여기에 대사가 표시됩니다.", 42, Cream, TextAlignmentOptions.TopLeft);
            Stretch((RectTransform)body.transform, 56, 64, 56, 70);
            var bodyTmp = body.GetComponent<TMP_Text>();
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            bodyTmp.lineSpacing = 8f;

            // Continue indicator
            var cont = NewText("ContinueIndicator", box.transform, "▼", 34, Gold, TextAlignmentOptions.BottomRight);
            var crt = (RectTransform)cont.transform;
            crt.anchorMin = new Vector2(1f, 0f);
            crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(1f, 0f);
            crt.sizeDelta = new Vector2(80, 60);
            crt.anchoredPosition = new Vector2(-30, 24);

            // Choices container (stacks above the dialogue box)
            var choices = NewUI("ChoicesContainer", panel.transform);
            var chrt = (RectTransform)choices.transform;
            chrt.anchorMin = new Vector2(0.5f, 0f);
            chrt.anchorMax = new Vector2(0.5f, 0f);
            chrt.pivot = new Vector2(0.5f, 0f);
            chrt.sizeDelta = new Vector2(860, 10);
            chrt.anchoredPosition = new Vector2(0, 560);
            var vlg = choices.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var fitter = choices.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // DialogueManager + wiring
            var dm = root.AddComponent<DialogueManager>();
            var so = new SerializedObject(dm);
            so.FindProperty("inkJson").objectReferenceValue = ink;
            so.FindProperty("playOnStart").boolValue = false;
            so.FindProperty("dialoguePanel").objectReferenceValue = panel;
            so.FindProperty("speakerText").objectReferenceValue = speakerTmp;
            so.FindProperty("dialogueText").objectReferenceValue = bodyTmp;
            so.FindProperty("choicesContainer").objectReferenceValue = choices.transform;
            so.FindProperty("choiceButtonPrefab").objectReferenceValue =
                choicePrefab.GetComponent<Button>();
            so.FindProperty("voiceSource").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DialoguePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------- Scene ----------
        static void BuildScene(GameObject dialoguePrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = ScreenBg;
            camGo.transform.position = new Vector3(0, 0, -10);

            // EventSystem (legacy input module — project uses old input handler)
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // Background canvas
            var bgRoot = new GameObject("Background",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var bgCanvas = bgRoot.GetComponent<Canvas>();
            bgCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            bgCanvas.sortingOrder = -10;
            var bgImg = NewUI("BG", bgRoot.transform);
            Stretch((RectTransform)bgImg.transform, 0, 0, 0, 0);
            var img = bgImg.AddComponent<Image>();
            img.color = ScreenBg;

            // Dialogue UI instance
            var ui = (GameObject)PrefabUtility.InstantiatePrefab(dialoguePrefab);
            var dm = ui.GetComponent<DialogueManager>();

            // Tester
            var testerGo = new GameObject("DialogueTester");
            var tester = testerGo.AddComponent<DialogueTester>();
            var so = new SerializedObject(tester);
            so.FindProperty("dialogue").objectReferenceValue = dm;
            so.FindProperty("autoStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);

            // Register both scenes in build settings (Main first, demo second)
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
        }

        // ---------- helpers ----------
        static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject NewText(string name, Transform parent, string text, float size,
            Color color, TextAlignmentOptions align)
        {
            var go = NewUI(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return go;
        }

        static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        static Sprite Builtin(string path) =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>(path);

        static void EnsureFolder(string parent, string child)
        {
            string full = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(full))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
