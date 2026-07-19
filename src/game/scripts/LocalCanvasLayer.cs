using Godot;

[GlobalClass]
public partial class LocalCanvasLayer : CanvasLayer
{
    public override void _EnterTree()
    {
        if (!IsMultiplayerAuthority()) QueueFree();
    }

}
