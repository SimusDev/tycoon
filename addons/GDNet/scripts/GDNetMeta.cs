using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

[GlobalClass]
public partial class GDNetMeta : Node
{
    private static readonly Dictionary<ulong, Dictionary<string, object>> _cache = new();
    private static readonly object _lock = new();

    public void SingletonReady()
    {
        GDNetGarbageCollector.Instance.TryCollect += OnTryCollectGarbage;
    }

    private void OnTryCollectGarbage()
    {
        Task.Run(_CleanUpMeta);
    }

    public static void Set(GodotObject obj, string key, object value)
    {
        if (obj == null) return;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            if (!_cache.TryGetValue(id, out var data))
            {
                data = new Dictionary<string, object>();
                _cache[id] = data;
            }
            data[key] = value;
        }
    }

    public static T Get<T>(GodotObject obj, string key, T defaultValue = default)
    {
        if (obj == null) return defaultValue;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            if (_cache.TryGetValue(id, out var data) && data.TryGetValue(key, out var value))
                return (T)value;
        }
        return defaultValue;
    }

    public static T GetOrAdd<T>(GodotObject obj, string key, Func<T> factory) where T : class
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            if (!_cache.TryGetValue(id, out var data))
            {
                data = new Dictionary<string, object>();
                _cache[id] = data;
            }

            if (data.TryGetValue(key, out var value))
                return (T)value;

            var newValue = factory();
            data[key] = newValue;
            return newValue;
        }
    }

    public static T GetOrAddValue<T>(GodotObject obj, string key, T defaultValue) where T : struct
    {
        if (obj == null) return defaultValue;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            if (!_cache.TryGetValue(id, out var data))
            {
                data = new Dictionary<string, object>();
                _cache[id] = data;
            }

            if (data.TryGetValue(key, out var value))
                return (T)value;

            data[key] = defaultValue;
            return defaultValue;
        }
    }

    public static bool Has(GodotObject obj, string key)
    {
        if (obj == null) return false;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            return _cache.TryGetValue(id, out var data) && data.ContainsKey(key);
        }
    }

    public static void Remove(GodotObject obj, string key)
    {
        if (obj == null) return;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            if (_cache.TryGetValue(id, out var data))
                data.Remove(key);
        }
    }

    public static void RemoveAll(GodotObject obj)
    {
        if (obj == null) return;
        ulong id = obj.GetInstanceId();

        lock (_lock)
        {
            _cache.Remove(id);
        }
    }

    private static void _CleanUpMeta()
    {
        var deadKeys = new List<ulong>();

        lock (_lock)
        {
            foreach (var kvp in _cache)
            {
                if (!GodotObject.IsInstanceIdValid(kvp.Key))
                    deadKeys.Add(kvp.Key);
            }
        }

        if (deadKeys.Count > 0)
        {
            lock (_lock)
            {
                foreach (var key in deadKeys)
                    _cache.Remove(key);
            }

            if (OS.IsDebugBuild())
                GD.PushWarning($"[GDNetMeta] Cleaned {deadKeys.Count} dead objects");
        }
    }
}