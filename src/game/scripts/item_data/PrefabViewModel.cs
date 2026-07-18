using Godot;

[GlobalClass]
public partial class PrefabViewModel : ViewModel
{
    [Export] public PackedScene Entity;
    [Export] public PackedScene Local;
    [Export] public PackedScene World;

    public override PackedScene GetEntityView()
    {
        return Entity;
    }

    public override PackedScene GetLocalView()
    {
        return Local;
    }
    
    public override PackedScene GetWorldView()
    {
        return World;
    }

}