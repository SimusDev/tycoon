using Godot;
using System;

public partial class SynchronizationServer : Node
{
    public static SynchronizationServer Instance;

    private GDNetBuffer _buffer = new();

    public override void _Ready()
    {
        Instance = this;
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, TransferChannel = (int)GameServer.TransferChannels.SynchronizationServerMessages)]
    public void ReceiveMessageRemote(byte[] data)
    {
        int fromPeer = Multiplayer.GetRemoteSenderId();

        _buffer.SetBytes(data);
        _buffer.Seek(0);

        uint uniqueId = (uint)_buffer.ReadLong();
        ushort packetId = (ushort)_buffer.ReadLong();
        Variant args = _buffer.ReadVar();

        var communicator = NetCommunicator.TryGetByNetworkId(uniqueId);

        if (communicator == null)
        {
            GD.PushError($"Cant Get NetCommunicator with id {uniqueId}");
            return;
        }

        communicator._MessageReceivedInternal(fromPeer, packetId, args);
    }

    public void SendMessage(int peer, uint uniqueId, ushort packetId, Variant args)
    {
        _buffer.Clear();
        _buffer.WriteLong(uniqueId);
        _buffer.WriteLong(packetId);
        _buffer.WriteVar(args);
        RpcId(peer, MethodName.ReceiveMessageRemote, _buffer.GetBytes());
    }

}

