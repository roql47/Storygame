// Auto-setup for Meshy AI exports (FBX/GLB).
//
// Drop Meshy files into Assets/Models/<character>/ using the naming rule:
//     <character>@<animation>.fbx     e.g. witch@idle.fbx, witch@walk.fbx
//
// On import this:
//   1) Applies sane model import settings (Generic rig, animations, no junk).
//   2) Collects every clip for a character into one AnimatorController.
//   3) Builds a ready-to-use prefab with an Animator wired up.
// Generated assets go to Assets/Models/Prefabs/.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Mygame.EditorTools
{
    public class MeshyModelPostprocessor : AssetPostprocessor
    {
        private const string ModelsRoot = "Assets/Models";
        private const string PrefabsRoot = "Assets/Models/Prefabs";

        // ---- 1) Import settings (runs before the model is read) ----
        private void OnPreprocessModel()
        {
            if (!IsManagedModel(assetPath)) return;

            var importer = (ModelImporter)assetImporter;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importCameras = false;
            importer.importLights = false;

            // Meshy auto-rigs vary; Generic is the most robust default and keeps
            // the baked clips intact. Switch to Human in the inspector if you want
            // Mecanim humanoid retargeting.
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.importConstraints = false;

            // Materials stay embedded in the model (Unity 6 removed External location).
            // Use "Extract Materials" in the inspector if you need editable .mat assets.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        // ---- 2) + 3) Build controller + prefab (runs after import finishes) ----
        private static void OnPostprocessAllAssets(
            string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            var characters = new HashSet<string>();
            foreach (var path in importedAssets)
            {
                if (!IsManagedModel(path)) continue;
                characters.Add(GetCharacterName(path));
            }
            if (characters.Count == 0) return;

            if (!AssetDatabase.IsValidFolder(PrefabsRoot))
                AssetDatabase.CreateFolder(ModelsRoot, "Prefabs");

            foreach (var character in characters)
                BuildCharacter(character);

            AssetDatabase.SaveAssets();
        }

        private static void BuildCharacter(string character)
        {
            // All model files belonging to this character.
            var modelPaths = AssetDatabase.FindAssets("t:Model", new[] { ModelsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(IsManagedModel)
                .Where(p => GetCharacterName(p) == character)
                .OrderBy(p => p)
                .ToList();
            if (modelPaths.Count == 0) return;

            // Gather animation clips (skip Unity's __preview__ clips).
            var clips = new List<AnimationClip>();
            foreach (var p in modelPaths)
            {
                foreach (var obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(p))
                {
                    if (obj is AnimationClip clip && !clip.name.StartsWith("__preview"))
                        clips.Add(clip);
                }
            }

            // --- AnimatorController ---
            string controllerPath = $"{PrefabsRoot}/{character}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)
                             ?? AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            var stateMachine = controller.layers[0].stateMachine;
            var existing = stateMachine.states.ToDictionary(s => s.state.name, s => s.state);

            foreach (var clip in clips)
            {
                string stateName = GetAnimName(clip.name, character);
                if (!existing.TryGetValue(stateName, out var state))
                {
                    state = stateMachine.AddState(stateName);
                    existing[stateName] = state;
                }
                state.motion = clip;
            }

            // Default to "idle" if present, else first state.
            if (existing.TryGetValue("idle", out var idle))
                stateMachine.defaultState = idle;
            else if (existing.Count > 0)
                stateMachine.defaultState = existing.Values.First();

            EditorUtility.SetDirty(controller);

            // --- Prefab ---
            string basePath = modelPaths.FirstOrDefault(p => GetAnimName(p, character) == "idle")
                              ?? modelPaths[0];
            var modelGo = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (modelGo == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelGo);
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Reuse the model's generated avatar if one exists.
            var avatar = AssetDatabase.LoadAllAssetsAtPath(basePath).OfType<Avatar>().FirstOrDefault();
            if (avatar != null) animator.avatar = avatar;

            string prefabPath = $"{PrefabsRoot}/{character}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            Debug.Log($"[Meshy] Built '{character}': {clips.Count} clip(s) -> {prefabPath}");
        }

        // ---- helpers ----
        private static bool IsManagedModel(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith(ModelsRoot)) return false;
            if (path.StartsWith(PrefabsRoot)) return false; // don't reprocess generated assets
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".fbx" || ext == ".glb" || ext == ".gltf";
        }

        // "witch@walk.fbx" -> "witch"  |  "witch.fbx" -> "witch"
        private static string GetCharacterName(string path)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            int at = file.IndexOf('@');
            return at >= 0 ? file.Substring(0, at) : file;
        }

        // "witch@walk" (or clip name) -> "walk"; falls back to the raw name.
        private static string GetAnimName(string nameOrPath, string character)
        {
            string file = Path.GetFileNameWithoutExtension(nameOrPath);
            int at = file.IndexOf('@');
            if (at >= 0) return file.Substring(at + 1);
            // Clip baked inside a single-anim FBX often shares the file/character name.
            return file == character ? "idle" : file;
        }
    }
}
