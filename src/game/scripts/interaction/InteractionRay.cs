using Godot;

[GlobalClass]
public partial class InteractionRay : RayCast3D
{
    [Export] private Node _root;
    public Node Root => _root;

    GodotObject _previousCollider = null;

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
                interactable.Interact(this);
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        GodotObject currentCollider = GetCollider();

        if (currentCollider != _previousCollider)
        {
            if (currentCollider != null)
            {
                if (Interactable.TryGetIn(currentCollider, out Interactable interactable))
                {
                    if (interactable.TryGetSelectedInteraction(out Interaction interaction))
                    {
                        interaction.OnRaySelected(this, interactable);
                    }
                }
            }
            else
            {
                if (Interactable.TryGetIn(_previousCollider, out Interactable interactable))
                {
                    if (interactable.TryGetSelectedInteraction(out Interaction interaction))
                    {
                        interaction.OnRayDeselected(this, interactable);
                    }
                }
            }
        }
        _previousCollider = currentCollider;
    }

}