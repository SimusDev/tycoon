using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    #region Sync
    [Signal] public delegate void SynchronizedEventHandler();
    private GDNetBuffer _buffer = new();

    private bool _isSynchronized = false;
    public bool IsSynchronized() => _isSynchronized;

    private void RequestSync() => RpcId(GetMultiplayerAuthority(), MethodName.SyncToSender);

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void SyncToSender()
	{
        _buffer.Clear();
        _buffer.WriteInt16((short)_slots.Count);
        foreach (InventorySlot slot in _slots)
        {
            _buffer.WriteBytesDynamic(slot.Serialize());
        }
        _buffer.WriteInt16(_selectedSlotIdx);

		RpcId(Multiplayer.GetRemoteSenderId(), MethodName.ReceiveFromSyncer, _buffer.GetBytes());
	}

	[Rpc(mode: MultiplayerApi.RpcMode.Authority, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void ReceiveFromSyncer(byte[] bytes)
	{
        _buffer.SetBytes(bytes);
        _buffer.Seek(0);

        short slotCount = _buffer.ReadInt16();

        _slots.Resize(slotCount);
        for (short i = 0; i < slotCount; i++) 
        {
            _slots[i] = InventorySlot.Deserialize(_buffer.ReadBytesDynamic());
        }


        _selectedSlotIdx = _buffer.ReadInt16();

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
    [Signal] public delegate void SlotAddedEventHandler(InventorySlot slot);
    [Signal] public delegate void SlotRemovedEventHandler(InventorySlot slot);
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
                _slots[i] = resourceSlot.Duplicate(true) as InventorySlot;
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
        EmitSignal(SignalName.SlotAdded, slot);
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
        EmitSignal(SignalName.SlotRemoved, slot);
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
        if (IsMultiplayerAuthority())
        {
            SelectSlot(idx);
            return;
        }

        RpcId(GetMultiplayerAuthority(), MethodName.SelectSlot, idx);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
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
        foreach (InventorySlot slot in Slots)
        {
            if (!slot.IsEmpty() && slot.CanStackWith(itemStack))
            {
                StackItem(slot, itemStack);

                if (itemStack.Quantity <= 0) return;
            }
        }
        
        if (TryGetFreeSlot(out InventorySlot freeSlot))
            freeSlot.ItemStack = itemStack;
    }

    public void AddItem(ItemData itemData)
    {
        AddItem(ItemStack.CreateFrom(itemData));
    }

    public void RemoveItem(InventorySlot slot) => slot.ItemStack = null;
    public void RemoveItem(short slotIdx)
    {
        if (TryGetSlot(slotIdx, out InventorySlot slot)) RemoveItem(slot);
    }
    #endregion

    #region Move/Swap/Stack Item
    public void MoveItem(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot.IsEmpty()) return;

        if (toSlot.IsEmpty())
        {
            toSlot.ItemStack = fromSlot.ItemStack;
            fromSlot.ItemStack = null;
        }
        else if (toSlot.CanStackWith(fromSlot.ItemStack))
        {
            StackItem(toSlot, fromSlot.ItemStack);
        }
        else SwapItem(fromSlot, toSlot);
    }

    public void SwapItem(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot.IsEmpty()) return;
        
        ItemStack t = fromSlot.ItemStack;
        fromSlot.ItemStack = toSlot.ItemStack;
        toSlot.ItemStack = t;
    }

    public void StackItem(InventorySlot slot, ItemStack itemStack)
    {
        if (slot == null || itemStack == null) return;
        
        if (slot.IsEmpty())
        {
            slot.ItemStack = itemStack;
            return;
        }

        if (slot.CanStackWith(itemStack))
        {
            ushort maxAmount = (ushort)slot.ItemStack.StackSize;
            ushort currentAmount = slot.ItemStack.Quantity;
            int spaceLeft = maxAmount - currentAmount;

            if (spaceLeft > 0)
            {
                ushort transferAmount = (ushort)Math.Min(spaceLeft, itemStack.Quantity);
                slot.ItemStack.Quantity += transferAmount;
                itemStack.Quantity -= transferAmount;
            }

        }
    }

    #endregion

    Inventory()
    {
        Interactable.GetOrCreate(this).AddInteraction(ResourceLoader.Load<Interaction>("uid://bf6f2mxftmr1l"));
    }

    public override void _Ready()
    {
        _inventoryOwnerId = GetParent().GetMultiplayerAuthority();
        SetProcessInput(IsInventoryOwner());
        SetMultiplayerAuthority(GameServer.ServerId);
        
        if (IsMultiplayerAuthority())
        {
            InitSlots();
            _isSynchronized = true;
        }
        else RequestSync();
    }

    public override void _Input(InputEvent @event)
    {
        for (short i = 0; i < 9; i++)
        {
            
            if (Input.IsActionJustPressed($"inventory.selectslot_{i}"))
            {
                RequestSelectSlot(i);
                break;
            }

        }
    }

}

