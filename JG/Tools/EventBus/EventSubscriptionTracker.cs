using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hidden helper that keeps event subscriptions per-owner and disposes them when the owner disables or is destroyed.
/// </summary>
[AddComponentMenu("")]
[DefaultExecutionOrder(int.MinValue)]
sealed class EventSubscriptionTracker : MonoBehaviour
{
    readonly Dictionary<MonoBehaviour, OwnerEntry> entries = new Dictionary<MonoBehaviour, OwnerEntry>();
    static readonly List<MonoBehaviour> scratchOwners = new List<MonoBehaviour>();
    static readonly HashSet<EventSubscriptionTracker> activeTrackers = new HashSet<EventSubscriptionTracker>();
    static readonly List<EventSubscriptionTracker> trackerScratch = new List<EventSubscriptionTracker>();

    void Awake()
    {
        hideFlags = HideFlags.HideInInspector | HideFlags.HideAndDontSave;
    }

    void OnEnable()
    {
        activeTrackers.Add(this);
    }

    internal EventSubscriptionGroup GetGroupFor(MonoBehaviour owner)
    {
        if (!entries.TryGetValue(owner, out var entry))
        {
            entry = new OwnerEntry(new EventSubscriptionGroup(owner), owner != null && owner.isActiveAndEnabled);
            entries.Add(owner, entry);
        }
        return entry.Group;
    }

    void LateUpdate()
    {
        if (entries.Count == 0)
            return;

        scratchOwners.Clear();
        scratchOwners.AddRange(entries.Keys);

        for (int i = 0; i < scratchOwners.Count; i++)
        {
            var owner = scratchOwners[i];
            if (!entries.TryGetValue(owner, out var entry))
                continue;

            if (owner == null)
            {
                entry.Group.DisposeAll();
                entries.Remove(owner);
                continue;
            }

            bool isEnabled = owner.isActiveAndEnabled;
            if (!isEnabled && entry.LastKnownEnabled)
            {
                entry.Group.DisposeOnDisable();
                entry = new OwnerEntry(entry.Group, false);
                entries[owner] = entry;
            }
            else if (isEnabled && !entry.LastKnownEnabled)
            {
                entry = new OwnerEntry(entry.Group, true);
                entries[owner] = entry;
            }
        }
    }

    void OnDisable()
    {
        DisposeEntries(disposeOnlyMarkedForDisable: true);
        activeTrackers.Remove(this);
    }

    void OnDestroy()
    {
        DisposeEntries(disposeOnlyMarkedForDisable: false);
        activeTrackers.Remove(this);
    }

    void DisposeEntries(bool disposeOnlyMarkedForDisable)
    {
        if (entries.Count == 0)
            return;

        scratchOwners.Clear();
        scratchOwners.AddRange(entries.Keys);

        for (int i = 0; i < scratchOwners.Count; i++)
        {
            var owner = scratchOwners[i];
            if (!entries.TryGetValue(owner, out var entry))
                continue;

            if (owner == null)
            {
                entry.Group.DisposeAll();
                entries.Remove(owner);
                continue;
            }

            if (disposeOnlyMarkedForDisable)
            {
                entry.Group.DisposeOnDisable();
                entries[owner] = new OwnerEntry(entry.Group, false);
            }
            else
            {
                entry.Group.DisposeAll();
                entries.Remove(owner);
            }
        }

        if (!disposeOnlyMarkedForDisable)
        {
            entries.Clear();
        }
    }

    internal static void DisposeAllTrackers()
    {
        if (activeTrackers.Count == 0)
            return;

        trackerScratch.Clear();
        trackerScratch.AddRange(activeTrackers);

        for (int i = 0; i < trackerScratch.Count; i++)
        {
            var tracker = trackerScratch[i];
            if (tracker == null)
                continue;

            tracker.DisposeEntries(disposeOnlyMarkedForDisable: false);
        }
    }

    internal static EventSubscriptionTracker GetOrCreate(MonoBehaviour owner)
    {
        var go = owner.gameObject;
        var tracker = go.GetComponent<EventSubscriptionTracker>();
        if (tracker == null)
        {
            tracker = go.AddComponent<EventSubscriptionTracker>();
        }
        return tracker;
    }

    struct OwnerEntry
    {
        public readonly EventSubscriptionGroup Group;
        public readonly bool LastKnownEnabled;

        public OwnerEntry(EventSubscriptionGroup group, bool lastKnownEnabled)
        {
            Group = group;
            LastKnownEnabled = lastKnownEnabled;
        }
    }
}
