using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    static readonly List<BindingEntry> bindings = new List<BindingEntry>();
    static bool warnOnClear = true;

    public static EventSubscription<T> Subscribe(Action<T> handler, UnityEngine.Object owner = null, bool autoDeregisterOnOwnerDestroyed = true)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        return AddBinding(handler, null, owner, autoDeregisterOnOwnerDestroyed);
    }

    public static EventSubscription<T> Subscribe(Action handler, UnityEngine.Object owner = null, bool autoDeregisterOnOwnerDestroyed = true)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        return AddBinding(null, handler, owner, autoDeregisterOnOwnerDestroyed);
    }

    static EventSubscription<T> AddBinding(Action<T> onEvent, Action onEventNoArgs, UnityEngine.Object owner, bool autoDeregisterOnOwnerDestroyed)
    {
        PruneDeadBindings();

        var entry = new BindingEntry(onEvent, onEventNoArgs, owner, autoDeregisterOnOwnerDestroyed);
        bindings.Add(entry);
        return new EventSubscription<T>(entry);
    }

    internal static void Deregister(BindingEntry entry)
    {
        if (entry == null)
            return;

        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            var candidate = bindings[i];
            if (ReferenceEquals(candidate, entry))
            {
                bindings.RemoveAt(i);
                candidate.MarkDisposed(BindingRemovalReason.Manual, candidate.OwnerAliveForWarnings);
                return;
            }
        }
    }

    public static void Raise(T @event)
    {
        PruneDeadBindings();

        var snapshot = bindings.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            snapshot[i].Invoke(@event);
        }
    }

    static void PruneDeadBindings()
    {
        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            var entry = bindings[i];
            if (entry.ShouldAutoPrune)
            {
                bindings.RemoveAt(i);
                entry.MarkDisposed(BindingRemovalReason.OwnerDestroyed, ownerAliveAtRemoval: false);
            }
        }
    }

    internal static void SetWarnOnClear(bool shouldWarn) => warnOnClear = shouldWarn;

    static void Clear()
    {
        var shouldWarn = warnOnClear;
        warnOnClear = true;

        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            var entry = bindings[i];
            bindings.RemoveAt(i);
            var ownerAlive = shouldWarn && entry.OwnerAliveForWarnings;
            entry.MarkDisposed(BindingRemovalReason.Cleared, ownerAlive);
        }
    }

    internal enum BindingRemovalReason
    {
        None,
        Manual,
        Cleared,
        OwnerDestroyed
    }

    internal sealed class BindingEntry
    {
        readonly Action<T> onEvent;
        readonly Action onEventNoArgs;
        readonly UnityEngine.Object ownerReference;
        readonly bool ownerSupplied;
        readonly bool autoDeregisterOnOwnerDestroyed;
        readonly string ownerName;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        readonly string capturedStackTrace;
#endif

        bool disposed;

        public BindingEntry(Action<T> onEvent, Action onEventNoArgs, UnityEngine.Object ownerReference, bool autoDeregisterOnOwnerDestroyed)
        {
            this.onEvent = onEvent;
            this.onEventNoArgs = onEventNoArgs;
            this.ownerReference = ownerReference;
            this.autoDeregisterOnOwnerDestroyed = autoDeregisterOnOwnerDestroyed;
            ownerSupplied = ownerReference != null;
            ownerName = ownerSupplied ? ownerReference.name : "<no owner>";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            capturedStackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: true).ToString();
#endif
        }

        public bool ShouldAutoPrune => autoDeregisterOnOwnerDestroyed && ownerSupplied && ownerReference == null;

        public bool OwnerAliveForWarnings => ownerSupplied ? ownerReference != null : true;

        public void Invoke(T @event)
        {
            onEvent?.Invoke(@event);
            onEventNoArgs?.Invoke();
        }

        public void MarkDisposed(BindingRemovalReason reason, bool ownerAliveAtRemoval)
        {
            if (disposed)
                return;

            disposed = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (ShouldWarn(reason, ownerAliveAtRemoval))
            {
                var reasonText = reason switch
                {
                    BindingRemovalReason.Cleared => "play mode exit or manual bus clear",
                    BindingRemovalReason.OwnerDestroyed => "owner destroyed without unsubscribe",
                    _ => "unknown reason"
                };

                UnityEngine.Debug.LogWarning(
                    $"EventBus<{typeof(T).Name}> detected subscription leak (owner: {ownerName}). " +
                    $"The binding was removed due to {reasonText}.\n" +
                    capturedStackTrace
                );
            }
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static bool ShouldWarn(BindingRemovalReason reason, bool ownerAliveAtRemoval)
        {
            if (reason == BindingRemovalReason.Manual)
                return false;

            return ownerAliveAtRemoval;
        }
#endif
    }
}

public sealed class EventSubscription<T> : IDisposable where T : IEvent
{
    readonly EventBus<T>.BindingEntry binding;
    bool disposed;

    internal EventSubscription(EventBus<T>.BindingEntry binding)
    {
        this.binding = binding;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        EventBus<T>.Deregister(binding);
    }
}
