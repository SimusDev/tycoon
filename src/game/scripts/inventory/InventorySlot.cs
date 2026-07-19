using Godot;
using System;

[GlobalClass]
public partial class InventorySlot : Resource
{
    //[Export] private ItemStack _itemStack = null;
    [Export] public ItemStack ItemStack
    {
        get => ItemStack;
        set
        {
            ItemStack = value;
            Send(value);
        }
    }
    public bool IsEmpty() => ItemStack == null;

    private static readonly GDNetBuffer _buffer = new();
    private GDNetCommunicator _communicator = new();
    
    public InventorySlot()
    {
        _communicator.OnBytesReceived += OnBytesReceived;
        _communicator.SynchronizeNetworkIDByUniqueID(GDNet.Instance.GenerateNetworkID());
    }

    private void Send(ItemStack itemStack)
    {
        if (!GameServer.Instance.Multiplayer.IsServer()) return;

        _buffer.Clear();
        _buffer.WriteResource(itemStack);
        
        _communicator.SendToAll(_buffer.GetBytes());
    }

    private void OnBytesReceived(int peer, byte[] bytes)
    {
        if (peer != GDNet.ServerID) return;

        _buffer.SetBytes(bytes);
        _buffer.Seek(0);
        ItemStack = _buffer.ReadResource<ItemStack>();
    }


    public bool CanStackWith(ItemStack itemStack)
    {
        if (ItemStack == null) return true;
        
        return false;
    }

    public byte[] Serialize()
    {
        _buffer.Clear();
        
        _buffer.WriteBytesDynamic(ItemStack.Serialize());

        return _buffer.GetBytes();
    }

    public static InventorySlot Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        return new()
        {
            ItemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic())
        };
    }
}

