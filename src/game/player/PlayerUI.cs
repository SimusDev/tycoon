using Godot;
using System;

public partial class PlayerUI : Control
{
    [Export] private Inventory inventory;
    static RandomNumberGenerator rng = new();

    public override void _Ready()
    {
        rng.Randomize();
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
                inventory.AddItem(
                    ItemStack.CreateFrom(ItemDataRegistry.Instance.Get(
                        rng.RandiRange(0, ItemDataRegistry.Instance.Count-1)
                )));
                break;
            case "RemoveSlot":
                inventory.RemoveSlot();
                break;
        }
    }

}
