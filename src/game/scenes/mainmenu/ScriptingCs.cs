using Godot;
using System;


public partial class ScriptingCs : Node
{
    public override void _Ready()
    {
        if (true) return;
        
        ItemDataRegistry.Instance.Register([
            "res://src/game/item_data/"
        ]);

        Node worldInstance = ItemDataRegistry.Instance.Get("jirobas").ViewModel.InstantiateView(ViewModel.ViewType.World);
        AddChild(worldInstance);
        GD.Print(worldInstance);
        
    }

}
