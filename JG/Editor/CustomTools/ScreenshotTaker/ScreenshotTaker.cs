using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JG.Tools.Editor
{
    public class HighResScreenshotTool : EditorWindow
    {
        private string screenshotName = "Screenshot";
        private int resolutionMultiplier = 1;
        private string defaultSavePath = "Screenshots";
        private bool useCustomPath = false;
        private string customPath = "";
        private bool openFolderAfterCapture = true;
        private bool isCapturingScreenshot = false;
        private bool saveInAssetsFolder = true;
        private bool includeUI = true;

        // NEW: transparent output option
        private bool transparentBackground = false;

        // Foldout state for sections
        private bool showFileSettings = true;
        private bool showCaptureSettings = true;

        [MenuItem("Tools/Screenshot Tool", false, 2501)]
        public static void ShowWindow()
        {
            GetWindow<HighResScreenshotTool>("Screenshot Tool");
        }

        [MenuItem("Tools/Take Screenshot %&s", false, 3500)] // % (ctrl/cmd), & (alt), s
        private static void CaptureScreenshotShortcut()
        {
            var window = GetWindow<HighResScreenshotTool>();
            window.CaptureScreenshot();
        }

        private void OnEnable()
        {
            // Load saved preferences
            screenshotName = EditorPrefs.GetString("HighResScreenshotTool_Name", screenshotName);
            resolutionMultiplier = EditorPrefs.GetInt("HighResScreenshotTool_Multiplier", resolutionMultiplier);
            defaultSavePath = EditorPrefs.GetString("HighResScreenshotTool_SavePath", defaultSavePath);
            useCustomPath = EditorPrefs.GetBool("HighResScreenshotTool_UseCustomPath", useCustomPath);
            customPath = EditorPrefs.GetString("HighResScreenshotTool_CustomPath", customPath);
            openFolderAfterCapture = EditorPrefs.GetBool("HighResScreenshotTool_OpenFolder", openFolderAfterCapture);
            saveInAssetsFolder = EditorPrefs.GetBool("HighResScreenshotTool_SaveInAssets", saveInAssetsFolder);
            includeUI = EditorPrefs.GetBool("HighResScreenshotTool_IncludeUI", includeUI);
            transparentBackground = EditorPrefs.GetBool("HighResScreenshotTool_TransparentBG", transparentBackground);
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Capture Screenshot"))
            {
                CaptureScreenshot();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Shortcut: {(Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl")} + Alt + S",
                MessageType.Info);

            // File Settings Section
            showFileSettings = EditorGUILayout.Foldout(showFileSettings, "File Settings", true);
            if (showFileSettings)
            {
                EditorGUI.indentLevel++;

                screenshotName = EditorGUILayout.TextField("Screenshot Name", screenshotName);

                saveInAssetsFolder = EditorGUILayout.Toggle("Save in Assets Folder", saveInAssetsFolder);
                useCustomPath = EditorGUILayout.Toggle("Use Custom Path", useCustomPath);

                if (useCustomPath)
                {
                    EditorGUILayout.BeginHorizontal();
                    customPath = EditorGUILayout.TextField("Custom Path", customPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(60)))
                    {
                        string defaultPath = saveInAssetsFolder ? Application.dataPath : customPath;
                        string title = saveInAssetsFolder ? "Choose Directory in Assets" : "Choose Directory";
                        string path = EditorUtility.OpenFolderPanel(title, defaultPath, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (saveInAssetsFolder)
                            {
                                if (path.StartsWith(Application.dataPath))
                                {
                                    customPath = "Assets" + path.Substring(Application.dataPath.Length);
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Invalid Path",
                                        "When 'Save in Assets Folder' is enabled, please select a folder within the Assets directory.", "OK");
                                }
                            }
                            else
                            {
                                customPath = path;
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else if (saveInAssetsFolder)
                {
                    defaultSavePath = EditorGUILayout.TextField("Save Folder", defaultSavePath);
                }

                openFolderAfterCapture = EditorGUILayout.Toggle("Open Folder After Capture", openFolderAfterCapture);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Capture Settings Section
            showCaptureSettings = EditorGUILayout.Foldout(showCaptureSettings, "Capture Settings", true);
            if (showCaptureSettings)
            {
                EditorGUI.indentLevel++;

                resolutionMultiplier = EditorGUILayout.IntPopup("Resolution Multiplier",
                    resolutionMultiplier,
                    new string[] { "1x", "2x", "3x" },
                    new int[] { 1, 2, 3 });

                includeUI = EditorGUILayout.Toggle(new GUIContent("Include UI in Screenshot",
                    "If you also enable Transparent Background, overlay canvases will be temporarily rendered via the camera for this capture."),
                    includeUI);

                transparentBackground = EditorGUILayout.Toggle(new GUIContent("Transparent Background",
                    "Renders to a RenderTexture with alpha and clears with Color.a = 0. Skyboxes are disabled for the capture."),
                    transparentBackground);

                if (transparentBackground && includeUI)
                {
                    EditorGUILayout.HelpBox(
                        "Including UI with transparency requires canvases to render via a Camera. " +
                        "Screen Space – Overlay canvases will be temporarily switched to Screen Space – Camera and restored after the capture.",
                        MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Persist settings
            if (GUI.changed)
            {
                EditorPrefs.SetString("HighResScreenshotTool_Name", screenshotName);
                EditorPrefs.SetInt("HighResScreenshotTool_Multiplier", resolutionMultiplier);
                EditorPrefs.SetString("HighResScreenshotTool_SavePath", defaultSavePath);
                EditorPrefs.SetBool("HighResScreenshotTool_UseCustomPath", useCustomPath);
                EditorPrefs.SetString("HighResScreenshotTool_CustomPath", customPath);
                EditorPrefs.SetBool("HighResScreenshotTool_OpenFolder", openFolderAfterCapture);
                EditorPrefs.SetBool("HighResScreenshotTool_SaveInAssets", saveInAssetsFolder);
                EditorPrefs.SetBool("HighResScreenshotTool_IncludeUI", includeUI);
                EditorPrefs.SetBool("HighResScreenshotTool_TransparentBG", transparentBackground);
            }

            // Display the full save path + open folder
            string fullSavePath = GetFullSavePath();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save Location:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(fullSavePath, EditorStyles.textField, GUILayout.Height(20));

            if (GUILayout.Button("Open Screenshot Folder"))
            {
                OpenScreenshotFolder();
            }
        }

        private string GetFullSavePath()
        {
            if (useCustomPath)
                return customPath;
            else if (saveInAssetsFolder)
                return Path.Combine(Application.dataPath, defaultSavePath);
            else
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Unity Screenshots");
        }

        private void OpenScreenshotFolder()
        {
            string folderPath = GetFullSavePath();
            if (Directory.Exists(folderPath))
                EditorUtility.RevealInFinder(folderPath);
            else
                EditorUtility.DisplayDialog("Folder not found", "The screenshot folder does not exist.", "OK");
        }

        private void CaptureScreenshot()
        {
            if (isCapturingScreenshot) return;
            isCapturingScreenshot = true;

            string folderPath = GetFullSavePath();
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"{screenshotName}_{timestamp}.png";
            string fullPath = Path.Combine(folderPath, filename);

            // If not transparent AND we want UI, use the simple full-screen capture.
            if (includeUI && !transparentBackground)
            {
                ScreenCapture.CaptureScreenshot(fullPath, resolutionMultiplier);
                Debug.Log($"Screenshot with UI saved: {fullPath}");
                if (openFolderAfterCapture) EditorUtility.RevealInFinder(fullPath);
                isCapturingScreenshot = false;
                return;
            }

            // Otherwise render manually (supports transparency and/or no-UI)
            CaptureToRenderTexture(fullPath, includeUI, transparentBackground);

            if (openFolderAfterCapture)
                EditorUtility.RevealInFinder(fullPath);

            isCapturingScreenshot = false;
        }

        private struct CanvasState
        {
            public Canvas canvas;
            public RenderMode renderMode;
            public Camera worldCamera;
            public float planeDistance;
        }

        private void CaptureToRenderTexture(string fullPath, bool includeUIInRT, bool transparentBG)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Error", "No main camera found in the scene.", "OK");
                return;
            }

            // Determine target size from current Game View
            Vector2 gv = Handles.GetMainGameViewSize();
            int width = Mathf.Max(1, (int)gv.x) * resolutionMultiplier;
            int height = Mathf.Max(1, (int)gv.y) * resolutionMultiplier;

            // Setup RT with alpha
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing)
            };

            // Save camera state
            var prevTarget = camera.targetTexture;
            var prevFlags = camera.clearFlags;
            var prevBG = camera.backgroundColor;

            // For transparency we need SolidColor clear with alpha 0 and NO skybox
            if (transparentBG)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                Color c = prevBG; c.a = 0f;
                camera.backgroundColor = c;
            }

            // Optionally coerce Overlay canvases to render via this camera (so they end up in the RT)
            List<CanvasState> changedCanvases = new List<CanvasState>();
            if (includeUIInRT)
            {
                Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
                foreach (var cv in canvases)
                {
                    if (!cv.isRootCanvas) continue;

                    // Save state
                    changedCanvases.Add(new CanvasState
                    {
                        canvas = cv,
                        renderMode = cv.renderMode,
                        worldCamera = cv.worldCamera,
                        planeDistance = cv.planeDistance
                    });

                    if (cv.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        cv.renderMode = RenderMode.ScreenSpaceCamera;
                        cv.worldCamera = camera;
                        // Keep it just in front of the camera
                        cv.planeDistance = 1f;
                    }
                    else if (cv.renderMode == RenderMode.ScreenSpaceCamera && cv.worldCamera == null)
                    {
                        // Make sure it renders through our camera
                        cv.worldCamera = camera;
                    }
                }

                Canvas.ForceUpdateCanvases();
            }

            // Render
            RenderTexture prevActive = RenderTexture.active;
            camera.targetTexture = rt;
            RenderTexture.active = rt;

            // If transparency AND we kept Skybox clear flags, clear manually anyway (safety)
            if (transparentBG)
            {
                GL.Clear(true, true, new Color(0, 0, 0, 0));
            }

            camera.Render();

            // Readback (RGBA if transparent, else RGB)
            Texture2D tex = new Texture2D(rt.width, rt.height, transparentBG ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            // Restore canvases
            if (includeUIInRT)
            {
                foreach (var s in changedCanvases)
                {
                    if (s.canvas == null) continue;
                    s.canvas.renderMode = s.renderMode;
                    s.canvas.worldCamera = s.worldCamera;
                    s.canvas.planeDistance = s.planeDistance;
                }
                Canvas.ForceUpdateCanvases();
            }

            // Restore camera/RT
            camera.targetTexture = prevTarget;
            camera.clearFlags = prevFlags;
            camera.backgroundColor = prevBG;
            RenderTexture.active = prevActive;

            // Save PNG
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);

            // Cleanup
            DestroyImmediate(tex);
            rt.Release();
            DestroyImmediate(rt);

            Debug.Log($"Screenshot {(transparentBG ? "with transparent background " : "")}saved: {fullPath}");
        }
    }
}
