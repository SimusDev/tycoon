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
    private GDNetCommunicator _communicator = new();
    
    public InventorySlot()
    {
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(GDNet.GenerateUniqueID());
    }

    private void Send()
    {
        if (!GameServer.Instance.Multiplayer.IsServer()) return;

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
        
        return false;
    }

    public byte[] Serialize()
    {
        _buffer.Clear();
        
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

        ItemStack itemStack = null;

        if (_buffer.ReadBool())
        {
            itemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic());
        }

        return new() { ItemStack = itemStack };
    }
}

