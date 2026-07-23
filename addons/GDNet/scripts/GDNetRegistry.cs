using Godot;
using System;
using System.Collections.Generic;
public class GDNetRegistry<T> where T : class
{
    private Dictionary<string, T> _byName = new();

    private Dictionary<ulong, T> _byId = new();

    private Dictionary<T, ulong> _objectToId = new();

    private ulong _nextId = 1;

    private bool _frozen = false;

    public T Register(string name, T obj)
    {
        if (_frozen)
            throw new InvalidOperationException($"Registry is frozen! can't add '{name}'");

        if (_byName.ContainsKey(name))
            throw new ArgumentException($"Object with name '{name}' already registered!");

        ulong id = _nextId++;

        _byName[name] = obj;
        _byId[id] = obj;
        _objectToId[obj] = id;

        return obj;
    }

    public T Get(string name)
    {
        return _byName.TryGetValue(name, out var obj) ? obj : null;
    }

    public T Get(ulong id)
    {
        return _byId.TryGetValue(id, out var obj) ? obj : null;
    }

    public ulong GetId(T obj)
    {
        return _objectToId.TryGetValue(obj, out var id) ? id : 0;
    }

    public string GetName(T obj)
    {
        foreach (var kvp in _byName)
        {
            if (kvp.Value.Equals(obj))
                return kvp.Key;
        }
        return null;
    }
    public bool Contains(string name)
    {
        return _byName.ContainsKey(name);
    }
    public bool Contains(ulong id)
    {
        return _byId.ContainsKey(id);
    }
    public void Freeze()
    {
        _frozen = true;
        GD.Print($"[REGISTRY] Is frozen! {_byName.Count} objects.");
    }

    public int Count => _byName.Count;

    public IEnumerable<(string name, ulong id, T obj)> GetAll()
    {
        foreach (var kvp in _byName)
        {
            var name = kvp.Key;
            var obj = kvp.Value;
            var id = _objectToId[obj];
            yield return (name, id, obj);
        }
    }
}