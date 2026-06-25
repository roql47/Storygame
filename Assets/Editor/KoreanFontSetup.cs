// Creates a dynamic TextMeshPro SDF font asset from Noto Sans KR and makes it
// the TMP default font, so Korean text renders without warnings.
// Dynamic atlas = glyphs are rasterized on demand (no 11k-glyph prebake).
// Run: -executeMethod Mygame.EditorTools.KoreanFontSetup.Run
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Mygame.EditorTools
{
    public static class KoreanFontSetup
    {
        const string TtfPath = "Assets/Fonts/NotoSansKR.ttf";
        const string FontAssetPath = "Assets/Fonts/NotoSansKR SDF.asset";

        [MenuItem("Mygame/Setup Korean Font")]
        public static void Run()
        {
            AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceSynchronousImport);
            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError($"[Font] TTF not found at {TtfPath}");
                EditorApplication.Exit(1);
                return;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                // 90pt sampling, 9px padding, SDFAA, 1024x1024 dynamic atlas.
                fontAsset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
                fontAsset.name = "NotoSansKR SDF";

                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

                // Persist the generated atlas texture(s) and material as sub-assets.
                if (fontAsset.atlasTextures != null)
                {
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        var tex = fontAsset.atlasTextures[i];
                        if (tex == null) continue;
                        tex.name = $"NotoSansKR Atlas {i}";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                }
                if (fontAsset.material != null)
                {
                    fontAsset.material.name = "NotoSansKR Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                }
                EditorUtility.SetDirty(fontAsset);
                Debug.Log("[Font] Created NotoSansKR SDF dynamic font asset.");
            }

            // Make it the project-wide TMP default.
            var settings = TMP_Settings.instance;
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = fontAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Font] Noto Sans KR set as TMP default font.");
            EditorApplication.Exit(0);
        }
    }
}
