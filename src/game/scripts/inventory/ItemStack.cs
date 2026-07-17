using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemStack : Resource
{
    [Export] public ItemData ItemData;
    [Export] public uint Count = 1;
    public uint SkinId = 0;

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
        var data = new Dictionary
        {
            ["count"] = Count,
            ["skin_id"] = SkinId
        };

        if (ItemData != null)
        {
            data["item_data_uid"] = ResourceUid.PathToUid(ItemData.ResourcePath);
        }

        return GD.VarToBytes(data);
    }

    public static ItemStack Deserialize(byte[] bytes)
    {
        Dictionary data = GD.BytesToVar(bytes).AsGodotDictionary();
        if (data == null) return null;

        return new()
        {
            ItemData = GD.Load<ItemData>(data?["item_data_uid"].AsString()),
            Count = data["count"].AsUInt32(),
            SkinId = data["skin_id"].AsUInt32()
        };
    }
}
