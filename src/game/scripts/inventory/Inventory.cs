using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    private GDNetStream _stream = new();
    private Array<InventorySlot> _slots;
    private bool _isSynchronized = false;

    public InventorySlot GetSlot(int idx)
    {
        if (_slots.Count < idx) return null; 
        return _slots[idx];
    }

    public void AddSlot(InventorySlot slot)
    {
        if (_slots.Contains(slot)) return;
        _slots.Add(slot);
    }

    public void RemoveSlot(InventorySlot slot)
    {
        if (!_slots.Contains(slot)) return;
        _slots.Remove(slot);
    }

    public void RemoveSlot(int idx)
    {
        if (_slots.Count < idx) return;
        _slots.RemoveAt(idx);
    }

    [Signal] public delegate void SlotsSynchronizedEventHandler(int idx);
    [Signal] public delegate void SlotSelectedEventHandler(int idx);
    [Signal] public delegate void SlotDeselectedEventHandler(int idx);

    public override void _Ready()
    {
        if (Multiplayer.IsServer())
        {
            _isSynchronized = true;
        }
        else
        {
            RpcId(GameServer.ServerId, MethodName.SyncToSender);
        }
        
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

