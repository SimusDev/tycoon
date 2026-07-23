using Godot;

[GlobalClass]
public partial class Interaction : Resource
{
    public virtual void Do(InteractionRay interactionRay, Interactable interactable, GodotObject collider)
    {
        GD.Print($"Ray '{interactionRay}' interacted with '{interactable}'");
    }
}