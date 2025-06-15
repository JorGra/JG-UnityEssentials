// HoverMaterialPreviewDrawer.cs   (put in an *Editor* folder)
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Material))]
public class HoverMaterialPreviewDrawer : PropertyDrawer
{
    // -----------------------------------------------------------------------
    // Constants
    // -----------------------------------------------------------------------
    const float kPreviewSize = 96f;      // square thumbnail (px)
    const float kPadding = 4f;       // gap between field and thumb

    // -----------------------------------------------------------------------
    // Per-editor-session caches
    // -----------------------------------------------------------------------
    static readonly Dictionary<int, Texture> s_PreviewCache = new();       // matID → preview
    static readonly Dictionary<int, bool> s_LoadingFlag = new();       // matID → waiting?
    static Mesh s_SphereMesh;                 // hi-poly sphere

    // -----------------------------------------------------------------------
    // Main IMGUI entry
    // -----------------------------------------------------------------------
    public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label)
    {
        // 1️⃣ Draw the normal object field
        EditorGUI.PropertyField(pos, prop, label, true);

        // 2️⃣ Hover check – only during Repaint (lowest cost)
        if (Event.current.type != EventType.Repaint) return;
        if (!pos.Contains(Event.current.mousePosition)) return;

        var mat = prop.objectReferenceValue as Material;
        if (!mat) return;

        // 3️⃣ Fetch (or create) a preview texture
        if (!TryGetPreview(mat, out var tex)) return;

        // 4️⃣ Decide where to place the thumbnail.
        //    We only need to know if there's room *above* inside the Inspector
        //    ̶n̶o̶t̶ inside the scroll-view (scroll clipping does NOT hide GUI
        //    drawn at Int32.MaxValue depth).
        var inspectorRect = EditorWindow.focusedWindow.position;
        bool roomAbove = pos.y - inspectorRect.yMin >= kPreviewSize + kPadding;

        var thumbRect = new Rect(
            pos.xMax - kPreviewSize,
            roomAbove ? pos.y - kPreviewSize - kPadding
                      : pos.yMax + kPadding,
            kPreviewSize,
            kPreviewSize);

        // 5️⃣ Draw *after* every other control so nothing can overlap us.
        int oldDepth = GUI.depth;
        GUI.depth = int.MaxValue;        // higher depth → drawn last (on top)

        GUI.Box(thumbRect, GUIContent.none, EditorStyles.helpBox);
        GUI.DrawTexture(thumbRect, tex, ScaleMode.ScaleToFit, true);

        GUI.depth = oldDepth;
    }

    public override float GetPropertyHeight(SerializedProperty p, GUIContent l)
        => EditorGUI.GetPropertyHeight(p, l, true);

    // -----------------------------------------------------------------------
    // Preview helpers
    // -----------------------------------------------------------------------
    static bool TryGetPreview(Material mat, out Texture tex)
    {
        int id = mat.GetInstanceID();

        // Already cached?
        if (s_PreviewCache.TryGetValue(id, out tex))
            return tex != null;

        // 1️⃣ First ask Unity's AssetPreview system (works for *assets*)
        tex = AssetPreview.GetAssetPreview(mat);
        if (tex)
        {
            s_PreviewCache[id] = tex;
            return true;
        }

        // 2️⃣ If Unity is still *building* the preview, let it finish
        if (AssetPreview.IsLoadingAssetPreview(id))
        {
            // Only request another repaint once (per material) while loading
            if (!s_LoadingFlag.TryGetValue(id, out bool alreadyQueued) || !alreadyQueued)
            {
                s_LoadingFlag[id] = true;
                EditorApplication.QueuePlayerLoopUpdate();   // repaint next frame
            }
            return false;
        }

        // 3️⃣ Scene-only material → render our own sphere once
        tex = RenderSphere(mat);
        s_PreviewCache[id] = tex;            // may be null if shader compiling
        return tex;
    }

    // -----------------------------------------------------------------------
    // One-off custom sphere render (for non-asset materials)
    // -----------------------------------------------------------------------
    static Texture RenderSphere(Material mat)
    {
        const int size = 128;

        var pr = new PreviewRenderUtility();
        pr.cameraFieldOfView = 15f;

        pr.lights[0].intensity = pr.lights[1].intensity = 1.3f;
        pr.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);

        Mesh sphere = GetSphereMesh();

        pr.BeginPreview(new Rect(0, 0, size, size), GUIStyle.none);
        pr.DrawMesh(sphere, Matrix4x4.identity, mat, 0);
        pr.camera.transform.position = new Vector3(0, 0, -5f);
        pr.camera.transform.LookAt(Vector3.zero);
        pr.camera.nearClipPlane = 0.1f;
        pr.camera.farClipPlane = 10f;
        pr.camera.Render();
        var tex = pr.EndPreview();
        pr.Cleanup();

        return tex;
    }

    // -----------------------------------------------------------------------
    // Robust sphere mesh lookup (works on every Unity version)
    // -----------------------------------------------------------------------
    static Mesh GetSphereMesh()
    {
        if (s_SphereMesh) return s_SphereMesh;

        // 1️⃣ Unity 2020.1+: PreviewRenderUtility.GetPreviewSphere()
        MethodInfo m = typeof(PreviewRenderUtility)
                       .GetMethod("GetPreviewSphere",
                                  BindingFlags.Static | BindingFlags.Public);
        if (m != null)
        {
            s_SphereMesh = m.Invoke(null, null) as Mesh;
            if (s_SphereMesh) return s_SphereMesh;
        }

        // 2️⃣ Built-in resource (stable path since 5.x)
        s_SphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        if (s_SphereMesh) return s_SphereMesh;

        // 3️⃣ Last-chance fallback – steal from a temp primitive
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s_SphereMesh = go.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(go);
        return s_SphereMesh;
    }
}
