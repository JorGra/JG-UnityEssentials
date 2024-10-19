using UnityEngine;
using UnityEditor;
using System;
using System.IO;

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

        [MenuItem("Tools/Screenshot Tool")]
        public static void ShowWindow()
        {
            GetWindow<HighResScreenshotTool>("Screenshot Tool");
        }

        [MenuItem("Tools/Take Screenshot %&s")] // % (ctrl/cmd), & (alt), s
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
        }

        private void OnGUI()
        {
            GUILayout.Label("Screenshot Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            screenshotName = EditorGUILayout.TextField("Screenshot Name", screenshotName);

            resolutionMultiplier = EditorGUILayout.IntPopup("Resolution Multiplier",
                resolutionMultiplier,
                new string[] { "1x", "2x", "3x" },
                new int[] { 1, 2, 3 });

            saveInAssetsFolder = EditorGUILayout.Toggle("Save in Assets Folder", saveInAssetsFolder);

            useCustomPath = EditorGUILayout.Toggle("Use Custom Path", useCustomPath);

            if (useCustomPath)
            {
                EditorGUILayout.BeginHorizontal();
                customPath = EditorGUILayout.TextField("Custom Path", customPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string defaultPath = saveInAssetsFolder ? Application.dataPath : customPath;
                    string title = saveInAssetsFolder ? "Choose Screenshot Directory in Assets" : "Choose Screenshot Directory";

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
            }

            if (GUILayout.Button("Capture Screenshot"))
            {
                CaptureScreenshot();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                $"Keyboard Shortcut: {(Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl")} + Alt + S",
                MessageType.Info);

            // Display the full save path
            string fullSavePath = GetFullSavePath();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Save Location:", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(fullSavePath, EditorStyles.textField, GUILayout.Height(20));
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

            // Start a coroutine to capture the screenshot at the end of the frame
            EditorApplication.delayCall += () =>
            {
                try
                {
                    CaptureScreenshotInternal(fullPath);
                }
                finally
                {
                    isCapturingScreenshot = false;
                }
            };
        }

        private void CaptureScreenshotInternal(string fullPath)
        {
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
            RenderTexture rt = new RenderTexture(width * resolutionMultiplier,
                height * resolutionMultiplier, 24);
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

            if (saveInAssetsFolder)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Screenshot saved: {fullPath}");

            if (openFolderAfterCapture)
            {
                EditorUtility.RevealInFinder(fullPath);
            }
        }
    }
}