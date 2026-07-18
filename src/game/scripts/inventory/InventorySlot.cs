using Godot;
using System;

[GlobalClass]
public partial class InventorySlot : Resource
{
    [Export] private ItemStack _itemStack = null;
    private static GDNetBuffer _buffer = new();
    
    public bool CanStackWith(ItemStack itemStack)
    {
        if (_itemStack == null) return true;
        
        return false;
    }

    public byte[] Serialize()
    {
        _buffer.Clear();
        
        _buffer.WriteBytesDynamic(_itemStack.Serialize());

        return _buffer.GetBytes();
    }

    public static InventorySlot Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        return new()
        {
            _itemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic())
        };
    }
}

