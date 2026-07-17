using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource
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
}
