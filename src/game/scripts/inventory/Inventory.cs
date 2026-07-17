using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
	private GDNetStream _stream = new();
	private List<InventorySlot> _slots;

	private GDNetRpc _rpc = new();

	public override void _Ready()
	{
		_rpc.BindOwnerAsNode(this);

		if (!Multiplayer.IsServer())
		{
			RpcId(GameServer.ServerId, MethodName.SyncToSender);
			_rpc.InvokeOnServer(nameof(RpcTest));
		}
			
	}

	[GDNetRpc(permission:Permission.Any)]
	public void RpcTest()
	{
		GD.Print($"hello from {_rpc.GetRemoteSender()}");
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

