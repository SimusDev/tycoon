using Godot;
using NetNodeSyncronizerResource;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[GlobalClass]
public partial class NetNodeSynchronizer : Node
{
    [Export] public Godot.Collections.Array<NetSyncData> SyncList = [];
    [Export] public float ReplicationTickrate = 32.0f;

    private double _replicationTime = 0;
    private float _tickInterval;
    
    // ============ ОПТИМИЗАЦИЯ 1: КЭШ УЗЛОВ ============
    private readonly Dictionary<NodePath, Node> _nodeCache = new();

    // ============ ОПТИМИЗАЦИЯ 2: КЭШ СВОЙСТВ ============
    private readonly Dictionary<ulong, object> _cachedValues = new();
    private readonly Dictionary<ulong, PropertyInfo> _propertyInfos = new();

    // ============ ОПТИМИЗАЦИЯ 3: БАТЧИНГ ============
    private readonly List<SyncPacket> _syncBatch = new();
    private readonly GDNetStream _stream = new();
    private readonly GDNetStream _receiveStream = new();

    public override void _Ready()
    {
        _tickInterval = 1.0f / ReplicationTickrate;
        InitializeCache();
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        _replicationTime += delta;
        if (_replicationTime >= _tickInterval)
        {
            Send();
            _replicationTime = 0;
        }
    }

    // ============ ИНИЦИАЛИЗАЦИЯ КЭША ============
    private void InitializeCache()
    {
        foreach (var syncData in SyncList)
        {
            var node = GetNode<Node>(syncData.TargetNode);
            if (node == null) continue;

            _nodeCache[syncData.TargetNode] = node;

            foreach (var property in syncData.Properties)
            {
                var key = GenerateKey(syncData.TargetNode, property.Name);
                var value = node.Get(property.Name);
                _cachedValues[key] = value;
                _propertyInfos[key] = new PropertyInfo(node, property.Name, property.TransferMode);
            }
        }
    }

    // ============ ГЕНЕРАЦИЯ КЛЮЧА (БЕЗ АЛЛОКАЦИЙ) ============
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong GenerateKey(NodePath path, string name)
    {
        // Комбинируем хэши без аллокаций
        return (ulong)(path.GetHashCode() ^ name.GetHashCode());
    }

    // ============ ОТПРАВКА (БАТЧИНГ) ============
    private void Send()
    {
        if (SyncList.Count == 0) return;

        _syncBatch.Clear();

        foreach (var syncData in SyncList)
        {
            if (!_nodeCache.TryGetValue(syncData.TargetNode, out var node) || node == null)
                continue;

            foreach (var property in syncData.Properties)
            {
                var key = GenerateKey(syncData.TargetNode, property.Name);
                var currentValue = node.Get(property.Name);

                if (_cachedValues.TryGetValue(key, out var cachedValue))
                {
                    if (Equals(cachedValue, currentValue)) continue;
                }

                _cachedValues[key] = currentValue;

                _syncBatch.Add(new SyncPacket
                {
                    Path = syncData.TargetNode,
                    Property = property.Name,
                    Value = currentValue,
                    Mode = property.TransferMode
                });
            }
        }

        if (_syncBatch.Count == 0) return;

        // ============ ОТПРАВКА БАТЧОМ ============
        SendBatch(_syncBatch);
    }

    // ============ БАТЧНАЯ ОТПРАВКА ============
    private void SendBatch(List<SyncPacket> batch)
    {
        _stream.Clear();

        // Записываем количество
        _stream.WriteInt32(batch.Count);

        foreach (var packet in batch)
        {
            _stream.WriteString(packet.Path.ToString());
            _stream.WriteString(packet.Property);
            byte[] varBytes = GD.VarToBytes(packet.Value);
            _stream.WriteUInt16((ushort)varBytes.Length);
            _stream.WriteBytes(varBytes);
            _stream.WriteByte((byte)packet.Mode);
        }

        // Отправляем ОДИН RPC вместо N
        Rpc(MethodName.UpdateNodePropertiesBatch, _stream.GetBytes());
    }

    // ============ ПРИЁМ БАТЧА (ОДИН RPC) ============
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = 7, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void UpdateNodePropertiesBatch(byte[] data)
    {
        _receiveStream.SetBytes(data);
        _receiveStream.Seek(0);

        int count = _receiveStream.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            var path = _receiveStream.ReadString();
            var property = _receiveStream.ReadString();
            byte[] valueBytes = _receiveStream.ReadBytes(_receiveStream.ReadUInt16());
            var mode = (MultiplayerPeer.TransferModeEnum)_receiveStream.ReadByte();

            UpdateNodeProperty(path, property, GD.BytesToVar(valueBytes), mode);
        }
    }

    // ============ ОБНОВЛЕНИЕ ОДНОГО СВОЙСТВА ============
    private void UpdateNodeProperty(string path, string property, Variant value, MultiplayerPeer.TransferModeEnum mode)
    {
        var nodePath = new NodePath(path);
        if (!_nodeCache.TryGetValue(nodePath, out var node))
        {
            node = GetNode<Node>(nodePath);
            if (node == null) return;
            _nodeCache[nodePath] = node;
        }

        try
        {
            node.Set(property, value);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error setting {property}: {ex.Message}");
        }
    }

    // ============ ПАКЕТ ============
    private struct SyncPacket
    {
        public NodePath Path;
        public string Property;
        public Variant Value;
        public MultiplayerPeer.TransferModeEnum Mode;
    }

    // ============ ВСПОМОГАТЕЛЬНЫЙ КЛАСС ============
    private class PropertyInfo
    {
        public Node Node;
        public string Name;
        public MultiplayerPeer.TransferModeEnum TransferMode;

        public PropertyInfo(Node node, string name, MultiplayerPeer.TransferModeEnum mode)
        {
            Node = node;
            Name = name;
            TransferMode = mode;
        }
    }

    public void ForceSync()
    {
        _cachedValues.Clear();
        InitializeCache();
        Send();
    }
}