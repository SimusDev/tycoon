using Godot;

[GlobalClass]
[Icon("res://icons/ClientSideNode3D.svg")]
public partial class ClientSideNode3D : Node3D
{
    public override void _EnterTree()
    {
        if (!IsMultiplayerAuthority()) QueueFree();
    }
}