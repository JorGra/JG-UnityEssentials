using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace JG.Tools.Itch
{
    /// <summary>
    /// Thin wrapper around the itch.io butler CLI. Runs butler as a background process
    /// and forwards its output to the Unity console without blocking the editor.
    /// </summary>
    public static class ButlerRunner
    {
        public static bool IsRunning => process != null && !process.HasExited;
        public static string LastCommand { get; private set; } = "";
        public static event Action<int> Exited;

        static Process process;
        static readonly ConcurrentQueue<string> pending = new ConcurrentQueue<string>();
        static bool exitReported;

        static ButlerRunner()
        {
            EditorApplication.update += Pump;
        }

        /// <summary>
        /// Returns an absolute path to butler, or null if it cannot be found.
        /// Checks the configured path, PATH, and the usual install locations.
        /// </summary>
        public static string Resolve(string configured)
        {
            foreach (var candidate in Candidates(configured))
            {
                if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
                    return Path.GetFullPath(candidate);
            }
            return null;
        }

        static IEnumerable<string> Candidates(string configured)
        {
            configured = (configured ?? "").Trim();
            var exe = Application.platform == RuntimePlatform.WindowsEditor ? "butler.exe" : "butler";

            if (!string.IsNullOrEmpty(configured))
            {
                if (Path.IsPathRooted(configured) || configured.Contains("/") || configured.Contains("\\"))
                    yield return configured;
                else
                    foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
                        yield return Path.Combine(dir, configured);
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            yield return Path.Combine(home, ".local", "bin", exe);
            yield return Path.Combine(home, ".config", "itch", "apps", "butler", exe);
            yield return Path.Combine(home, "Library", "Application Support", "itch", "apps", "butler", exe);
            yield return Path.Combine(appData, "itch", "apps", "butler", exe);
            yield return Path.Combine(localAppData, "itch", "apps", "butler", exe);
            yield return Path.Combine(home, "bin", exe);
            yield return "/usr/local/bin/" + exe;
            yield return "/usr/bin/" + exe;
        }

        /// <summary>True if butler has stored credentials (i.e. `butler login` was run).</summary>
        public static bool HasCredentials()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return File.Exists(Path.Combine(home, ".config", "itch", "butler_creds"))
                || File.Exists(Path.Combine(appData, "itch", "butler_creds"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUTLER_API_KEY"));
        }

        public static bool Run(string butlerPath, string arguments)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[Itch] butler is already running.");
                return false;
            }

            var resolved = Resolve(butlerPath);
            if (resolved == null)
            {
                Debug.LogError($"[Itch] Could not find butler ('{butlerPath}'). Install it from https://itch.io/docs/butler/ and set the path in Tools > Itch.io Upload.");
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = resolved,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(Application.dataPath),
            };
            // butler prints progress bars when it thinks it has a TTY; force plain output.
            startInfo.EnvironmentVariables["BUTLER_NO_TTY"] = "1";

            LastCommand = $"{Path.GetFileName(resolved)} {arguments}";
            Debug.Log($"[Itch] > {LastCommand}");

            try
            {
                process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, e) => { if (e.Data != null) pending.Enqueue(e.Data); };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) pending.Enqueue(e.Data); };
                exitReported = false;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Itch] Failed to start butler: {ex.Message}");
                process = null;
                return false;
            }
        }

        static void Pump()
        {
            while (pending.TryDequeue(out var line))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                // butler emits progress with carriage returns; keep only the last segment.
                var idx = line.LastIndexOf('\r');
                if (idx >= 0)
                    line = line.Substring(idx + 1);
                Debug.Log($"[butler] {line}");
            }

            if (process != null && process.HasExited && !exitReported)
            {
                exitReported = true;
                var code = process.ExitCode;
                process.Dispose();
                process = null;

                if (code == 0)
                    Debug.Log("[Itch] butler finished successfully.");
                else
                    Debug.LogError($"[Itch] butler exited with code {code}.");

                Exited?.Invoke(code);
            }
        }

        public static string Quote(string s) => $"\"{s.Replace("\"", "\\\"")}\"";
    }
}
