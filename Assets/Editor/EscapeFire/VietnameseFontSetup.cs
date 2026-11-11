using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace EscapeFire.EditorTools
{
    /// <summary>
    /// Vietnamese text everywhere (fix for the wrist HUD showing "CHÁY R I" instead of "CHÁY RỒI").
    /// Builds one dynamic SDF font from LiberationSans with every Vietnamese letter pre-baked, makes it the TMP default
    /// and assigns it to every TMP text in the game scenes. Idempotent.
    /// </summary>
    public static class VietnameseFontSetup
    {
        private const string SOURCE = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string ASSET = "Assets/TextMesh Pro/Resources/Fonts & Materials/EF_Vietnamese SDF.asset";
        private static readonly string[] SCENES =
        {
            "Assets/Scenes/00_MainMenu.unity", "Assets/Scenes/BasicScene.unity", "Assets/Scenes/02_AAR_Evaluation.unity"
        };

        /// <summary>Basic Latin + Latin-1 + Vietnamese letters (precomposed), digits and common symbols.</summary>
        public static string Charset()
        {
            var sb = new StringBuilder();
            for (int c = 0x20; c <= 0x7E; c++) sb.Append((char)c);
            for (int c = 0xA0; c <= 0xFF; c++) sb.Append((char)c);
            sb.Append("ĂăĐđĨĩŨũƠơƯư");
            for (int c = 0x1EA0; c <= 0x1EF9; c++) sb.Append((char)c);
            sb.Append("₀₁₂₃₄₅₆₇₈₉²³°±×÷–—‘’“”…•→←↑↓≥≤✓✗€");
            return sb.ToString();
        }

        [MenuItem("EscapeFire/V1/Setup Vietnamese Font")]
        public static void Run()
        {
            var font = GetOrCreate();
            SetDefault(font);
            int n = 0;
            foreach (var path in SCENES)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                n += AssignInOpenScene(font);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[EscapeFire] Vietnamese font ready: {font.characterTable.Count} characters baked, {n} texts reassigned.");
        }

        public static TMP_FontAsset GetOrCreate()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ASSET);
            if (fa == null)
            {
                var src = AssetDatabase.LoadAssetAtPath<Font>(SOURCE);
                fa = TMP_FontAsset.CreateFontAsset(src, 72, 8, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                fa.name = "EF_Vietnamese SDF";
                AssetDatabase.CreateAsset(fa, ASSET);
                fa.material.name = "EF_Vietnamese SDF Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
                foreach (var tex in fa.atlasTextures)
                {
                    if (tex == null || AssetDatabase.Contains(tex)) continue;
                    tex.name = "EF_Vietnamese SDF Atlas";
                    AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }
            if (fa.atlasPopulationMode == AtlasPopulationMode.Static && fa.characterTable.Count > 200) return fa;
            fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fa.TryAddCharacters(Charset(), out string missing);
            if (!string.IsNullOrEmpty(missing)) Debug.LogWarning($"[EscapeFire] Font is missing: {missing}");
            // Freeze it: dynamic glyphs added in the editor are not written to disk; a static atlas is.
            fa.atlasPopulationMode = AtlasPopulationMode.Static;
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null || AssetDatabase.Contains(tex)) continue;
                tex.name = "EF_Vietnamese SDF Atlas";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        private static void SetDefault(TMP_FontAsset fa)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null) return;
            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        /// <summary>Points every TMP text in the open scene(s) at the Vietnamese font. Returns how many changed.</summary>
        public static int AssignInOpenScene(TMP_FontAsset fa)
        {
            int n = 0;
            foreach (var t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.font == fa) continue;
                t.font = fa;
                t.fontSharedMaterial = fa.material;
                EditorUtility.SetDirty(t);
                n++;
            }
            return n;
        }
    }
}
