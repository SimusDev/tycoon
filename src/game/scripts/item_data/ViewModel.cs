using Godot;

[GlobalClass]
public abstract partial class ViewModel : Resource
{
    public enum ViewType { Entity, Local, World }

    public virtual PackedScene GetEntityView() { return null; }
    public virtual Node InstantiateEntityView() { return GetEntityView().Instantiate(); }
    
    public virtual PackedScene GetLocalView() { return null; }
    public virtual Node InstantiateLocalView() { return GetLocalView().Instantiate(); }

    public virtual PackedScene GetWorldView() { return null; }
    public virtual Node InstantiateWorldView() { return GetWorldView().Instantiate(); }

    public PackedScene GetView(ViewType viewType)
    {
        return viewType switch
        {
            ViewType.Entity => GetEntityView(),
            ViewType.Local => GetLocalView(),
            ViewType.World => GetWorldView(),
            _ => null
        };
    }

    public Node InstantiateView(ViewType viewType)
    {
        return viewType switch
        {
            ViewType.Entity => InstantiateEntityView(),
            ViewType.Local => InstantiateLocalView(),
            ViewType.World => InstantiateWorldView(),
            _ => null
        };
    }

}