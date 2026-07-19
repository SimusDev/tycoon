using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    #region Sync
    [Signal] public delegate void SynchronizedEventHandler();
    private bool _isSynchronized = false;
    public bool IsSynchronized() => _isSynchronized;

    private void RequestSync() => RpcId(GetMultiplayerAuthority(), MethodName.SyncToSender);

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

        _isSlotsInitialized = true;
        EmitSignal(SignalName.SlotsInitialized);
        _isSynchronized = true;
        EmitSignal(SignalName.Synchronized);
	}
    #endregion

    #region Inventory Owner
    private long _inventoryOwnerId = -1;
    public long GetOwnerId() => _inventoryOwnerId;
    public bool IsInventoryOwner() => Multiplayer.GetUniqueId() == _inventoryOwnerId;
    #endregion


    #region Slots
    [Signal] public delegate void SlotSelectedEventHandler(short idx);
    [Signal] private delegate void SlotsInitializedEventHandler();
    
    [Export] private Array<InventorySlot> _slots = [];
    public Array<InventorySlot> Slots => _slots;
    private bool _isSlotsInitialized = false;

    private void InitSlots()
    {
        if (_isSlotsInitialized) return;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i] is Resource resourceSlot)
            {
                _slots[i] = resourceSlot.Duplicate() as InventorySlot;
            }
        }
        _isSlotsInitialized = true;
        EmitSignal(SignalName.SlotsInitialized);
    }

    public InventorySlot GetSlot(short idx)
    {
        if (idx >= 0 && idx < _slots.Count) return _slots[idx];
        return null;
    }

    public bool TryGetSlot(short idx, out InventorySlot slot)
    {
        slot = GetSlot(idx);
        return slot != null;
    }

    public InventorySlot GetFreeSlot()
    {
        foreach (InventorySlot slot in _slots)
        {
            if (slot.IsEmpty())
            {
                return slot;
            }
        }
        
        return null;
    }

    public bool TryGetFreeSlot(out InventorySlot slot)
    {
        slot = GetFreeSlot();
        return slot != null;
    }


    private short _selectedSlotIdx = 0;
    public short SelectedSlotIdx => _selectedSlotIdx;
    public InventorySlot SelectedSlot => GetSlot(_selectedSlotIdx);
    

    #endregion

    #region Add/Receive Slot
    public void AddSlot(InventorySlot slot)
    {
        if (!IsMultiplayerAuthority()) return;
        ReceiveSlot(slot);
        Rpc(MethodName.ReceiveSlot, slot.Serialize());
    }

    public void AddSlot() => AddSlot(new());

    
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


    #region RemoveSlot
    public void RemoveSlot(InventorySlot slot)
    {
        if (!IsMultiplayerAuthority()) return;

        RemoveSlotRpc(slot);
        Rpc(MethodName.RemoveSlotRpc, slot.Serialize());
    }

    public void RemoveSlot() => RemoveSlot(GetSlot((short)_slots.Count)); //Remove front slot

    public void RemoveSlot(int idx)
    {
        if (!IsMultiplayerAuthority()) return;

        RemoveSlotRpc(idx);
        Rpc(MethodName.RemoveSlotRpc, idx);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void RemoveSlotRpc(InventorySlot slot)
    {
        if (!_slots.Contains(slot)) return;
        _slots.Remove(slot);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void RemoveSlotRpc(byte[] bytes) => RemoveSlotRpc(InventorySlot.Deserialize(bytes));

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
    private void RemoveSlotRpc(int idx)
    {
        if (_slots.Count < idx) return;
        _slots.RemoveAt(idx);
    }
    #endregion

    #region Select/Deselect Slot
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

    #region Add/Remove Item
    public void AddItem(ItemStack itemStack)
    {
        //if (!IsMultiplayerAuthority()) return;
        
        if (!TryGetFreeSlot(out InventorySlot free_slot)) return;
        
        free_slot.ItemStack = itemStack;
    }

    public void AddItem(ItemData itemData)
    {
        //if (!IsMultiplayerAuthority()) return;
        AddItem(ItemStack.CreateFrom(itemData));
    }

    public void RemoveItem(InventorySlot slot) => slot.ItemStack = null;
    #endregion

    public override void _Ready()
    {
        _inventoryOwnerId = GetParent().GetMultiplayerAuthority();
        SetMultiplayerAuthority(GameServer.ServerId);
        
        if (IsMultiplayerAuthority())
        {
            InitSlots();
            //_isSynchronized = true;
        }
        else RequestSync();
    }
}

