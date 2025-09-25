using System;
using UnityEngine;

public static class EventSubscriptionExtensions
{
    public static EventSubscription<T> SubscribeEvent<T>(this MonoBehaviour owner, Action<T> handler, bool disposeOnDisable = true) where T : IEvent
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var tracker = EventSubscriptionTracker.GetOrCreate(owner);
        var group = tracker.GetGroupFor(owner);
        return group.Listen<T>(owner, handler, disposeOnDisable);
    }

    public static EventSubscription<T> SubscribeEvent<T>(this MonoBehaviour owner, Action handler, bool disposeOnDisable = true) where T : IEvent
    {
        if (owner == null)
            throw new ArgumentNullException(nameof(owner));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var tracker = EventSubscriptionTracker.GetOrCreate(owner);
        var group = tracker.GetGroupFor(owner);
        return group.Listen<T>(owner, handler, disposeOnDisable);
    }
}
