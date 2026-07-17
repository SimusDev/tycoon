using Godot;
using System;

[GlobalClass]
public partial class ItemStack : Resource, IGDNetSerializable
{
	public int TestVarSex = 0;

    void IGDNetSerializable.Serialize(GDNetBuffer buffer)
    {
        buffer.WriteInt32(TestVarSex);
    }

    void IGDNetSerializable.Deserialize(GDNetBuffer buffer)
    {
        TestVarSex = buffer.ReadInt32();
    }
}
