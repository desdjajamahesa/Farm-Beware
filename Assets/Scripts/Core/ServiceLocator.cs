using System;
using System.Collections.Generic;

namespace FarmBeware.Core.Runtime
{
    /// <summary>
    /// Lightweight, type-safe service locator for decoupling subsystems without concrete dependencies.
    /// Thread-safe registry for single service providers conforming to core contracts.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private static readonly object _lock = new object();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            lock (_lock)
            {
                _services[typeof(T)] = service;
            }
        }

        public static T Resolve<T>() where T : class
        {
            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var service))
                {
                    return (T)service;
                }
                return null;
            }
        }

        public static bool TryResolve<T>(out T service) where T : class
        {
            service = Resolve<T>();
            return service != null;
        }

        public static void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                _services.Remove(typeof(T));
            }
        }

        public static void Reset()
        {
            lock (_lock)
            {
                _services.Clear();
            }
        }
    }
}
