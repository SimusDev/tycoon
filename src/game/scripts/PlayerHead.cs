using System;
using Godot;

[GlobalClass]
public partial class PlayerHead : Node3D
{
    [Export] public Node3D Player;
    [Export] public Camera3D HeadCamera;
    private bool isAuthority;
    private float SENSITIVITY_NORMALIZE_VALUE = 0.1f;

    public override void _Ready()
    {
        isAuthority = IsMultiplayerAuthority();

        SetProcess(isAuthority);
        SetPhysicsProcess(isAuthority);
        SetProcessInput(isAuthority);
        SetProcessUnhandledInput(isAuthority);
        if (!isAuthority) return;

        if (IsInstanceValid(HeadCamera)) HeadCamera.MakeCurrent();
        SetMouseCapture();

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            InverseMouseCapture();
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2 relative = mouseMotion.Relative * (1.0f * SENSITIVITY_NORMALIZE_VALUE);

            float x = Godot.Mathf.DegToRad(-relative.Y);
            float y = Godot.Mathf.DegToRad(-relative.X);

            if (IsInstanceValid(Player))
            {
                Player.RotateY(y);
            }
            RotateX(x);
            Rotation = new Vector3(
                Mathf.Clamp(Rotation.X, Mathf.DegToRad(-90f), Mathf.DegToRad(90f)),
                Rotation.Y,
                Rotation.Z
            );
        }
    }

    private void SetMouseCapture(bool value = true)
    {
        if (value) Input.MouseMode = Input.MouseModeEnum.Captured;
        else Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    private void InverseMouseCapture(bool value = true)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured) Input.MouseMode = Input.MouseModeEnum.Visible;
        else Input.MouseMode = Input.MouseModeEnum.Captured;
    }
}