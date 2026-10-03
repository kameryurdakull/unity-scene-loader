using UnityEngine;

namespace SceneLoader
{
    [CreateAssetMenu(fileName = "SceneReference", menuName = "Scene Loader/Scene Reference")]
    public sealed class SceneReference : ScriptableObject
    {
        public const string PathProperty = "scenePath";
        public const string GuidProperty = "sceneGuid";
        [SerializeField, HideInInspector] private string scenePath;
        [SerializeField, HideInInspector] private string sceneGuid;

        public string Path => scenePath;
        public SceneAddress Address => new SceneAddress(scenePath);
    }
}
