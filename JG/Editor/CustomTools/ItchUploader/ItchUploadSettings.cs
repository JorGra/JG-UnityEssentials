using System;
using UnityEditor;
using UnityEngine;

namespace JG.Tools.Itch
{
    /// <summary>
    /// Per-project itch.io upload configuration. Stored in ProjectSettings so it is
    /// version controlled and does not clutter the Assets folder.
    /// </summary>
    [FilePath("ProjectSettings/ItchUploadSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ItchUploadSettings : ScriptableSingleton<ItchUploadSettings>
    {
        [Tooltip("Itch project page, e.g. https://user.itch.io/game, or the short form user/game.")]
        public string projectUrl = "";

        [Tooltip("Butler channel name. itch.io treats 'html5' (or anything containing 'html') as a playable web build.")]
        public string channel = "html5";

        [Tooltip("Output folder for the WebGL build, relative to the project root.")]
        public string buildPath = "Builds/WebGL";

        [Tooltip("Path to the butler executable. Leave as 'butler' to resolve from PATH, or use Detect.")]
        public string butlerPath = "butler";

        [Tooltip("Pass PlayerSettings.bundleVersion to butler as --userversion.")]
        public bool useBundleVersion = true;

        [Tooltip("Additional arguments appended to 'butler push'.")]
        public string extraArgs = "";

        public void SaveSettings() => Save(true);

        /// <summary>
        /// Resolves the configured project URL into a butler target of the form "user/game".
        /// Accepts full itch URLs (https://user.itch.io/game) and the short form (user/game).
        /// </summary>
        public bool TryGetTarget(out string target)
        {
            target = null;
            var raw = (projectUrl ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(raw))
                return false;

            if (Uri.TryCreate(raw, UriKind.Absolute, out var uri) && uri.Host.EndsWith(".itch.io", StringComparison.OrdinalIgnoreCase))
            {
                var user = uri.Host.Substring(0, uri.Host.Length - ".itch.io".Length);
                var segments = uri.AbsolutePath.Trim('/').Split('/');
                if (string.IsNullOrEmpty(user) || segments.Length == 0 || string.IsNullOrEmpty(segments[0]))
                    return false;
                target = $"{user}/{segments[0]}".ToLowerInvariant();
                return true;
            }

            var parts = raw.Split('/');
            if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
            {
                target = $"{parts[0]}/{parts[1]}".ToLowerInvariant();
                return true;
            }

            return false;
        }

        public string GetPageUrl()
        {
            if (!TryGetTarget(out var target))
                return null;
            var parts = target.Split('/');
            return $"https://{parts[0]}.itch.io/{parts[1]}";
        }
    }
}
