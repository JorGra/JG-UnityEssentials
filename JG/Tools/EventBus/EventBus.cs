using System;
using System.Collections.Generic;

public interface IEvent { }

public static class EventBus<T> where T : IEvent
{
    static readonly HashSet<IEventBinding<T>> bindings = new HashSet<IEventBinding<T>>();

    public static void Register(IEventBinding<T> binding)
    {
        bindings.Add(binding);
    }

    public static void Deregister(IEventBinding<T> binding)
    {
        bindings.Remove(binding);
    }

    public static void Raise(T @event)
    {
        foreach (var binding in bindings)
        {
            binding.OnEvent.Invoke(@event);
            binding.OnEventNoArgs.Invoke();
        }
    }
}