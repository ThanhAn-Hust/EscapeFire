using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeFire.Core
{
    /// <summary>
    /// Typed, allocation-free (on Publish) static event bus.
    /// Handler order is NOT guaranteed. A throwing handler is logged and does not stop the others.
    /// Subscribe in OnEnable, Unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        private static readonly List<Action<T>> _handlers = new List<Action<T>>(8);

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int HandlerCount => _handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || _handlers.Contains(handler)) return;
            _handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            _handlers.Remove(handler);
        }

        public static void Publish(T evt)
        {
            // Reverse iteration: a handler may unsubscribe itself (or others) safely without allocating a copy.
            for (int i = _handlers.Count - 1; i >= 0; i--)
            {
                if (i >= _handlers.Count) continue;
                try
                {
                    _handlers[i](evt);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        public static void Clear()
        {
            _handlers.Clear();
        }
    }

    /// <summary>
    /// Clears every EventBus that has been used, at the start of each play session
    /// (required when "Enter Play Mode Options" disables domain reload).
    /// </summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> _clearActions = new List<Action>();

        internal static void Register(Action clearAction)
        {
            _clearActions.Add(clearAction);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAll()
        {
            for (int i = 0; i < _clearActions.Count; i++)
            {
                _clearActions[i]();
            }
        }
    }
}
