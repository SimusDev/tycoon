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
}