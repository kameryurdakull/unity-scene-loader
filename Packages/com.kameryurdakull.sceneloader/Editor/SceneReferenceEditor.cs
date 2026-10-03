using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SceneLoader.Editor
{
    [CustomEditor(typeof(SceneReference))]
    public sealed class SceneReferenceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var path = serializedObject.FindProperty(SceneReference.PathProperty);
            var guid = serializedObject.FindProperty(SceneReference.GuidProperty);
            var currentPath = string.IsNullOrEmpty(guid.stringValue) ? path.stringValue :
                AssetDatabase.GUIDToAssetPath(guid.stringValue);
            var current = AssetDatabase.LoadAssetAtPath<SceneAsset>(currentPath);
            EditorGUI.BeginChangeCheck();
            var selected = (SceneAsset)EditorGUILayout.ObjectField("Scene", current, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                path.stringValue = selected == null ? string.Empty : AssetDatabase.GetAssetPath(selected);
                guid.stringValue = selected == null ? string.Empty : AssetDatabase.AssetPathToGUID(path.stringValue);
                serializedObject.ApplyModifiedProperties();
            }

            if (selected == null)
            {
                EditorGUILayout.HelpBox("Select a scene asset.", MessageType.Warning);
                return;
            }

            var selectedPath = AssetDatabase.GetAssetPath(selected);
            EditorGUILayout.LabelField("Path", selectedPath);
            var included = EditorBuildSettings.scenes.Any(scene => scene.path == selectedPath && scene.enabled);
            if (included) return;
            EditorGUILayout.HelpBox("Scene is missing or disabled in the build scene list.", MessageType.Warning);
            if (GUILayout.Button("Enable in Build Scene List"))
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                var existing = scenes.Find(scene => scene.path == selectedPath);
                if (existing == null) scenes.Add(new EditorBuildSettingsScene(selectedPath, true));
                else existing.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }

    internal sealed class SceneReferenceSynchronizer : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Synchronize();

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            if (moved.Any(IsScene) || deleted.Any(IsScene)) Synchronize();
        }

        private static bool IsScene(string path) => path.EndsWith(SceneAddress.Extension, StringComparison.Ordinal);

        private static void Synchronize()
        {
            var references = AssetDatabase.FindAssets($"t:{nameof(SceneReference)}");
            foreach (var referenceGuid in references)
            {
                var reference = AssetDatabase.LoadAssetAtPath<SceneReference>(AssetDatabase.GUIDToAssetPath(referenceGuid));
                var serialized = new SerializedObject(reference);
                var guid = serialized.FindProperty(SceneReference.GuidProperty);
                if (string.IsNullOrEmpty(guid.stringValue)) continue;
                var path = serialized.FindProperty(SceneReference.PathProperty);
                var resolvedPath = AssetDatabase.GUIDToAssetPath(guid.stringValue);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(resolvedPath) == null)
                    resolvedPath = string.Empty;
                if (path.stringValue == resolvedPath) continue;
                path.stringValue = resolvedPath;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(reference);
            }
        }
    }
}
