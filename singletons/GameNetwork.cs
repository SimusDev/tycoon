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

    private GDNetOptimizedSend _optimizedSend;

    [Signal] public delegate void PacketReceivedEventHandler(PacketType type, long peer, byte[] bytes);

    public enum PacketType: byte
    {

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

    public override void _Ready()
    {
        _optimizedSend = new GDNetOptimizedSend();
        AddChild(_optimizedSend);
        _optimizedSend.MultiplayerPeerPacket += OptimizedPeerPacket;
    }

    private void OptimizedPeerPacket(long id, byte[] bytes)
    {

    }

    public void SendPacket(PacketType type, int peer, byte[] data, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        _optimizedSend.MultiplayerSendBytes(data, peer, mode, channel);
    }


}
