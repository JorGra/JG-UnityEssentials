using System;
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
        private bool includeUI = true; // Option to include UI or not

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
            includeUI = EditorPrefs.GetBool("HighResScreenshotTool_IncludeUI", includeUI); // Include UI preference
        }

        private void OnGUI()
        {
            //GUILayout.Label("Screenshot Tool", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

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

                // Save Path Settings
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

                includeUI = EditorGUILayout.Toggle("Include UI in Screenshot", includeUI); // Toggle for UI inclusion

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Apply changes if any
            if (EditorGUI.EndChangeCheck())
            {
                // Save preferences
                EditorPrefs.SetString("HighResScreenshotTool_Name", screenshotName);
                EditorPrefs.SetInt("HighResScreenshotTool_Multiplier", resolutionMultiplier);
                EditorPrefs.SetString("HighResScreenshotTool_SavePath", defaultSavePath);
                EditorPrefs.SetBool("HighResScreenshotTool_UseCustomPath", useCustomPath);
                EditorPrefs.SetString("HighResScreenshotTool_CustomPath", customPath);
                EditorPrefs.SetBool("HighResScreenshotTool_OpenFolder", openFolderAfterCapture);
                EditorPrefs.SetBool("HighResScreenshotTool_SaveInAssets", saveInAssetsFolder);
                EditorPrefs.SetBool("HighResScreenshotTool_IncludeUI", includeUI); // Save UI preference
            }

            EditorGUILayout.Space();

            // Display the full save path
            string fullSavePath = GetFullSavePath();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save Location:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(fullSavePath, EditorStyles.textField, GUILayout.Height(20));

            // Open Screenshot Folder Button
            if (GUILayout.Button("Open Screenshot Folder"))
            {
                OpenScreenshotFolder();
            }
        }

        private string GetFullSavePath()
        {
            if (useCustomPath)
            {
                return customPath;
            }
            else if (saveInAssetsFolder)
            {
                return Path.Combine(Application.dataPath, defaultSavePath);
            }
            else
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Unity Screenshots");
            }
        }

        private void OpenScreenshotFolder()
        {
            string folderPath = GetFullSavePath();
            if (Directory.Exists(folderPath))
            {
                EditorUtility.RevealInFinder(folderPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Folder not found", "The screenshot folder does not exist.", "OK");
            }
        }

        private void CaptureScreenshot()
        {
            if (isCapturingScreenshot) return;
            isCapturingScreenshot = true;

            string folderPath = GetFullSavePath();

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate filename with timestamp
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"{screenshotName}_{timestamp}.png";
            string fullPath = Path.Combine(folderPath, filename);

            // If you want to include UI and capture everything on screen
            if (includeUI)
            {
                // Use ScreenCapture to capture everything on the screen
                ScreenCapture.CaptureScreenshot(fullPath, resolutionMultiplier);
                Debug.Log($"Screenshot with UI saved: {fullPath}");
            }
            else
            {
                // If you don't want to include UI, use manual rendering (could also use RenderTexture)
                CaptureWithoutUI(fullPath);
            }

            if (openFolderAfterCapture)
            {
                EditorUtility.RevealInFinder(fullPath);
            }

            isCapturingScreenshot = false;
        }

        private void CaptureWithoutUI(string fullPath)
        {
            // Manual camera rendering code for game view only without UI
            Camera camera = Camera.main;
            if (camera == null)
            {
                EditorUtility.DisplayDialog("Error", "No main camera found in the scene.", "OK");
                return;
            }

            // Get the current game view resolution
            int width = (int)Handles.GetMainGameViewSize().x;
            int height = (int)Handles.GetMainGameViewSize().y;

            // Create a render texture with the multiplied resolution
            RenderTexture rt = new RenderTexture(width * resolutionMultiplier, height * resolutionMultiplier, 24);
            RenderTexture prev = camera.targetTexture;
            RenderTexture.active = rt;
            camera.targetTexture = rt;

            // Render the camera to the render texture
            camera.Render();

            // Create a texture2D and read the render texture
            Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            screenshot.Apply();

            // Clean up
            camera.targetTexture = prev;
            RenderTexture.active = null;
            DestroyImmediate(rt);

            // Save the screenshot
            byte[] bytes = screenshot.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            DestroyImmediate(screenshot);

            Debug.Log($"Screenshot without UI saved: {fullPath}");
        }
    }
}
