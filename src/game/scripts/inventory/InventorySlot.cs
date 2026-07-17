using Godot;
using System;

[GlobalClass]
public partial class InventorySlot : Resource
{
    private ItemStack _itemStack = null;
    
    public bool CanStackWith(ItemStack itemStack)
    {
        if (_itemStack == null) return true;
        

        return false;
    }
}
