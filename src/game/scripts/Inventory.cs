using Godot;
using System;

[GlobalClass]
public partial class Inventory : Node
{
    private GDNetStream _stream = new();

    public override void _Ready()
    {
        if (!Multiplayer.IsServer())
            RpcId(GameServer.ServerId, MethodName.SyncToSender);
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void SyncToSender()
    {
        RpcId(Multiplayer.GetRemoteSenderId(), MethodName.ReceiveFromSyncer);
    }

    [Rpc(mode: MultiplayerApi.RpcMode.Authority, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void ReceiveFromSyncer()
    {

    }

}
