using Godot;
using System;
using System.Collections.Generic;
using NetNodeSyncronizerResource;

[GlobalClass]
public partial class NetNodeSynchronizer : Node
{
    [Export] public Godot.Collections.Array<NetSyncData> SyncList = [];
    [Export] public float ReplicationTickrate = 32.0f;
    private const int RPC_CHANNEL = 7;
    
    private Timer _replicationTimer;
    private Dictionary<string, object> _cachedValues = [];

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority()) return;

        _replicationTimer = new();
        _replicationTimer.WaitTime = 1.0f / ReplicationTickrate;
        _replicationTimer.Timeout += OnReplicationTick;

        AddChild(_replicationTimer);
        _replicationTimer.Start();
        
        InitializeCache();
    }

    private void InitializeCache()
    {
        foreach (var syncData in SyncList)
        {
            var node = GetNode<Node>(syncData.TargetNode);
            if (node == null) continue;
            
            foreach (var property in syncData.Properties)
            {
                string key = $"{syncData.TargetNode}:{property.Name}";
                var value = node.Get(property.Name);
                _cachedValues[key] = value;
            }
        }
    }

    private void OnReplicationTick()
    {
        Send();
    }

    private void Send()
    {
        if (SyncList.Count == 0) return;

        foreach (NetSyncData syncData in SyncList)
        {
            var node = GetNode<Node>(syncData.TargetNode);
            if (node == null || !node.IsInsideTree()) continue;

            foreach (NetSyncProperty property in syncData.Properties)
            {
                try
                {
                    string key = $"{syncData.TargetNode}:{property.Name}";
                    var currentValue = node.Get(property.Name);
                    
                    if (_cachedValues.TryGetValue(key, out var cachedValue))
                    {
                        if (Equals(cachedValue, currentValue)) continue;
                    }
                    
                    _cachedValues[key] = currentValue;
                    
                    switch (property.TransferMode)
                    {
                        case MultiplayerPeer.TransferModeEnum.Unreliable:
                            Rpc(MethodName.UpdateNodePropertyUnreliable, syncData.TargetNode, property.Name, currentValue);
                            break;
                        case MultiplayerPeer.TransferModeEnum.UnreliableOrdered:
                            Rpc(MethodName.UpdateNodePropertyUnreliableOrdered, syncData.TargetNode, property.Name, currentValue);
                            break;
                        case MultiplayerPeer.TransferModeEnum.Reliable:
                            Rpc(MethodName.UpdateNodePropertyReliable, syncData.TargetNode, property.Name, currentValue);
                            break;
                    }  
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error syncing property {property.Name}: {ex.Message}");
                }
            }
        }
    }

    private void UpdateNodeProperty(NodePath targetNode, string propertyName, Variant value)
    {
        var node = GetNode<Node>(targetNode);
        if (node == null) return;
        
        try
        {
            node.Set(propertyName, value);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error setting property {propertyName} on client: {ex.Message}");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = RPC_CHANNEL)]
    private void UpdateNodePropertyReliable(NodePath targetNode, string propertyName, Variant value)
    {
        UpdateNodeProperty(targetNode, propertyName, value);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = RPC_CHANNEL, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void UpdateNodePropertyUnreliable(NodePath targetNode, string propertyName, Variant value)
    {
        UpdateNodeProperty(targetNode, propertyName, value);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = RPC_CHANNEL, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void UpdateNodePropertyUnreliableOrdered(NodePath targetNode, string propertyName, Variant value)
    {
        UpdateNodeProperty(targetNode, propertyName, value);
    }

    public void ForceSync()
    {
        _cachedValues.Clear();
        InitializeCache();
        Send();
    }
}