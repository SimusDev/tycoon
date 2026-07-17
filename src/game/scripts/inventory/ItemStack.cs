using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource, IGDNetSerializable
{
    [Export] public ItemData ItemData;
    public uint SkinId;

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

    void IGDNetSerializable.Serialize(GDNetBuffer buffer)
    {
        buffer.WriteResource(ItemData);
        buffer.WriteUInt32(SkinId);
    }

    void IGDNetSerializable.Deserialize(GDNetBuffer buffer)
    {
        ItemData = buffer.ReadResource<ItemData>();
        SkinId = buffer.ReadUInt32();
    }
}
