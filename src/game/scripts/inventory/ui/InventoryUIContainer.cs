using Godot;

[GlobalClass]
public partial class InventoryUIContainer : HBoxContainer
{
    [Export] private PackedScene _inventoryUIPrefab;

    [Export] public InventoryUI playerInventoryUI;
    [Export] public InventoryUI otherInventoryUI;

    private Input.MouseModeEnum _lastMouseMode = Input.MouseMode;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("inventory"))
        {
            Switch();
        }
    }

    public void Switch() // Open/Close inventory ui
    {
        Visible = !Visible;
        if (otherInventoryUI != null) otherInventoryUI.Visible = Visible;
        
        if (Visible)
        {
            _lastMouseMode = Input.MouseMode;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        } 
        else
        {
            Input.MouseMode = _lastMouseMode;
        }
    }

    public void OpenOther(Inventory inventory)
    {
        if (otherInventoryUI != null)
        {
            if (otherInventoryUI.Inventory == inventory)
            {
                Switch();
                return;
            }

            RemoveChild(otherInventoryUI);
            otherInventoryUI.QueueFree();
        }

        otherInventoryUI = _inventoryUIPrefab.Instantiate<InventoryUI>();
        otherInventoryUI.Inventory = inventory;

        AddChild(otherInventoryUI);
        Switch();
    }

    public void CloseOther()
    {
        otherInventoryUI.Hide();
    }
}