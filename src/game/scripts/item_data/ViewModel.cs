using Godot;

[GlobalClass]
public abstract partial class ViewModel : Resource
{
    public virtual PackedScene GetWorldView()
    {
        return null;
    }

    public virtual PackedScene GetEntityView()
    {
        return null;
    }

    public virtual PackedScene GetLocalView()
    {
        return null;
    }

    public PackedScene GetView<T>()
    {
        string typeName = typeof(T).Name;
        
        return typeName switch
        {
            "World" => GetWorldView(),
            "Entity" => GetEntityView(),
            "Local" => GetLocalView(),
            _ => null
        };
    }
}