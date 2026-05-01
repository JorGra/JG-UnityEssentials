using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Adds a "Open in Terminal" entry to the Project view context menu that
/// launches a terminal at the selected folder's absolute path.
/// </summary>
[InitializeOnLoad]
public static class OpenInTerminal
{
    private const string MenuPath = "Assets/Open in Terminal";

    static OpenInTerminal()
    {
        EditorApplication.projectWindowItemOnGUI -= OnProjectItemGUI;
        EditorApplication.projectWindowItemOnGUI += OnProjectItemGUI;
    }

    // Workaround: Unity's left-pane tree view in Project window does not auto-select
    // the item under the cursor on right-click. Force-select before context menu opens.
    private static void OnProjectItemGUI(string guid, Rect selectionRect)
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.MouseDown || e.button != 1) return;
        if (!selectionRect.Contains(e.mousePosition)) return;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return;

        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        if (obj == null) return;

        Selection.activeObject = obj;
        // Don't consume the event — Unity still needs it to open the context menu.
    }

    [MenuItem(MenuPath, false, 20)]
    private static void OpenSelectedFolder()
    {
        string folder = GetSelectedFolderAbsolutePath();
        if (string.IsNullOrEmpty(folder))
        {
            Debug.LogWarning("[OpenInTerminal] No folder selected.");
            return;
        }

        try
        {
            LaunchTerminal(folder);
        }
        catch (Exception e)
        {
            Debug.LogError($"[OpenInTerminal] Failed to open terminal at '{folder}': {e.Message}");
        }
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidateOpenSelectedFolder()
    {
        return !string.IsNullOrEmpty(GetSelectedFolderAbsolutePath());
    }

    private static string GetSelectedFolderAbsolutePath()
    {
        var obj = Selection.activeObject;
        if (obj == null) return null;

        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(assetPath)) return null;

        string fullPath = Path.GetFullPath(assetPath);
        if (Directory.Exists(fullPath)) return fullPath;
        if (File.Exists(fullPath)) return Path.GetDirectoryName(fullPath);
        return null;
    }

    private static void LaunchTerminal(string folder)
    {
#if UNITY_EDITOR_WIN
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c start \"\" cmd /K \"cd /d \"{folder}\"\"",
            UseShellExecute = false,
            CreateNoWindow = false,
        };
        Process.Start(psi);
#elif UNITY_EDITOR_OSX
        var psi = new ProcessStartInfo
        {
            FileName = "open",
            Arguments = $"-a Terminal \"{folder}\"",
            UseShellExecute = false,
        };
        Process.Start(psi);
#else
        TryLaunchLinuxTerminal(folder); // logs its own diagnostics on failure.
#endif
    }

