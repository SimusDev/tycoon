using Godot;
using System;
using System.IO;


public partial class GameNetwork : Node
{
    public static GameNetwork Instance;

    private MemoryStream _writerStream;
    private BinaryWriter _writer;

    private MemoryStream _readerStream;
    private BinaryReader _reader;

    private MultiplayerPeer.ConnectionStatus _connectionStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
    public const int ServerID = 1;
    public bool IsConnectedToServer = false;
    public bool IsServer = true;
    public int UniqueID = ServerID;

    private GDNetOptimizedSend _optimizedSend;

    private long _serverNextNetworkID = 0;

    [Signal] public delegate void OnNetworkPeerConnectionStatusChangedEventHandler(MultiplayerPeer.ConnectionStatus status);
    [Signal] public delegate void OnNetworkReadyEventHandler();
    [Signal] public delegate void OnNetworkConnectingEventHandler();
    [Signal] public delegate void OnNetworkDisconnectedEventHandler();

    public enum PacketType: byte
    {
        CommunicatorMessage,
    }

    public long ServerGenerateNetworkID()
    {
        _serverNextNetworkID++;
        return _serverNextNetworkID;
    }

    public long SynchronizeNetworkID(Node node)
    {
        string pathStr = node.GetPath().ToString();
        return $"{"NetNodeHashPath"}{pathStr}".Hash();
    }

    public long SynchronizeNetworkID(Resource diskResource)
    {
        string pathStr = diskResource.ResourcePath;
        return $"{"NetResourceHashPath"}{pathStr}".Hash();
    }

    public GameNetwork()
    {
        _writerStream = new MemoryStream();
        _writer = new BinaryWriter(_writerStream);

        _readerStream = new MemoryStream();
        _reader = new BinaryReader(_readerStream);
    }

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateNetworkStateTick();
    }

    public override void _Ready()
    {
        _optimizedSend = new GDNetOptimizedSend();
        AddChild(_optimizedSend);
        _optimizedSend.MultiplayerPeerPacket += OptimizedPeerPacket;
    }

    private void UpdateNetworkStateTick()
    {
        MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
        if (peer == null)
            return;

        if (peer is OfflineMultiplayerPeer)
        {
            return;
        }

        if (peer.GetConnectionStatus() != _connectionStatus)
        {
            _connectionStatus = peer.GetConnectionStatus();
            ConnectionStatusChanged();
            EmitSignal(SignalName.OnNetworkPeerConnectionStatusChanged, ((int)_connectionStatus));
        }
    }

    private void ConnectionStatusChanged()
    {
        IsServer = Multiplayer.IsServer();
        IsConnectedToServer = _connectionStatus == MultiplayerPeer.ConnectionStatus.Connected;
        UniqueID = Multiplayer.GetUniqueId();

        switch (_connectionStatus)
        {
            case MultiplayerPeer.ConnectionStatus.Disconnected:
                EmitSignal(SignalName.OnNetworkDisconnected);
                break;
            case MultiplayerPeer.ConnectionStatus.Connecting:
                EmitSignal(SignalName.OnNetworkConnecting);
                break;
            case MultiplayerPeer.ConnectionStatus.Connected:
                EmitSignal(SignalName.OnNetworkReady);
                break;
        }
    }

    private void OptimizedPeerPacket(long id, byte[] bytes)
    {
        _readerStream.Position = 0;
        _readerStream.SetLength(0);
        _readerStream.Write(bytes, 0, bytes.Length);

        var packetType = (PacketType)_reader.ReadByte();
        byte[] receivedBytes = _reader.ReadBytes((int)_readerStream.Length - 1);

        switch (packetType)
        {
            case PacketType.CommunicatorMessage:
                long networkId = _reader.ReadInt64();
                NetCommunicator.FindByNetworkID(networkId)?.ReceivedBytesInternal((int)id, _reader.ReadBytes((int)(_readerStream.Length - _readerStream.Position)));
                break;
        }
    }

    public void SendPacket(PacketType type, int peer, byte[] data, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        _writerStream.Position = 0;
        _writerStream.SetLength(0);
        _writer.Write((byte)type);
        _writer.Write(data);
        _optimizedSend.MultiplayerSendBytes(_writerStream.ToArray(), peer, mode, channel);
    }


}
