using Godot;
using System;
using System.Collections.Generic;
using System.IO;

[GlobalClass]
public partial class NetCommunicator : Resource, IDisposable
{

    [Export] private long _networkID = 0;
    [Export] public MultiplayerPeer.TransferModeEnum Mode = MultiplayerPeer.TransferModeEnum.Reliable;
    [Export] public int Channel = 0;

    private static Dictionary<long, ulong> _registry = new();

    private MemoryStream _writerStream;
    private BinaryWriter _writer;

    [Signal] public delegate void OnReceivedBytesEventHandler(int peer, byte[] data);

    public static NetCommunicator FindByNetworkID(long id)
    {
        GodotObject obj = InstanceFromId(_registry.GetValueOrDefault(id));
        if (obj != null)
        {
            return (NetCommunicator)obj;
        }

        return null;
    }

    public NetCommunicator()
    {
        _writerStream = new MemoryStream();
        _writer = new BinaryWriter(_writerStream);
        SetNetworkID(_networkID);
    }

    public void SetNetworkID(long id)
    {
        _registry.Remove(_networkID);
        _networkID = id;
        _registry[id] = this.GetInstanceId();
    }

    public long GetNetworkID()
    {
        return _networkID;
    }

    public void Send(int peerId, byte[] data)
    {
        _writerStream.Position = 0;
        _writerStream.SetLength(0);
        _writer.Write(_networkID);
        _writer.Write(data);
        GameNetwork.Instance.SendPacket(GameNetwork.PacketType.CommunicatorMessage, peerId, _writerStream.ToArray(), Mode, Channel);
    }

    public void SendToServer(byte[] data)
    {
        Send(GameNetwork.ServerID, data);
    }

    public void SendByPeers(int[] peers, byte[] data)
    {
        foreach(var peer in peers)
        {
            Send(peer, data);
        }
    }

    public void ReceivedBytesInternal(int peerId, byte[] data)
    {
        EmitSignal(SignalName.OnReceivedBytes, peerId, data);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _registry.Remove(_networkID);
        }

        _writerStream?.Dispose();
        _writer?.Dispose();

        base.Dispose(disposing);
    }


}
