using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class EventBus<T> where T : IEvent
{
    static readonly List<BindingEntry> bindings = new List<BindingEntry>();
    static bool warnOnClear = true;

    public static void Register(IEventBinding<T> binding) => Register(binding, null);

    public static void Register(IEventBinding<T> binding, UnityEngine.Object owner) => Register(binding, owner, true);

    public static void Register(IEventBinding<T> binding, UnityEngine.Object owner, bool autoDeregisterOnOwnerDestroyed)
    {
        if (binding == null)
            throw new ArgumentNullException(nameof(binding));

        PruneDeadBindings();

        // Prevent duplicate registrations of the same binding instance.
        for (int i = 0; i < bindings.Count; i++)
        {
            if (ReferenceEquals(bindings[i].Binding, binding))
            {
                return;
            }
        }

        bindings.Add(new BindingEntry(binding, owner, autoDeregisterOnOwnerDestroyed));
    }

    public static EventSubscription<T> Subscribe(Action<T> handler, UnityEngine.Object owner = null, bool autoDeregisterOnOwnerDestroyed = true)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var binding = new EventBinding<T>(handler);
        Register(binding, owner, autoDeregisterOnOwnerDestroyed);
        return new EventSubscription<T>(binding);
    }

    public static EventSubscription<T> Subscribe(Action handler, UnityEngine.Object owner = null, bool autoDeregisterOnOwnerDestroyed = true)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var binding = new EventBinding<T>(handler);
        Register(binding, owner, autoDeregisterOnOwnerDestroyed);
        return new EventSubscription<T>(binding);
    }

    public static void Deregister(IEventBinding<T> binding)
    {
        if (binding == null)
            return;

        for (int i = bindings.Count - 1; i >= 0; i--)
        {
            var entry = bindings[i];
            if (ReferenceEquals(entry.Binding, binding))
            {
                bindings.RemoveAt(i);
                entry.MarkDisposed(BindingRemovalReason.Manual, entry.OwnerAliveForWarnings);
                return;
            }
        }
    }

    public static void Raise(T @event)
    {
        PruneDeadBindings();

        // Snapshot current bindings to allow modifications during invocation.
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

    enum BindingRemovalReason
    {
        None,
        Manual,
        Cleared,
        OwnerDestroyed
    }

    sealed class BindingEntry
    {
        readonly IEventBinding<T> binding;
        readonly UnityEngine.Object ownerReference;
        readonly bool ownerSupplied;
        readonly bool autoDeregisterOnOwnerDestroyed;
        readonly string ownerName;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        readonly string capturedStackTrace;
#endif

        bool disposed;

        public BindingEntry(IEventBinding<T> binding, UnityEngine.Object ownerReference, bool autoDeregisterOnOwnerDestroyed)
        {
            this.binding = binding;
            this.ownerReference = ownerReference;
            this.autoDeregisterOnOwnerDestroyed = autoDeregisterOnOwnerDestroyed;
            ownerSupplied = ownerReference != null;
            ownerName = ownerSupplied ? ownerReference.name : "<no owner>";
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            capturedStackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: true).ToString();
#endif
        }

        public IEventBinding<T> Binding => binding;

        public bool ShouldAutoPrune => autoDeregisterOnOwnerDestroyed && ownerSupplied && ownerReference == null;

        public bool OwnerAliveForWarnings => ownerSupplied ? ownerReference != null : true;

        public void Invoke(T @event)
        {
            binding.OnEvent?.Invoke(@event);
            binding.OnEventNoArgs?.Invoke();
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

            // If no owner was provided, treat it as alive for leak warnings.
            return ownerAliveAtRemoval;
        }
#endif
    }
}

public sealed class EventSubscription<T> : IDisposable where T : IEvent
{
    readonly IEventBinding<T> binding;
    bool disposed;

    internal EventSubscription(IEventBinding<T> binding)
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




