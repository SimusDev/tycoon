using Godot;
using System;

public partial class PlayerUI : Control
{
    [Export] private Inventory inventory;

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            if (child is Button button)
            {
                button.Pressed += () => OnBtnPressed(button.Name);
            }
        }
    }

    private void OnBtnPressed(string btnName)
    {
        switch (btnName)
        {
            case "AddSlot":
                inventory.AddSlot();
                break;
            case "AddItem":
                inventory.AddItem( ItemStack.CreateFrom(ItemDataRegistry.Instance.Get(0)) );
                break;
            case "RemoveSlot":
                inventory.RemoveSlot();
                break;
        }
    }

}
