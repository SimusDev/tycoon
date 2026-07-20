using Godot;
using System;

public partial class PlayerUI : Control
{
    [Export] public Control Inventory; // эта типа нода где интерфейсы инвентаренй крч и тд
    private Input.MouseModeEnum _lastMouseMode;

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("inventory")) {
            Inventory.Visible = !Inventory.Visible;
            if (Inventory.Visible)
            {
                _lastMouseMode = Input.MouseMode;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            } else Input.MouseMode = _lastMouseMode;
        }
    }

}
