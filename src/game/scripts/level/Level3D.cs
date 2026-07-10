using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Level3D : Node3D
{
    public Array<long> ConnectedPeers = new();

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
        }

        else
        {
            Rpc("ServerReceivePeerEnter");
        }
    }

#nullable enable
    public static Node? FindAbove(Node? node)
    {
        if (!IsInstanceValid(node))
        {
            return null;
        }

        if (node is Level3D)
        {
            return node;
        }

        return FindAbove(node.GetParent());
    }
#nullable disable

    public override void _ExitTree()
    {
        if (!Multiplayer.IsServer())
        {
            Rpc("ServerReceivePeerExit");
        }
    }

    [Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, 
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)GameNetwork.Channel.Connection)]
    private void ServerReceivePeerEnter()
    {
        ConnectedPeers.Add(Multiplayer.GetRemoteSenderId());
    }

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = (int)GameNetwork.Channel.Connection)]
    private void ServerReceivePeerExit()
    {
        ConnectedPeers.Remove(Multiplayer.GetRemoteSenderId());
    }

    private void OnPeerDisconnected(long id)
    {
        ConnectedPeers.Remove(id);
    }



}
