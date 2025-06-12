#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Instantly previews an <see cref="AudioClip"/> when it is single-clicked in the
/// Project window, while preserving normal selection behaviour.
/// • Plays only AudioClips (ignores other asset types).  
/// • Selecting a new clip stops the previous one and plays the new selection.  
/// • Clicking the already-selected clip toggles play/pause.  
/// • Uses a hidden <see cref="AudioSource"/> created in the editor for playback.  
/// </summary>
[InitializeOnLoad]
public static class AudioClipInstantPreview
{
    static AudioSource audioSource;
    static bool isPaused;

    // --------------------------------------------------------------------- //
    // Static constructor: initialise once on domain load.
    // --------------------------------------------------------------------- //
    static AudioClipInstantPreview()
    {
        CreateHiddenAudioSource();

        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;

        EditorApplication.quitting += Cleanup;
        AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
    }

    // --------------------------------------------------------------------- //
    // Selection changed: play the newly selected clip (if any).
    // --------------------------------------------------------------------- //
    static void OnSelectionChanged()
    {
        var clip = Selection.activeObject as AudioClip;
        if (clip == null)
        {
            StopPlayback();
            return;
        }

        PlayClip(clip);
    }

    // --------------------------------------------------------------------- //
    // Project-window GUI: detect clicks on the *currently selected* clip
    // to toggle play/pause without blocking normal selection behaviour.
    // --------------------------------------------------------------------- //
    static void OnProjectWindowItemGUI(string guid, Rect rect)
    {
        var e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition))
            return;

        // Determine which asset was clicked.
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var clickedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clickedClip == null)
            return;

        // If it's the clip already selected → toggle play/pause
        // (consume the event so Unity doesn’t treat it as a second selection).
        if (Selection.activeObject == clickedClip)
        {
            TogglePlayPause(clickedClip);
            e.Use();                      // *** only here ***

            // No need to proceed further; we don’t want another selection event.
            return;
        }

        // Else: let Unity process the click normally.
        // OnSelectionChanged will handle playing the new clip.
    }

    // --------------------------------------------------------------------- //
    // Playback helpers
    // --------------------------------------------------------------------- //
    static void PlayClip(AudioClip clip)
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.clip = clip;
        audioSource.Play();
        isPaused = false;
    }

    static void TogglePlayPause(AudioClip clip)
    {
        // Same clip guaranteed.
        if (audioSource.isPlaying && !isPaused)
        {
            audioSource.Pause();
            isPaused = true;
        }
        else
        {
            // If paused or not playing at all, resume/start.
            if (audioSource.clip != clip)
                audioSource.clip = clip;

            audioSource.UnPause();
            if (!audioSource.isPlaying)
                audioSource.Play();

            isPaused = false;
        }
    }

    static void StopPlayback()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        isPaused = false;
    }

    static void CreateHiddenAudioSource()
    {
        var go = new GameObject("AudioClipPreviewPlayer")
        {
            hideFlags = HideFlags.HideAndDontSave | HideFlags.DontSaveInBuild
        };

        audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D
        audioSource.loop = false;
    }

    static void Cleanup()
    {
        if (audioSource != null)
        {
            Object.DestroyImmediate(audioSource.gameObject);
            audioSource = null;
        }
    }
}
#endif
