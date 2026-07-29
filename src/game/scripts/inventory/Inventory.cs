using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
	[Export] private Node _root;
	[Export] private bool _clientSidePrediction = false;

	#region Sync
	[Signal] public delegate void SynchronizedEventHandler();
	private GDNetBuffer _buffer = new();

	private bool _isSynchronized = false;
	public bool IsSynchronized() => _isSynchronized;

	private void RequestSync()
	{
		RpcId(GetMultiplayerAuthority(), MethodName.SyncToSender);
	}


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

		_slots.Clear();
		short slotCount = _buffer.ReadInt16();

		for (short i = 0; i < slotCount; i++) 
		{
			var deserialized = InventorySlot.Deserialize(_buffer.ReadBytesDynamic());
			ReceiveSlot(deserialized);
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
	[Signal] public delegate void SlotAddedEventHandler(short idx);
	[Signal] public delegate void SlotRemovedEventHandler(short idx);
	[Signal] public delegate void SlotSelectedEventHandler(short idx);
	[Signal] public delegate void SlotsInitializedEventHandler();
	
	[Export] private Array<InventorySlot> _slots = [];
	public Array<InventorySlot> Slots => _slots;
	private bool _isSlotsInitialized = false;
	public bool IsSlotsInitialized => _isSlotsInitialized;

	private void InitSlots() // Server Only
	{
		if (_isSlotsInitialized) return;

		Array<InventorySlot> temp = [];
		foreach (InventorySlot inventorySlot in _slots)
		{
			temp.Add(inventorySlot.Duplicate(true) as InventorySlot);
		}
		_slots = temp;

		_isSlotsInitialized = true;
		EmitSignal(SignalName.SlotsInitialized);
	}

	// В #region Slots или в отдельный регион
	public short IndexOfSlot(InventorySlot slot)
	{
		for (short i = 0; i < _slots.Count; i++)
			if (_slots[i] == slot) return i;
		return -1;
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void SyncSlot(short index, byte[] slotData)
	{
		if (!TryGetSlot(index, out var slot)) return;
		var newSlot = InventorySlot.Deserialize(slotData);
		slot.ItemStack = newSlot.ItemStack; // триггерит сигнал ItemStackChanged
	}

	private void BroadcastSlotUpdate(short index)
	{
		if (!IsMultiplayerAuthority() || !_isSynchronized) return;
		var slot = GetSlot(index);
		if (slot == null) return;
		Rpc(MethodName.SyncSlot, index, slot.Serialize());
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
		EmitSignal(SignalName.SlotAdded, _slots.Count-1);
	}
	#endregion


	#region RemoveSlot
	public void RemoveSlot() => RemoveSlot(_slots.Count); //Remove front slot

	public void RemoveSlot(int idx)
	{
		if (!IsMultiplayerAuthority()) return;

		RemoveSlotRpc(idx);
		Rpc(MethodName.RemoveSlotRpc, idx);
		EmitSignal(SignalName.SlotRemoved, idx);
	}


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
		if (!IsMultiplayerAuthority()) return;
		foreach (InventorySlot slot in Slots)
		{
			if (!slot.IsEmpty() && slot.CanStackWith(itemStack))
			{
				StackItem(slot, itemStack);
				BroadcastSlotUpdate(IndexOfSlot(slot)); // <-- добавить
				if (itemStack.Quantity <= 0) return;
			}
		}
		if (TryGetFreeSlot(out InventorySlot freeSlot))
		{
			freeSlot.ItemStack = itemStack;
			BroadcastSlotUpdate(IndexOfSlot(freeSlot)); // <-- добавить
		}
	}

	public void AddItem(ItemData itemData)
	{
		if (!IsMultiplayerAuthority()) return;
		AddItem(ItemStack.CreateFrom(itemData));
	}

	// Remove

	public void RequestRemoveItem(short slotIdx)
	{
		if (IsMultiplayerAuthority())
		{
			RemoveItem(slotIdx);
			return;
		}

		if (_clientSidePrediction) RemoveItem(slotIdx);
		RpcId(GetMultiplayerAuthority(), MethodName.RemoveItem, slotIdx);
	}

	private void RemoveItem(InventorySlot slot)
	{
		slot.ItemStack = null;
		BroadcastSlotUpdate(IndexOfSlot(slot)); // <-- добавить
	}

	private void RemoveItem(short slotIdx)
	{
		if (TryGetSlot(slotIdx, out InventorySlot slot)) RemoveItem(slot);
	}
	#endregion

	#region Move/Swap/Stack Item

	/// <summary> Move item in <b>this</b> Inventory </summary>
	public void RequestMoveItem(short fromSlotIdx, short toSlotIdx) 
	{
		if (IsMultiplayerAuthority())
		{
			MoveItem(fromSlotIdx, toSlotIdx);
			return;
		}
		
		if (_clientSidePrediction) MoveItem(fromSlotIdx, toSlotIdx);
		RpcId(GetMultiplayerAuthority(), MethodName.MoveItem, fromSlotIdx, toSlotIdx);
	}

	/// <summary> Move item in <b>fromInventory</b> Inventory </summary>
	public void RequestMoveItem(short fromSlotIdx, short toSlotIdx, NodePath fromInventory)
	{
		if (IsMultiplayerAuthority())
		{
			MoveItem(fromSlotIdx, toSlotIdx, fromInventory);
			return;
		}

		if (_clientSidePrediction) MoveItem(fromSlotIdx, toSlotIdx, fromInventory);
		RpcId(GetMultiplayerAuthority(), MethodName.MoveItem, fromSlotIdx, toSlotIdx, fromInventory);
	} 

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void MoveItem(short fromSlotIdx, short toSlotIdx)
	{
		if (TryGetSlot(fromSlotIdx, out InventorySlot fromSlot) && TryGetSlot(toSlotIdx, out InventorySlot toSlot))
		{
			MoveItem(fromSlot, toSlot);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void MoveItem(short fromSlotIdx, short toSlotIdx, NodePath fromInventory)
	{
		Inventory fromInventoryNode = GetTree().Root.GetNodeOrNull<Inventory>(fromInventory);
		if (fromInventoryNode == null) return;

		if (fromInventoryNode.TryGetSlot(fromSlotIdx, out InventorySlot fromSlot) && TryGetSlot(toSlotIdx, out InventorySlot toSlot))
		{
			MoveItem(fromSlot, toSlot);
		}
	}

	private void MoveItem(InventorySlot fromSlot, InventorySlot toSlot)
	{
		if (fromSlot.IsEmpty())
		{
			GD.Print("EEMMMPTYY");
			return;
		}

		if (toSlot.IsEmpty())
		{
			toSlot.ItemStack = fromSlot.ItemStack;
			fromSlot.ItemStack = null;
			GD.Print("17");
			BroadcastSlotUpdate(IndexOfSlot(fromSlot));
			BroadcastSlotUpdate(IndexOfSlot(toSlot));
		}
		else if (toSlot.CanStackWith(fromSlot.ItemStack))
		{
			StackItem(toSlot, fromSlot.ItemStack);
			GD.Print("27");
			BroadcastSlotUpdate(IndexOfSlot(fromSlot));
			BroadcastSlotUpdate(IndexOfSlot(toSlot));
		}
		else
		{
			SwapItem(fromSlot, toSlot);
			GD.Print("37");
			BroadcastSlotUpdate(IndexOfSlot(fromSlot));
			BroadcastSlotUpdate(IndexOfSlot(toSlot));
		}
	}

	public void RequestSwapItem(short fromIdx, short toIdx)
	{
		if (IsMultiplayerAuthority())
		{
			SwapItem(fromIdx, toIdx);
			return;
		}
		
		if (_clientSidePrediction) SwapItem(fromIdx, toIdx);
		RpcId(GetMultiplayerAuthority(), MethodName.SwapItem, fromIdx, toIdx);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferChannel = (int)GameServer.TransferChannels.Inventory)]
	private void SwapItem(short fromIdx, short toIdx)
	{
		if (TryGetSlot(fromIdx, out InventorySlot fromSlot) && TryGetSlot(toIdx, out InventorySlot toSlot))
		{
			SwapItem(fromSlot, toSlot);
		}
	}

	private void SwapItem(InventorySlot fromSlot, InventorySlot toSlot)
	{
		if (fromSlot.IsEmpty()) return;
		(fromSlot.ItemStack, toSlot.ItemStack) = (toSlot.ItemStack, fromSlot.ItemStack);
		BroadcastSlotUpdate(IndexOfSlot(fromSlot));
		BroadcastSlotUpdate(IndexOfSlot(toSlot));
	}


	private void StackItem(InventorySlot toSlot, ItemStack itemStack)
	{
		if (toSlot == null || itemStack == null) return;

		if (toSlot.IsEmpty())
		{
			toSlot.ItemStack = itemStack;
			GD.Print("Stack: 1");
			return;
		}

		if (toSlot.CanStackWith(itemStack))
		{
			GD.Print("Stack: 2");
			int spaceLeft = toSlot.ItemStack.StackSize - toSlot.ItemStack.Quantity;
			if (spaceLeft > 0)
			{
				ushort transfer = (ushort)Math.Min(spaceLeft, itemStack.Quantity);
				toSlot.ItemStack.Quantity += transfer;
				itemStack.Quantity -= transfer;
			}
		} else GD.Print("Stack: 3");
	}

	#endregion

	Inventory()
	{
	   // Interactable.GetOrCreate(this).AddInteraction(ResourceLoader.Load<Interaction>("uid://bf6f2mxftmr1l"));
	}

	public override void _Ready()
	{
		Interactable.GetOrCreate(_root).AddInteraction(ResourceLoader.Load<Interaction>("uid://cdo8axpxmp65j"));
		_inventoryOwnerId = _root.GetMultiplayerAuthority();
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
