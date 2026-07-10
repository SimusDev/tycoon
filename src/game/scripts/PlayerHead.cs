using Godot;

[GlobalClass]
public partial class PlayerHead : Node3D
{
    [Export] public Camera3D HeadCamera;
    private bool isAuthority;

    public override void _Ready()
    {
        isAuthority = IsMultiplayerAuthority();

        SetProcess(isAuthority);
        SetPhysicsProcess(isAuthority);
        SetProcessInput(isAuthority);
        SetProcessUnhandledInput(isAuthority);
        if (!isAuthority) return;

        if (IsInstanceValid(HeadCamera)) HeadCamera.MakeCurrent();
        setMouseCaptureMode();

    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
                Vector2 relative = @event.Relat
                
        }
    }

    private void setMouseCaptureMode()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }


}