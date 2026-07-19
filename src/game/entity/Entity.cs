using Godot;

[GlobalClass]
public partial class Entity : Node
{
    [Export] Godot.Collections.Array<EntityComponent> InitialComponents = new();

    private System.Collections.Generic.List<EntityComponent> _components = new();

    public override void _Ready()
    {
        for (int i = 0; i < InitialComponents.Count; i++)
        {
            var component = (EntityComponent)InitialComponents[i].DuplicateDeep();
            _components.Add(component);
        }

    }

    public override void _Process(double delta)
    {
    }
}


