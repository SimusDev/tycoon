using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemStack : Resource
{
    [Export] public ItemData ItemData;
    [Export] public ushort Count = 1;
    public ushort SkinId = 0;
    private static GDNetBuffer _buffer = new();

    public static ItemStack CreateFrom(Node node)
    {
        ItemData itemData = ItemData.FindIn(node);
        if (itemData != null)
        {
            ItemStack itemStack = new()
            {
                ItemData = itemData
            };
            return itemStack;
        }
        return null;
    }

    

    public byte[] Serialize()
    {
        _buffer.Clear();
        
        _buffer.WriteUInt16(Count);
        _buffer.WriteUInt16(SkinId);

        _buffer.WriteResource(ItemData);

        return _buffer.GetBytes();
    }

    public static ItemStack Deserialize(byte[] bytes)
    {
        _buffer.Clear();
        _buffer.SetBytes(bytes);

        return new()
        {
            ItemData = _buffer.ReadResource<ItemData>(),
            Count = _buffer.ReadUInt16(),
            SkinId = _buffer.ReadUInt16()
        };
    }
}
