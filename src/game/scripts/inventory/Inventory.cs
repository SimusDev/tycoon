using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    private GDNetStream _stream = new();
    //[Export] public GodotObject InventoryOwner = null;
    [Export] private Array<InventorySlot> _slots = [];
    public Array<InventorySlot> Slots => _slots;

    private short _selectedSlotIdx = 0;
    public short SelectedSlotIdx => _selectedSlotIdx;
    private bool _isSynchronized = false;
    public bool IsSynchronized() => _isSynchronized;



    public InventorySlot GetSlot(short idx)
    {
        if (_slots.Count < idx || idx < 0) return null; 
        return _slots[idx];
    }

    public InventorySlot GetSelectedSlot()
    {
        return GetSlot(_selectedSlotIdx);
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

    #region Slot Selecting/Deselecting
    public void RequestSelectSlot(short idx)
    {
        RpcId(GetMultiplayerAuthority(), MethodName.SelectSlot, idx);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void SelectSlot(short idx)
    {
        if (_slots.Count < idx) return;
        if (idx == _selectedSlotIdx) idx = -1;

        ReceiveSelectedSlotIdx(idx);
        Rpc(MethodName.ReceiveSelectedSlotIdx, idx);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void ReceiveSelectedSlotIdx(short idx)
    {
        _selectedSlotIdx = idx;
        EmitSignal(SignalName.SlotSelected, idx);
    }

    #endregion

    [Signal] public delegate void SynchronizedEventHandler();
    [Signal] public delegate void SlotSelectedEventHandler(short idx);
    //[Signal] public delegate void SlotDeselectedEventHandler(short idx);

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
        data["selected_slot_idx"] = _selectedSlotIdx;

		RpcId(Multiplayer.GetRemoteSenderId(), MethodName.ReceiveFromSyncer, GD.VarToBytes(data));
	}

	[Rpc(mode: MultiplayerApi.RpcMode.Authority, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void ReceiveFromSyncer(byte[] bytes)
	{
        Dictionary data = GD.BytesToVar(bytes).AsGodotDictionary();
        if (data == null) return;

        _slots = data["slots"].AsGodotArray<InventorySlot>();
        _selectedSlotIdx = data["selected_slot_dx"].AsInt16();

        _isSynchronized = true;
        EmitSignal(SignalName.Synchronized);
	}
}

