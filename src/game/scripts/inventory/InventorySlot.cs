using Godot;
using System;

[GlobalClass]
public partial class InventorySlot : Resource
{
    [Signal] public delegate void ItemStackChangedEventHandler();
    private ItemStack _itemStack = null;
    [Export] public ItemStack ItemStack
    {
        get => _itemStack;
        set
        {
            _itemStack = value;
            Send();
            EmitSignal(SignalName.ItemStackChanged);
        }
    }
    public bool IsEmpty() => ItemStack == null;

    private static readonly GDNetBuffer _buffer = new();
    [Export] private GDNetCommunicator _communicator = new();

    private long _netId = 0;
    
    public InventorySlot()
    {
        _netId = GDNet.GenerateUniqueID();
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(_netId);
    }

    public InventorySlot(long netId)
    {
        _netId = netId;
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(netId);
    }

    private void Send()
    {
        if (!GameServer.IsMultiplayerValid() || !GameServer.Instance.Multiplayer.IsServer()) return;

        _buffer.Clear();
        
        _buffer.WriteBool(_itemStack != null);
        if (_itemStack != null)
        {
            _buffer.WriteBytesDynamic(_itemStack.Serialize());
        }
        

        _communicator.SendToAll(_buffer.GetBytes());
    }

    private void OnBytesReceived(int peer, byte[] bytes)
    {
        if (peer != GDNet.ServerID) return;

        _buffer.SetBytes(bytes);
        _buffer.Seek(0);
        if (_buffer.ReadBool()) // != null
        {
            _itemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic());
        }
    }

    public bool CanStackWith(ItemStack itemStack)
    {
        if (ItemStack == null) return true;
        // if (
        //     itemStack.ItemData == ItemStack.ItemData &&
        //     itemStack.GetData() == ItemStack.GetData()
        //    ) return true;
        
        return false;
    }

    public byte[] Serialize()
    {
        _buffer.Clear();
        
        _buffer.WriteInt64(_netId);

        _buffer.WriteBool(_itemStack != null);
        if (_itemStack != null)
        {
            _buffer.WriteBytesDynamic(ItemStack.Serialize());
        }

        return _buffer.GetBytes();
    }

    public static InventorySlot Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        InventorySlot newSlot = new(_buffer.ReadInt64());

        if (_buffer.ReadBool())
        {
            newSlot.ItemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic());
        }

        return newSlot;
    }
}

