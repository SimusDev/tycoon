using Godot;
using System;


public partial class ScriptingCs : Node
{
    [Export] private InventorySlot slot;
    [Export] private byte[] slotSerialized;
    [Export] private InventorySlot slotDeserialized;

    [Export] private ItemStack stack;
    [Export] private byte[] stackSerialized;
    [Export] private ItemStack stackDeserialized;

    public override void _Ready()
    {
        if (true == false)
        {
            slotSerialized = slot.Serialize();
            slotDeserialized = InventorySlot.Deserialize(slotSerialized);

            stackSerialized = stack.Serialize();
            stackDeserialized = ItemStack.Deserialize(stackSerialized);
        }

        if (true) return;
        
        ItemDataRegistry.Instance.Register([
            "res://src/game/item_data/"
        ]);

        Node worldInstance = ItemDataRegistry.Instance.Get("jirobas").ViewModel.InstantiateView(ViewModel.ViewType.World);
        AddChild(worldInstance);
        GD.Print(worldInstance);
        
    }

}
