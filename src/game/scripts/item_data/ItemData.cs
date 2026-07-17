using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id = "ItemData";
    [Export] public string[] Tags;
    [Export] public ViewModel Viewmodel = null;
    [Export] public ItemStackConfig ItemStackConfig = new();
    [Export] public Dictionary Data = [];

    public void SetIn(Node node)
    {
        node.SetMeta("ItemData", this);
    }

    public static ItemData FindIn(Node node)
    {
        if (node.HasMeta("ItemData"))
        {
            return node.GetMeta("ItemData").As<ItemData>();
        }
        return null;
    }
}