#if !UNITY_EDITOR_WIN && !UNITY_EDITOR_OSX
    private static readonly string[] BinDirs =
    {
        "/usr/bin", "/usr/local/bin", "/bin", "/opt/bin",
        // Flatpak host bind-mounts:
        "/run/host/usr/bin", "/run/host/usr/local/bin",
        "/var/run/host/usr/bin",
    };

    private static bool IsFlatpak() => File.Exists("/.flatpak-info");
    private static bool IsSnap() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP"));

    private static string SandboxSpawnExe()
    {
        if (IsFlatpak() && File.Exists("/usr/bin/flatpak-spawn")) return "/usr/bin/flatpak-spawn";
        if (IsSnap())
        {
            foreach (string p in new[] { "/usr/bin/host-spawn", "/snap/bin/host-spawn" })
                if (File.Exists(p)) return p;
        }
        return null;
    }

    private static bool TryLaunchLinuxTerminal(string folder)
    {
        var tried = new System.Collections.Generic.List<string>();
        var notFound = new System.Collections.Generic.List<string>();
        string spawnExe = SandboxSpawnExe(); // null when not sandboxed.

        // (executable, args) — args use {0} placeholder for folder.
        var candidates = new (string exe, string args)[]
        {
            ("kgx",            "--working-directory=\"{0}\""),
            ("ptyxis",         "--working-directory=\"{0}\""),
            ("gnome-terminal", "--working-directory=\"{0}\""),
            ("kitty",          "--directory \"{0}\""),
            ("alacritty",      "--working-directory \"{0}\""),
            ("blackbox",       "--working-directory \"{0}\""),
            ("deepin-terminal","--work-directory \"{0}\""),
            ("wezterm",        "start --cwd \"{0}\""),
            ("foot",           "--working-directory=\"{0}\""),
            ("konsole",        "--workdir \"{0}\""),
            ("xfce4-terminal", "--working-directory=\"{0}\""),
            ("tilix",          "--working-directory=\"{0}\""),
            ("terminator",     "--working-directory=\"{0}\""),
            ("mate-terminal",  "--working-directory=\"{0}\""),
            ("lxterminal",     "--working-directory=\"{0}\""),
            ("urxvt",          "-cd \"{0}\""),
            ("xterm",          "-e \"cd '{0}' && $SHELL\""),
        };

        // Sandboxed: ask the host to launch — we don't need to resolve the binary ourselves.
        if (spawnExe != null)
        {
            string envTermSb = Environment.GetEnvironmentVariable("TERMINAL");
            if (!string.IsNullOrEmpty(envTermSb))
            {
                tried.Add($"{spawnExe} --host {envTermSb}");
                if (TryStart(spawnExe, $"--host {envTermSb} --working-directory=\"{folder}\"", folder))
                    return true;
            }
            foreach (var (exe, argsTpl) in candidates)
            {
                string args = string.Format(argsTpl, folder);
                tried.Add($"{spawnExe} --host {exe}");
                if (TryStart(spawnExe, $"--host {exe} {args}", folder)) return true;
            }
            Debug.LogError(
                "[OpenInTerminal] Sandboxed (Flatpak/Snap) — host spawn failed for all candidates.\n" +
                $"Tried via {spawnExe}: {string.Join(", ", tried)}");
            return false;
        }

        string envTerm = Environment.GetEnvironmentVariable("TERMINAL");
        if (!string.IsNullOrEmpty(envTerm))
        {
            string resolved = Resolve(envTerm);
            if (resolved != null)
            {
                tried.Add(resolved);
                if (TryStart(resolved, $"--working-directory=\"{folder}\"", folder)) return true;
            }
            else notFound.Add(envTerm);
        }

        foreach (var (exe, argsTpl) in candidates)
        {
            string resolved = Resolve(exe);
            if (resolved == null) { notFound.Add(exe); continue; }
            tried.Add(resolved);
            string args = string.Format(argsTpl, folder);
            if (TryStart(resolved, args, folder)) return true;
        }

        Debug.LogError(
            "[OpenInTerminal] No terminal launched.\n" +
            $"Flatpak: {IsFlatpak()}, Snap: {IsSnap()}, spawnExe: {spawnExe ?? "<null>"}\n" +
            $"PATH: {Environment.GetEnvironmentVariable("PATH")}\n" +
            $"Resolved+tried (failed to start): {string.Join(", ", tried)}\n" +
            $"Not found on disk: {string.Join(", ", notFound)}");
        return false;
    }

    private static string Resolve(string exe)
    {
        if (Path.IsPathRooted(exe)) return File.Exists(exe) ? exe : null;
        foreach (string dir in BinDirs)
        {
            string full = Path.Combine(dir, exe);
            if (File.Exists(full)) return full;
        }
        string pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            foreach (string dir in pathEnv.Split(':'))
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string full = Path.Combine(dir, exe);
                if (File.Exists(full)) return full;
            }
        }
        return null;
    }

    private static bool TryStart(string exe, string args, string workdir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = workdir,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            // Strip Unity's bundled lib paths so child GUI apps don't crash.
            psi.EnvironmentVariables.Remove("LD_LIBRARY_PATH");
            psi.EnvironmentVariables.Remove("LD_PRELOAD");
            psi.EnvironmentVariables.Remove("DYLD_LIBRARY_PATH");
            psi.EnvironmentVariables.Remove("DYLD_INSERT_LIBRARIES");
            psi.EnvironmentVariables.Remove("MONO_PATH");
            psi.EnvironmentVariables.Remove("MONO_CFG_DIR");

            using var p = Process.Start(psi);
            if (p == null) return false;

            // Give it a moment; if it dies immediately, treat as failure.
            if (p.WaitForExit(400))
            {
                if (p.ExitCode != 0)
                {
                    string err = p.StandardError.ReadToEnd();
                    Debug.LogWarning($"[OpenInTerminal] {exe} exited {p.ExitCode}: {err.Trim()}");
                    return false;
                }
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OpenInTerminal] start '{exe}' threw: {e.Message}");
            return false;
        }
    }
#else
    private static bool TryLaunchLinuxTerminal(string folder) => false;
#endif
}
