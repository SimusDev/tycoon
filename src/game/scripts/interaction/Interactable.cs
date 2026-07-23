using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Interactable : RefCounted
{
    [Export] private Array<Interaction> _interactions = [];
    public static short SelectedIdx = 0;

    public void AddInteraction(Interaction interaction, int position = -1)
    {
        if (_interactions.Contains(interaction)) return;
        if (position >= 0 && position <= _interactions.Count)
            _interactions.Insert(position, interaction);

        else _interactions.Add(interaction);
    }

    public void RemoveInteraction(Interaction interaction)
    {
        GD.Print("sas78");
        if (!_interactions.Contains(interaction)) return;
        _interactions.Remove(interaction);
    }

    public static Interactable GetOrCreate(GodotObject godotObject)
    {
        if (!godotObject.HasMeta(nameof(Interactable)))
        {
            Interactable interactable = new();
            godotObject.SetMeta(nameof(Interactable), interactable);
        }

        return (Interactable)(GodotObject)godotObject.GetMeta(nameof(Interactable));
    }

    public static Interactable GetIn(GodotObject godotObject)
    {
        if (!godotObject.HasMeta(nameof(Interactable))) return null;
        return (Interactable)(GodotObject)godotObject.GetMeta(nameof(Interactable));
    }

    public static bool TryGetIn(GodotObject godotObject, out Interactable interactable)
    {
        interactable = GetIn(godotObject);
        return interactable != null;
    }

    public void Interact(InteractionRay interactionRay, GodotObject collider)
    {
        if (_interactions.Count == 0) return;
        
        if (SelectedIdx >= _interactions.Count)
            SelectedIdx = (short)(_interactions.Count - 1);
            
        _interactions[SelectedIdx].Do(interactionRay, this, collider);
    }
}