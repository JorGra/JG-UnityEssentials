using UnityEngine;

public static class ComponentExtensions
{
    /// <summary>
    /// Attempts to find a component of type <typeparamref name="T"/> on the
    /// calling object or any of its parents.
    /// </summary>
    /// <remarks>
    /// Works with both Component-derived classes **and** interfaces.
    /// Returns <c>true</c> if found; <c>false</c> otherwise.
    /// </remarks>
    public static bool TryGetComponentInParent<T>(
        this Component caller,
        out T found,
        bool includeInactive = false) where T : class
    {
        found = null;

        if (caller == null) return false;

        Transform current = caller.transform;

        while (current != null)
        {
            // For each Component on this Transform…
            foreach (var comp in current.GetComponents<Component>())
            {
                if (!includeInactive && !comp.gameObject.activeInHierarchy)
                    continue;

                if (comp is T match)
                {
                    found = match;
                    return true;
                }
            }

            current = current.parent;   // climb up one level
        }

        return false;                   // none found
    }
}
