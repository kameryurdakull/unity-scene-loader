using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SceneLoader.Tests
{
    public sealed class SceneReferenceTests
    {
        private const string AssetsRoot = "Assets";
        private const string FolderPrefix = "SceneLoaderReferenceTest_";
        private const string SceneFileName = "Original.unity";
        private const string MovedSceneFileName = "Moved.unity";
        private const string ReferenceFileName = "Reference.asset";
        private const string FixtureScenePath = "Packages/com.kameryurdakull.sceneloader/Tests/Editor/Fixtures/ReferenceScene.unity";

        [Test]
        public void SceneMoveUpdatesReferenceAndDeletionClearsPath()
        {
            var folderName = FolderPrefix + Guid.NewGuid().ToString("N");
            var folder = AssetsRoot + "/" + folderName;
            AssetDatabase.CreateFolder(AssetsRoot, folderName);
            var scenePath = folder + "/" + SceneFileName;
            var movedPath = folder + "/" + MovedSceneFileName;
            try
            {
                Assert.That(AssetDatabase.CopyAsset(FixtureScenePath, scenePath), Is.True);
                var reference = ScriptableObject.CreateInstance<SceneReference>();
                AssetDatabase.CreateAsset(reference, folder + "/" + ReferenceFileName);
                var serialized = new SerializedObject(reference);
                serialized.FindProperty(SceneReference.PathProperty).stringValue = scenePath;
                serialized.FindProperty(SceneReference.GuidProperty).stringValue = AssetDatabase.AssetPathToGUID(scenePath);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(reference);

                Assert.That(AssetDatabase.MoveAsset(scenePath, movedPath), Is.Empty);
                Assert.That(reference.Path, Is.EqualTo(movedPath));
                Assert.That(reference.Address.Path, Is.EqualTo(movedPath));

                Assert.That(AssetDatabase.DeleteAsset(movedPath), Is.True);
                Assert.That(reference.Path, Is.Empty);
                Assert.Throws<ArgumentException>(() => { var address = reference.Address; });
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
