using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource
{
    public byte[] Serialize()
    {
        return [];
    }
    
    

    public static ItemStack Deserialize(byte[] bytes)
    {
        return new ItemStack();
    }
}
