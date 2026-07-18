using System.Net.Sockets;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    private GDNetStream _stream = new();
    [Export] private Array<InventorySlot> _slots = [];
    public Array<InventorySlot> Slots => _slots;

    private InventorySlot _selectedSlot = null;
    private bool _isSynchronized = false;
    public bool IsSynchronized() => _isSynchronized;

    public InventorySlot GetSlot(int idx)
    {
        if (_slots.Count < idx) return null; 
        return _slots[idx];
    }

    // public InventorySlot GetSlot(string[] tags)
    // {
        
    // }

    #region Server: AddSlot/ReceiveSlot
    public void AddSlot(InventorySlot slot)
    {
        if (!IsMultiplayerAuthority()) return;
        ReceiveSlot(slot);
        Rpc(MethodName.ReceiveSlot, slot.Serialize());
    }
    
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void ReceiveSlot(byte[] bytes)
    {
        ReceiveSlot(InventorySlot.Deserialize(bytes));
    }

    private void ReceiveSlot(InventorySlot slot)
    {
        if (_slots.Contains(slot)) return;
        _slots.Add(slot);
    }
    #endregion


    #region Server: RemoveSlot
    public void RemoveSlot(InventorySlot slot)
    {
        if (!IsMultiplayerAuthority()) return;

        RemoveSlotRpc(slot);
        Rpc(MethodName.RemoveSlotRpc, slot.Serialize());
    }

    public void RemoveSlot(int idx)
    {
        if (!IsMultiplayerAuthority()) return;

        RemoveSlotRpc(idx);
        Rpc(MethodName.RemoveSlotRpc, idx);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void RemoveSlotRpc(byte[] bytes)
    {
        RemoveSlotRpc(InventorySlot.Deserialize(bytes));
    }

    private void RemoveSlotRpc(InventorySlot slot)
    {
        if (!_slots.Contains(slot)) return;
        _slots.Remove(slot);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void RemoveSlotRpc(int idx)
    {
        if (_slots.Count < idx) return;
        _slots.RemoveAt(idx);
    }
    #endregion

    [Signal] public delegate void SlotsSynchronizedEventHandler(int idx);
    [Signal] public delegate void SlotSelectedEventHandler(int idx);
    [Signal] public delegate void SlotDeselectedEventHandler(int idx);

    public override void _Ready()
    {
        SetMultiplayerAuthority(GameServer.ServerId);

        if (IsMultiplayerAuthority())
        {
            _isSynchronized = true;
        }
        else
        {
            RequestSync();
        }   
    }

    private void RequestSync()
    {
        RpcId(GetMultiplayerAuthority(), MethodName.SyncToSender);
    }

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void SyncToSender()
	{
        Dictionary data = [];
        data["slots"] = _slots;

		RpcId(Multiplayer.GetRemoteSenderId(), MethodName.ReceiveFromSyncer, GD.VarToBytes(data));
	}

	[Rpc(mode: MultiplayerApi.RpcMode.Authority, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void ReceiveFromSyncer(byte[] bytes)
	{
        Dictionary data = GD.BytesToVar(bytes).AsGodotDictionary();
        if (data == null) return;

        _slots = data["slots"].AsGodotArray<InventorySlot>();

        _isSynchronized = true;
        EmitSignal(SignalName.SlotsSynchronized);
	}
}

