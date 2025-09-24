using System;
using System.Collections.Generic;

public interface IEvent { }

public static class EventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();
    static readonly object bindingsLock = new object();

    public static void Register(IEventBinding<T> binding)
    {
        lock (bindingsLock)
        {
            bindings.Add(binding);
        }
    }

    public static void Deregister(IEventBinding<T> binding)
    {
        lock (bindingsLock)
        {
            bindings.Remove(binding);
        }
    }

    public static void Raise(T @event)
    {
        IEventBinding<T>[] snapshot;

        lock (bindingsLock)
        {
            if (bindings.Count == 0)
            {
                return;
            }

            snapshot = new IEventBinding<T>[bindings.Count];
            bindings.CopyTo(snapshot);
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            var binding = snapshot[i];
            binding.OnEvent.Invoke(@event);
            binding.OnEventNoArgs.Invoke();
        }
    }
}