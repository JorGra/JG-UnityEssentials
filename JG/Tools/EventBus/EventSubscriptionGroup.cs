using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks EventBus subscriptions (and other disposables) for a single owner.
/// </summary>
public sealed class EventSubscriptionGroup : IDisposable
{
    readonly List<TrackedSubscription> tracked = new List<TrackedSubscription>();
    readonly UnityEngine.Object defaultOwner;

    public EventSubscriptionGroup(UnityEngine.Object defaultOwner = null)
    {
        this.defaultOwner = defaultOwner;
    }

    public EventSubscription<T> Listen<T>(UnityEngine.Object owner, Action<T> handler, bool disposeOnDisable = true) where T : IEvent
    {
        var subscription = EventBus<T>.Subscribe(handler, owner ?? defaultOwner);
        Track(subscription, disposeOnDisable);
        return subscription;
    }

    public EventSubscription<T> Listen<T>(UnityEngine.Object owner, Action handler, bool disposeOnDisable = true) where T : IEvent
    {
        var subscription = EventBus<T>.Subscribe(handler, owner ?? defaultOwner);
        Track(subscription, disposeOnDisable);
        return subscription;
    }

    public EventSubscription<T> Listen<T>(Action<T> handler, bool disposeOnDisable = true) where T : IEvent
    {
        return Listen(defaultOwner, handler, disposeOnDisable);
    }

    public EventSubscription<T> Listen<T>(Action handler, bool disposeOnDisable = true) where T : IEvent
    {
        return Listen<T>(defaultOwner, handler, disposeOnDisable);
    }

    public void Track(IDisposable disposable, bool disposeOnDisable = true)
    {
        if (disposable == null)
            return;

        tracked.Add(new TrackedSubscription(disposable, disposeOnDisable));
    }

    public void DisposeOnDisable()
    {
        DisposeTracked(disposeOnlyMarkedForDisable: true);
    }

    public void DisposeAll()
    {
        DisposeTracked(disposeOnlyMarkedForDisable: false);
    }

    public void Dispose()
    {
        DisposeAll();
    }

    void DisposeTracked(bool disposeOnlyMarkedForDisable)
    {
        for (int i = tracked.Count - 1; i >= 0; i--)
        {
            var entry = tracked[i];
            if (disposeOnlyMarkedForDisable && !entry.DisposeOnDisable)
                continue;

            entry.Subscription?.Dispose();
            tracked.RemoveAt(i);
        }
    }

    readonly struct TrackedSubscription
    {
        public readonly IDisposable Subscription;
        public readonly bool DisposeOnDisable;

        public TrackedSubscription(IDisposable subscription, bool disposeOnDisable)
        {
            Subscription = subscription;
            DisposeOnDisable = disposeOnDisable;
        }
    }
}
