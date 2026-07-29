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
            EmitSignal(SignalName.ItemStackChanged);
        }
    }
    public bool IsEmpty() => ItemStack == null;

    private static readonly GDNetBuffer _buffer = new();

    public bool CanStackWith(ItemStack itemStack)
    {
        if (ItemStack == null) return true;
        if (
            itemStack.ItemData == ItemStack.ItemData
            // && itemStack.GetData().Equals(ItemStack.GetData()) //тут чут чут надо пофиксит xD
           ) return true;
        
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

        InventorySlot newSlot = new();

        if (_buffer.ReadBool())
        {
            newSlot.ItemStack = ItemStack.Deserialize(_buffer.ReadBytesDynamic());
        }

        return newSlot;
    }
}

