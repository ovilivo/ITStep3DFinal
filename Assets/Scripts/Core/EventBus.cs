using System;
using System.Collections.Generic;

/// <summary>
/// Lightweight static event bus for decoupled cross-system communication.
/// Usage: EventBus.Subscribe&lt;MyEvent&gt;(handler) / EventBus.Publish(new MyEvent())
/// Always Unsubscribe in OnDisable/OnDestroy to avoid stale listeners.
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers = new();

    // Clears stale subscriptions between Editor play sessions (no-op in builds).
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearHandlers() => _handlers.Clear();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_handlers.ContainsKey(type))
            _handlers[type] = Delegate.Combine(_handlers[type], handler);
        else
            _handlers[type] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type)) return;

        _handlers[type] = Delegate.Remove(_handlers[type], handler);

        if (_handlers[type] == null)
            _handlers.Remove(type);
    }

    public static void Publish<T>(T eventData)
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var del))
            (del as Action<T>)?.Invoke(eventData);
    }
}
