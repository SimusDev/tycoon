using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Inventory : Node
{
    private GDNetStream _stream = new();
    [Export] private Array<InventorySlot> _slots = [];
    private InventorySlot _selectedSlot = null;
    private bool _isSynchronized = false;
    public bool IsSynchronized() => _isSynchronized;

    public InventorySlot GetSlot(int idx)
    {
        if (_slots.Count < idx) return null; 
        return _slots[idx];
    }

    #region AddSlot
    private void AddSlot(InventorySlot slot)
    {
        if (_slots.Contains(slot)) return;
        _slots.Add(slot);
    }

    private void AddSlot(byte[] bytes)
    {
        InventorySlot slot = (InventorySlot)(GodotObject)GD.BytesToVar(bytes);
        if (slot == null) return;

        AddSlot(slot);
    }

    public void RequestAddSlot(InventorySlot slot)
    {
        
    }

    #endregion

    private void RemoveSlot(InventorySlot slot)
    {
        if (!_slots.Contains(slot)) return;
        _slots.Remove(slot);
    }

    private void RemoveSlot(int idx)
    {
        if (_slots.Count < idx) return;
        _slots.RemoveAt(idx);
    }

    [Signal] public delegate void SlotsSynchronizedEventHandler(int idx);
    [Signal] public delegate void SlotSelectedEventHandler(int idx);
    [Signal] public delegate void SlotDeselectedEventHandler(int idx);

    public override void _Ready()
    {
        SetMultiplayerAuthority(GameServer.ServerId);

        if (Multiplayer.IsServer())
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

