// One-shot helper to import TextMeshPro Essential Resources in batchmode.
// Run WITHOUT -quit so the async package import can finish:
//   -executeMethod Mygame.EditorTools.TmpEssentialsImporter.Import
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Mygame.EditorTools
{
    public static class TmpEssentialsImporter
    {
        private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private static int _frames;

        public static void Import()
        {
            if (File.Exists(SettingsPath))
            {
                Debug.Log("[TMP] Essentials already present.");
                EditorApplication.Exit(0);
                return;
            }

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            _frames++;
            bool ready = File.Exists(SettingsPath);
            if (!ready && _frames < 3000) return;

            EditorApplication.update -= Poll;
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log(ready
                ? "[TMP] Essential resources imported."
                : "[TMP] Timed out waiting for essentials import.");
            EditorApplication.Exit(ready ? 0 : 1);
        }
    }
}
