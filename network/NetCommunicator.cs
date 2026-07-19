using Godot;
using System;

public partial class NetCommunicator : RefCounted
{
    private static System.Collections.Generic.Dictionary<uint, WeakReference<NetCommunicator>> _registry = new();
        
    private static uint _nextUniqueId = 0;

    private uint _uniqueId = 0;
    public uint UniqueId => _uniqueId;

    [Signal] public delegate void MessageReceivedEventHandler(int fromPeer, ushort packetId, Variant args);

    public static NetCommunicator TryGetByNetworkId(uint id)
    {
        if (_registry.TryGetValue(id, out WeakReference<NetCommunicator> weak))
        {
            if (weak.TryGetTarget(out var target))
            {
                return target;
            }
        }

        return null;
    }

    public void Register(uint id)
    {
        _registry.Remove(UniqueId);
        _registry[id] = new WeakReference<NetCommunicator>(this);
        _uniqueId = id;
    }

    public static uint GenerateUniqueId()
    {
        _nextUniqueId++;
        return _nextUniqueId;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            _registry.Remove(UniqueId);
        }
    }

    public virtual void _MessageReceivedInternal(int fromPeer, ushort packetId, Variant args)
    {
        EmitSignal(SignalName.MessageReceived, fromPeer, packetId, args);
    }

    public void SendMessageTo(int peer, ushort packetId, Variant args)
    {
        SynchronizationServer.Instance.SendMessage(peer, UniqueId, packetId, args);
    }

}
