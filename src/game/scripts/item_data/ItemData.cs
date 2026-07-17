using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ItemData : Resource
{
    [Export] public string Id = "ItemData";
    [Export] public ViewModel Viewmodel = null;
    [Export] public ItemStackConfig ItemStackConfig = new();
    [Export] public Dictionary Data = [];
}