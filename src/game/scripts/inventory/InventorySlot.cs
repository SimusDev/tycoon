using Godot;
using System;

[GlobalClass]
public partial class InventorySlot : Resource
{
    [Export] private ItemStack _itemStack = null;
    
    public bool CanStackWith(ItemStack itemStack)
    {
        if (_itemStack == null) return true;
        
        return false;
    }

    // public byte[] Serialize()
    // {
        
    // }
}

