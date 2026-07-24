using Godot;

[GlobalClass]
public partial class Interaction : Resource
{
    [Export] string Name = "InteractionName";
    public virtual void OnRaySelected(InteractionRay interactionRay, Interactable interactable)
    {
        PlayerUI playerUI = interactionRay.Root.GetNodeOrNull<PlayerUI>("LocalCanvasLayer/PlayerUI");
        if (playerUI != null) playerUI.LabelSelection.Text = Name;
    }
    
    public virtual void OnRayDeselected(InteractionRay interactionRay, Interactable interactable)
    {
        PlayerUI playerUI = interactionRay.Root.GetNodeOrNull<PlayerUI>("LocalCanvasLayer/PlayerUI");
        if (playerUI != null) playerUI.LabelSelection.Text = "";
    }

    public virtual void Do(InteractionRay interactionRay, Interactable interactable)
    {
        
    }
}