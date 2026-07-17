using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource
{
    public byte[] Serialize()
    {
        return new byte[0];
    }

    public static ItemStack Deserialize()
    {
        return new ItemStack();
    }
}
