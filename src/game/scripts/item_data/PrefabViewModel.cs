using Godot;

[GlobalClass]
public partial class PrefabViewModel : ViewModel
{
    [Export] public PackedScene World;
    [Export] public PackedScene Entity;
    [Export] public PackedScene Local;

    public override PackedScene GetWorldView()
    {
        return World;
    }

    public override PackedScene GetEntityView()
    {
        return Entity;
    }

    public override PackedScene GetLocalView()
    {
        return Local;
    }

}