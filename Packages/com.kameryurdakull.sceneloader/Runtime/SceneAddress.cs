using System;

namespace SceneLoader
{
    /// <summary>A full build scene path. Unlike scene names, paths remain unambiguous.</summary>
    public readonly struct SceneAddress : IEquatable<SceneAddress>
    {
        public const string Extension = ".unity";
        public const string AssetsPrefix = "Assets/";
        public const string PackagesPrefix = "Packages/";

        public string Path { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Path);

        public SceneAddress(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A scene path is required.", nameof(path));

            var normalized = path.Replace('\\', '/');
            if ((!normalized.StartsWith(AssetsPrefix, StringComparison.Ordinal) &&
                 !normalized.StartsWith(PackagesPrefix, StringComparison.Ordinal)) ||
                !normalized.EndsWith(Extension, StringComparison.Ordinal))
                throw new ArgumentException("Use a full Assets/ or Packages/ scene path ending in .unity.", nameof(path));

            Path = normalized;
        }

        public bool Equals(SceneAddress other) => string.Equals(Path, other.Path, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SceneAddress other && Equals(other);
        public override int GetHashCode() => Path == null ? 0 : StringComparer.Ordinal.GetHashCode(Path);
        public override string ToString() => Path ?? string.Empty;
    }
}
