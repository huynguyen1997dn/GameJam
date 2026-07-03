using System;
using System.Collections.Generic;

public static class EventDispatcher
{
    private static readonly Dictionary<string, Delegate> eventMap = new();

    // =====================
    // Subscribe
    // =====================

    public static void Subscribe(string eventName, Action listener)
    {
        AddListener(eventName, listener);
    }

    public static void Subscribe<T>(string eventName, Action<T> listener)
    {
        AddListener(eventName, listener);
    }

    // =====================
    // Unsubscribe
    // =====================

    public static void Unsubscribe(string eventName, Action listener)
    {
        RemoveListener(eventName, listener);
    }

    public static void Unsubscribe<T>(string eventName, Action<T> listener)
    {
        RemoveListener(eventName, listener);
    }


    // =====================
    // Dispatch
    // =====================

    public static void Dispatch(string eventName)
    {
        if (eventMap.TryGetValue(eventName, out var del))
            (del as Action)?.Invoke();
    }

    public static void Dispatch<T>(string eventName, T arg)
    {
        if (eventMap.TryGetValue(eventName, out var del))
            (del as Action<T>)?.Invoke(arg);
    }
    // =====================
    // Internal helpers (AUTO CACHE)
    // =====================

    private static void AddListener(string eventName, Delegate listener)
    {
        if (eventMap.TryGetValue(eventName, out var existing))
        {
            if (existing.GetType() != listener.GetType())
                throw new Exception(
                    $"Event '{eventName}' signature mismatch. " +
                    $"Expected {existing.GetType()}, got {listener.GetType()}"
                );

            eventMap[eventName] = Delegate.Combine(existing, listener);
        }
        else
        {
            eventMap[eventName] = listener;
        }
    }

    private static void RemoveListener(string eventName, Delegate listener)
    {
        if (!eventMap.TryGetValue(eventName, out var existing))
            return;

        var current = Delegate.Remove(existing, listener);

        if (current == null)
            eventMap.Remove(eventName);
        else
            eventMap[eventName] = current;
    }
}
