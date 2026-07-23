using Godot;

[GlobalClass]
public partial class InteractionRay : RayCast3D
{
    [Export] private Node _root;
    public Node Root => _root;

    InteractionRay()
    {
        CollideWithAreas = true;
    }

    public override void _Ready()
    {
        bool isAuth = IsMultiplayerAuthority();

        SetProcess(isAuth);
        SetPhysicsProcess(isAuth);
        SetProcessInput(isAuth);
        SetProcessUnhandledInput(isAuth);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("interact"))
        {
            GodotObject collider = GetCollider();
            if (collider == null) return;
            

            if (Interactable.TryGetIn(collider, out Interactable interactable))
            {
                interactable.Interact(this, collider);
            }
        }
    }
}