using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id = "item_data";
    [Export] public Texture2D Icon = null;
    [Export] public string[] Tags;
    [Export] public ViewModel ViewModel = null;
    [Export] public ItemStackConfig ItemStackConfig = new();


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