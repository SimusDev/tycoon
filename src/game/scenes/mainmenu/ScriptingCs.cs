using Godot;
using System;


public partial class ScriptingCs : Node
{
    public override void _Ready()
    {
        return;
        ItemDataRegistry.Instance.Register([
            "res://src/game/item_data/"
        ]);
    }

}
