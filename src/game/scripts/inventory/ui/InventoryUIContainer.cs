using Godot;

[GlobalClass]
public partial class InventoryUIContainer : Control
{
    [Export] public InventoryUI playerInventoryUI;
    [Export] public InventoryUI otherInventoryUI;

    private Input.MouseModeEnum _lastMouseMode = Input.MouseMode;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("inventory")) {
            Visible = !Visible;
            if (Visible)
            {
                _lastMouseMode = Input.MouseMode;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            } else Input.MouseMode = _lastMouseMode;
        }
    }

    public void OpenOther(Inventory inventory)
    {
        otherInventoryUI.Show();
        otherInventoryUI.Inventory = inventory;
        Show();
    }

    public void CloseOther()
    {
        otherInventoryUI.Hide();
    }
}